// KcpServer.h

#pragma once

#include <sys/epoll.h>
#include <sys/socket.h>
#include <sys/fcntl.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <functional>
#include <unordered_map>
#include <vector>
#include <chrono>
#include <memory>
#include <random>
#include <mutex>
#include <algorithm>
#include <thread>
#include <atomic>
#include "ikcp.h"
#include "message/gen/message.pb.h" // Protobuf生成的头 / Protobuf generated headers
#include "room/room_manager.h"      // Room管理系统 / Room management system

// 定义KCP相关常量 / Define KCP related constants
#define KCP_HEADER_SIZE 24 // KCP头部大小 / KCP header size

class KcpSession
{
public:
    uint32_t conv;
    ikcpcb *kcp = nullptr;
    sockaddr_in peerAddr;

    KcpSession(uint32_t _conv, const sockaddr_in &addr, int udpFd)
        : conv(_conv), peerAddr(addr)
    {
        kcp = ikcp_create(conv, this);
        ikcp_nodelay(kcp, 1, 1, 2, 1);
        kcp->rx_minrto = 10;
        ikcp_wndsize(kcp, 32 * 4, 32 * 4);
        ikcp_setoutput(kcp, &KcpSession::kcpOutput);

        udpSocket = udpFd;
    }

    ~KcpSession()
    {
        if (kcp)
            ikcp_release(kcp);
    }

    // 定时调用 / Called periodically
    void update(uint32_t nowMs)
    {
        ikcp_update(kcp, nowMs);
    }

    // 收到UDP数据后输入 / Input after receiving UDP data
    void input(const char *data, int len)
    {
        ikcp_input(kcp, data, len);
    }

    // 从kcp中读取所有完整消息并分发 / Read all complete messages from kcp and dispatch
    template <typename F>
    void recvAll(F &&onMessage)
    {
        while (true)
        {
            int peek = ikcp_peeksize(kcp);
            if (peek < 0)
                break; // 没有完整包 / No complete packet

            std::vector<char> buf(peek);
            int n = ikcp_recv(kcp, buf.data(), peek);
            if (n <= 0)
                continue;

            // 解析Protobuf消息 / Parse Protobuf message
            message::MessageWrapper wrapper;
            if (wrapper.ParseFromArray(buf.data(), n))
            {
                // 根据oneof字段类型调用回调 / Call callback based on oneof field type
                if (wrapper.has_string_message())
                {
                    onMessage(conv, wrapper.string_message());
                }
                else if (wrapper.has_soul_basic_message())
                {
                    onMessage(conv, wrapper.soul_basic_message());
                }
                else if (wrapper.has_reaper_attack_message())
                {
                    onMessage(conv, wrapper.reaper_attack_message());
                }
                else if (wrapper.has_prop_try_get_message())
                {
                    onMessage(conv, wrapper.prop_try_get_message());
                }
                else if (wrapper.has_prop_get_message())
                {
                    onMessage(conv, wrapper.prop_get_message());
                }
            }
        }
    }

    // 发送任意Protobuf消息 / Send any Protobuf message
    void sendMessage(const google::protobuf::Message &msg)
    {
        std::string data;
        msg.SerializeToString(&data);
        ikcp_send(kcp, data.data(), data.size());
    }

private:
    int udpSocket;

    // ikcp_output回调：通过UDP发包 / ikcp_output callback: send packets via UDP
    static int kcpOutput(const char *buf, int len, ikcpcb *, void *user)
    {
        KcpSession *session = static_cast<KcpSession *>(user);
        return sendto(session->udpSocket, buf, len, 0,
                      (sockaddr *)&session->peerAddr, sizeof(session->peerAddr));
    }
};

class KcpServer
{
public:
    KcpServer(uint16_t port)
        : listenPort(port), random_engine(std::random_device{}()), prev_conv(1000), running(false)
    {
        initSocket();
        initEpoll();
    }

    ~KcpServer()
    {
        running = false;
        if (networkThread.joinable())
            networkThread.join();
        if (gameThread.joinable())
            gameThread.join();
        close(epollFd);
        close(udpFd);
    }

    // 启动主循环（阻塞） / Start main loop (blocking)
    void run()
    {
        running = true;

        // 启动两个线程 / Start two threads
        networkThread = std::thread(&KcpServer::networkThreadFunc, this);
        gameThread = std::thread(&KcpServer::gameThreadFunc, this);

        // 等待线程结束 / Wait for threads to finish
        networkThread.join();
        gameThread.join();
    }

    // 用户注册回调：收到客户端消息 / User register callback: receive client message
    template <typename F>
    void setMessageCallback(F &&cb)
    {
        onClientMessage = std::forward<F>(cb);
    }

    void sendTo(uint32_t conv, const google::protobuf::Message &msg)
    {
        auto it = sessions.find(conv);
        if (it != sessions.end())
        {
            it->second->sendMessage(msg);
        }
    }

    // 向指定房间广播消息 / Broadcast message to specified room
    void broadcastToRoom(int room_id, const google::protobuf::Message &msg,
                         const std::vector<int> &skip_player_ids = {})
    {
        RoomManager *manager = RoomManager::getInstance();
        std::shared_ptr<Room> room = manager->getRoom(room_id);
        if (!room)
            return;

        std::vector<int> all_players = room->getAllPlayerIds();

        for (int pid : all_players)
        {
            // 跳过指定的玩家ID / Skip specified player IDs
            if (std::find(skip_player_ids.begin(), skip_player_ids.end(), pid) != skip_player_ids.end())
                continue;

            int conv = room->getPlayerConv(pid);
            if (conv != -1)
            {
                auto it = sessions.find(conv);
                if (it != sessions.end())
                {
                    it->second->sendMessage(msg);
                }
            }
        }
    }

    void gameLogicTick(uint32_t now)
    {
        // 游戏逻辑更新-每16ms执行一次 / Game logic update - executed every 16ms
        // 1. 更新所有房间的游戏状态 / 1. Update all room game states
        updateAllRooms(now);

        // 2. 广播必要的状态更新 / 2. Broadcast necessary state updates
        broadcastAllRooms(now);
    }

    void updateAllRooms(uint32_t now)
    {
        RoomManager *manager = RoomManager::getInstance();
        std::vector<int> roomIds = manager->getAllRoomIds();

        for (int roomId : roomIds)
        {
            std::shared_ptr<Room> room = manager->getRoom(roomId);
            if (room)
            {
                // 更新房间内的游戏逻辑 / Update room game logic
                // 例如：玩家位置同步、碰撞检测、技能冷却等 / For example: player position sync, collision detection, skill cooldowns, etc.
                updateRoomLogic(room, now);
            }
        }
    }

    void broadcastAllRooms(uint32_t now)
    {
        // 定期广播游戏状态 / Periodic game state broadcast
        // 可以不是每tick都广播，比如每3-5个tick广播一次 / Don't broadcast every tick, e.g., broadcast every 3-5 ticks
        static uint32_t lastBroadcast = 0;
        const uint32_t BROADCAST_INTERVAL = 50; // 20fps广播频率 / 20fps broadcast frequency

        if (now - lastBroadcast >= BROADCAST_INTERVAL)
        {
            RoomManager *manager = RoomManager::getInstance();
            std::vector<int> roomIds = manager->getAllRoomIds();

            for (int roomId : roomIds)
            {
                // 创建状态消息 / Create state message
                // message::SoulBasicMessage stateMsg;
                // ... 填充状态数据 / ... fill state data ...

                // 直接广播到房间 / Broadcast directly to room
                // broadcastToRoom(roomId, stateMsg);

                // TODO: 实现具体的状态广播逻辑 / TODO: Implement specific state broadcast logic
            }

            lastBroadcast = now;
        }
    }

    void updateRoomLogic(std::shared_ptr<Room> room, uint32_t now)
    {
        // 房间内的具体游戏逻辑 / Specific game logic within room
        // 这里可以处理： / Can handle:
        // - 玩家移动 / - Player movement
        // - 攻击判定 / - Attack determination
        // - 道具生成 / - Item generation
        // - AI逻辑等 / - AI logic, etc.

        // TODO: 实现具体的游戏逻辑 / TODO: Implement specific game logic
    }

private:
    uint16_t listenPort;
    int udpFd = -1;
    int epollFd = -1;

    // 随机数生成器，用于角色分配 / Random number generator for character assignment
    std::mt19937 random_engine;

    // conv生成相关 / conv generation related
    std::mutex conv_mutex_;
    uint32_t prev_conv;

    // conv -> Session
    std::unordered_map<uint32_t, std::shared_ptr<KcpSession>> sessions;

    // conv -> (room_id, player_id)
    std::unordered_map<uint32_t, std::pair<int, int>> player_room_map;

    // 回调：conv + protobuf消息 / Callback: conv + protobuf message
    std::function<void(uint32_t, const google::protobuf::Message &)> onClientMessage;

    // 生成唯一的conv值 / Generate unique conv value
    uint32_t generateConv()
    {
        std::lock_guard<std::mutex> lock(conv_mutex_);
        return ++prev_conv;
    }

    void initSocket()
    {
        udpFd = socket(AF_INET, SOCK_DGRAM, 0);
        sockaddr_in addr{};
        addr.sin_family = AF_INET;
        addr.sin_addr.s_addr = INADDR_ANY;
        addr.sin_port = htons(listenPort);
        bind(udpFd, (sockaddr *)&addr, sizeof(addr));
        fcntl(udpFd, F_SETFL, O_NONBLOCK);
    }

    void initEpoll()
    {
        epollFd = epoll_create1(0);
        epoll_event ev{};
        ev.events = EPOLLIN;
        ev.data.fd = udpFd;
        epoll_ctl(epollFd, EPOLL_CTL_ADD, udpFd, &ev);
    }

    // 随机生成角色类型 / Randomly generate character type
    message::CharacterType getRandomCharacterType()
    {
        std::uniform_int_distribution<int> dist(0, 3);
        return static_cast<message::CharacterType>(dist(random_engine));
    }

    void handleHello(const char *buf, int len, const sockaddr_in &cliAddr)
    {
        // 直接解析HelloMessage / Parse HelloMessage directly
        message::HelloMessage helloMsg;
        // 跳过4字节conv-注意这是还不是KCP,只是单纯的UDP=conv+helloMsg / Skip 4-byte conv - note this is not KCP yet, just plain UDP = conv + helloMsg
        if (!helloMsg.ParseFromArray(buf + 4, len - 4))
        {
            printf("Failed to parse HelloMessage\n");
            return;
        }

        printf("Hello message received with room_id: %d\n", helloMsg.room_id());

        // 获取房间管理器 / Get room manager
        RoomManager *manager = RoomManager::getInstance();
        message::RoomMessage roomMsg;

        if (helloMsg.room_id() == 0)
        {
            // 创建新房间 / Create new room
            std::shared_ptr<Room> room = manager->createRoom();
            int room_id = room->getRoomId();
            int player_id = room->getNextPlayerId();

            // 添加玩家到房间，使用互斥锁保证conv唯一性 / Add player to room, use mutex to ensure conv uniqueness
            uint32_t conv = generateConv();
            room->addPlayer(player_id, conv);
            player_room_map[conv] = std::make_pair(room_id, player_id);

            // 设置RoomMessage回复 / Set RoomMessage reply
            roomMsg.set_is_join(true);
            roomMsg.set_room_id(room_id);
            roomMsg.set_player_id(player_id);

            // 添加角色信息 / Add character information
            message::Character *character = roomMsg.add_characters();
            character->set_player_id(player_id);
            character->set_character_type(getRandomCharacterType());

            printf("New room created: %d, player_id: %d, conv: %u\n",
                   room_id, player_id, conv);

            // 创建新会话 / Create new session
            auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd);
            sessions[conv] = session;

            // 发送RoomMessage给客户端 / Send RoomMessage to client
            // 添加wrapper / Add wrapper
            message::MessageWrapper wrapper_room;
            wrapper_room.mutable_room_message()->CopyFrom(roomMsg);
            session->sendMessage(wrapper_room);

            // 创建地图消息给客户端，地图从room中获取 / Create map message for client, map obtained from room
            message::StringMessage maze_map_msg;
            maze_map_msg.set_message_type(message::StringMessageType::MAZE_MAP);
            maze_map_msg.set_message_content(room->getMazeMap().get_rle_compressed_maze());
            // 添加wrapper / Add wrapper
            message::MessageWrapper wrapper_maze_map;
            wrapper_maze_map.mutable_string_message()->CopyFrom(maze_map_msg);
            session->sendMessage(wrapper_maze_map);
        }
        else
        {
            // 加入现有房间 / Join existing room
            int room_id = helloMsg.room_id();
            std::shared_ptr<Room> room = manager->getRoom(room_id);

            if (!room)
            {
                // 如果房间不存在，返回错误 / If room doesn't exist, return error
                roomMsg.set_is_join(false);
                roomMsg.set_room_id(-1); // 表示错误 / Indicates error

                // 创建临时会话发送错误 / Create temporary session to send error
                uint32_t conv = generateConv();
                auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd);
                session->sendMessage(roomMsg);
                return;
            }

            // 获取新玩家ID / Get new player ID
            int player_id = room->getNextPlayerId();

            // 添加玩家到房间，使用互斥锁保证conv唯一性 / Add player to room, use mutex to ensure conv uniqueness
            uint32_t conv = generateConv();
            room->addPlayer(player_id, conv);
            player_room_map[conv] = std::make_pair(room_id, player_id);

            // 设置RoomMessage回复 / Set RoomMessage reply
            roomMsg.set_is_join(true); // 加入现有房间 / Join existing room
            roomMsg.set_room_id(room_id);
            roomMsg.set_player_id(player_id);

            // 添加该房间所有玩家信息（包括新玩家） / Add all player information in the room (including new player)
            std::vector<int> all_players = room->getAllPlayerIds();
            for (int pid : all_players)
            {
                message::Character *character = roomMsg.add_characters();
                character->set_player_id(pid);
                // TODO: 未来需要创建map同时有player_id和character_type，以直接获取，保证所有人character_type唯一 / TODO: Need to create map with both player_id and character_type for direct access, ensure unique character_type for everyone
                character->set_character_type(getRandomCharacterType());
            }

            printf("Player joined room: %d, player_id: %d, conv: %u\n",
                   room_id, player_id, conv);

            // 创建新会话 / Create new session
            auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd);
            sessions[conv] = session;

            // 发送RoomMessage给新玩家 / Send RoomMessage to new player
            session->sendMessage(roomMsg);

            // 向房间中其他玩家广播新玩家加入 / Broadcast new player join to other players in room
            broadcastToRoom(room_id, roomMsg, {player_id});
        }
    }

    void handleUdpRead()
    {
        char buf[4096];
        sockaddr_in cliAddr;
        socklen_t cliLen = sizeof(cliAddr);

        while (true)
        {
            int n = recvfrom(udpFd, buf, sizeof(buf), 0, (sockaddr *)&cliAddr, &cliLen);
            if (n < 0)
            {
                if (errno == EAGAIN || errno == EWOULDBLOCK)
                    break;
                perror("recvfrom");
                break;
            }
            // data too small, not have conv id
            if (n < 4)
                continue;

            // 解析KCP会话ID / Parse KCP session ID
            uint32_t conv = ikcp_getconv(buf);

            // if conv == 0, handleHello
            if (conv == 0)
            {
                handleHello(buf, n, cliAddr);
                continue;
            }

            // 处理现有会话 / Handle existing session
            auto it = sessions.find(conv);
            if (it == sessions.end())
            {
                // 如果会话不存在，但conv不为0，可能是连接断开后重连 / If session doesn't exist but conv is not 0, might be reconnection after disconnection
                // 首先检查该conv是否在player_room_map中 / First check if this conv exists in player_room_map
                auto map_it = player_room_map.find(conv);
                if (map_it != player_room_map.end())
                {
                    // 获取房间和玩家ID / Get room and player ID
                    int room_id = map_it->second.first;
                    int player_id = map_it->second.second;

                    // 检查房间是否存在 / Check if room exists
                    RoomManager *manager = RoomManager::getInstance();
                    std::shared_ptr<Room> room = manager->getRoom(room_id);

                    if (room && room->hasPlayer(player_id))
                    {
                        // 重建会话 / Rebuild session
                        auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd);
                        sessions[conv] = session;

                        printf("Reconnected session conv=%u addr=%s:%d for player %d in room %d\n",
                               conv, inet_ntoa(cliAddr.sin_addr), ntohs(cliAddr.sin_port),
                               player_id, room_id);

                        it = sessions.find(conv);
                    }
                }

                if (it == sessions.end())
                {
                    // 真的是新会话或无效会话 / Really new session or invalid session
                    printf("Unknown session conv=%u addr=%s:%d\n",
                           conv, inet_ntoa(cliAddr.sin_addr), ntohs(cliAddr.sin_port));
                    continue;
                }
            }
            else
            {
                // Session exists, check if address has changed
                auto &sess = it->second;
                if (sess->peerAddr.sin_addr.s_addr != cliAddr.sin_addr.s_addr ||
                    sess->peerAddr.sin_port != cliAddr.sin_port)
                {
                    // Address changed: treat as reconnection, rebuild session
                    printf("Session conv=%u reconnected, old addr %s:%d -> new addr %s:%d\n",
                           conv,
                           inet_ntoa(sess->peerAddr.sin_addr), ntohs(sess->peerAddr.sin_port),
                           inet_ntoa(cliAddr.sin_addr), ntohs(cliAddr.sin_port));

                    // Release old
                    sessions.erase(it);
                    // New
                    auto newSess = std::make_shared<KcpSession>(conv, cliAddr, udpFd);
                    sessions.emplace(conv, newSess);
                    it = sessions.find(conv);
                }
                // Otherwise same address, just update input normally
            }

            // Finally, give the packet to KCP
            it->second->input(buf, n);
        }
    }

    // Calculate how many milliseconds until the next kcp update
    int calcNextTimeout()
    {
        uint32_t now = currentMs();
        uint32_t next = 100; // 最多100ms / Maximum 100ms
        for (auto &kv : sessions)
        {
            uint32_t time_stamp = ikcp_check(kv.second->kcp, now);
            uint32_t diff = time_stamp > now ? time_stamp - now : 0;
            next = std::min(next, diff);
        }
        return next;
    }

    static uint32_t currentMs()
    {
        using namespace std::chrono;
        return (uint32_t)duration_cast<milliseconds>(
                   steady_clock::now().time_since_epoch())
            .count();
    }

    // 网络处理线程 / Network handling thread
    void networkThreadFunc()
    {
        const int MAX_EVENTS = 10;
        epoll_event events[MAX_EVENTS];

        while (running)
        {
            // 计算距离下一次kcp更新的最小超时时间 / Calculate minimum timeout until next kcp update
            int timeoutMs = calcNextTimeout();

            int nfds = epoll_wait(epollFd, events, MAX_EVENTS, timeoutMs);
            uint32_t now = currentMs();

            // 1. Handle UDP packet reception
            for (int i = 0; i < nfds; ++i)
            {
                if (events[i].data.fd == udpFd)
                {
                    handleUdpRead();
                }
            }

            // 2. Update all KCP sessions
            for (auto &kv : sessions)
            {
                kv.second->update(now);
                // Read complete messages from kcp
                kv.second->recvAll(onClientMessage);
            }
        }
    }

    // 游戏逻辑线程 / Game logic thread
    void gameThreadFunc()
    {
        uint32_t lastGameTick = currentMs();
        const uint32_t GAME_TICK_INTERVAL = 16; // 60fps

        while (running)
        {
            uint32_t now = currentMs();

            // 3. Game logic tick (60fps)
            if (now >= lastGameTick + GAME_TICK_INTERVAL)
            {
                gameLogicTick(now);
                lastGameTick = now;
            }

            // 短暂休眠避免空转 / Short sleep to avoid busy waiting
            std::this_thread::sleep_for(std::chrono::milliseconds(1));
        }
    }

    std::thread networkThread;
    std::thread gameThread;
    std::atomic<bool> running;
};
