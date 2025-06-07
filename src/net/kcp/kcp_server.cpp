#include <arpa/inet.h>
#include <unistd.h>
#include "kcp_server.h"
#include <sys/epoll.h>
#include <sys/socket.h>
#include <sys/fcntl.h>
#include <chrono>
#include <algorithm>

KcpServer::KcpServer(uint16_t port)
    : listenPort(port), random_engine(std::random_device{}()), prev_conv(1000), running(false)
{
    initSocket();
    initEpoll();
}

KcpServer::~KcpServer()
{
    running = false;
    if (networkThread.joinable())
        networkThread.join();
    if (gameThread.joinable())
        gameThread.join();
    close(epollFd);
    close(udpFd);
}

void KcpServer::run()
{
    running = true;
    networkThread = std::thread(&KcpServer::networkThreadFunc, this);
    gameThread = std::thread(&KcpServer::gameThreadFunc, this);
    networkThread.join();
    gameThread.join();
}

void KcpServer::sendTo(const uint32_t conv, const google::protobuf::Message &msg)
{
    auto it = sessions.find(conv);
    if (it != sessions.end())
    {
        it->second->sendMessage(msg);
    }
}

void KcpServer::broadcastToRoom(int room_id, const google::protobuf::Message &msg,
                                const std::vector<int> &skip_player_ids)
{
    RoomManager *manager = RoomManager::getInstance();
    std::shared_ptr<Room> room = manager->getRoom(room_id);
    if (!room)
        return;
    std::vector<int> all_players = room->getAllPlayerIds();
    for (int pid : all_players)
    {
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

void KcpServer::gameLogicTick(uint32_t now)
{
    updateAllRooms(now);
    broadcastAllRooms(now);
}

void KcpServer::updateAllRooms(uint32_t now)
{
    RoomManager *manager = RoomManager::getInstance();
    std::vector<int> roomIds = manager->getAllRoomIds();
    for (int roomId : roomIds)
    {
        std::shared_ptr<Room> room = manager->getRoom(roomId);
        if (room)
        {
            updateRoomLogic(room, now);
        }
    }
}

void KcpServer::broadcastAllRooms(uint32_t now)
{
    static uint32_t lastBroadcast = 0;
    const uint32_t BROADCAST_INTERVAL = 50;
    if (now - lastBroadcast >= BROADCAST_INTERVAL)
    {
        RoomManager *manager = RoomManager::getInstance();
        std::vector<int> roomIds = manager->getAllRoomIds();
        for (int roomId : roomIds)
        {
            std::shared_ptr<Room> room = manager->getRoom(roomId);
            if (!room)
                continue;
            std::vector<int> all_players = room->getAllPlayerIds();
            for (int player_id : all_players)
            {
                message::SoulBasicMessage stateMsg;
                stateMsg.set_player_id(player_id);
                stateMsg.set_position_x(0.0f);
                stateMsg.set_position_y(0.0f);
                stateMsg.set_hp(100.0f);
                stateMsg.set_max_hp(100.0f);
                message::MessageWrapper wrapper;
                wrapper.mutable_soul_basic_message()->CopyFrom(stateMsg);
                broadcastToRoom(roomId, wrapper, {player_id});
            }
        }
        lastBroadcast = now;
    }
}

void KcpServer::updateRoomLogic(std::shared_ptr<Room> room, uint32_t now)
{
    // TODO: 实现具体的游戏逻辑
}

uint32_t KcpServer::generateConv()
{
    std::lock_guard<std::mutex> lock(conv_mutex_);
    return ++prev_conv;
}

void KcpServer::initSocket()
{
    udpFd = socket(AF_INET, SOCK_DGRAM, 0);
    sockaddr_in addr{};
    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = INADDR_ANY;
    addr.sin_port = htons(listenPort);
    bind(udpFd, (sockaddr *)&addr, sizeof(addr));
    fcntl(udpFd, F_SETFL, O_NONBLOCK);
}

void KcpServer::initEpoll()
{
    epollFd = epoll_create1(0);
    epoll_event ev{};
    ev.events = EPOLLIN;
    ev.data.fd = udpFd;
    epoll_ctl(epollFd, EPOLL_CTL_ADD, udpFd, &ev);
}

message::CharacterType KcpServer::getRandomCharacterType()
{
    std::uniform_int_distribution<int> dist(0, 3);
    return static_cast<message::CharacterType>(dist(random_engine));
}

void KcpServer::handleHello(const char *buf, int len, const sockaddr_in &cliAddr)
{
    message::HelloMessage helloMsg;
    if (!helloMsg.ParseFromArray(buf + 4, len - 4))
    {
        printf("Failed to parse HelloMessage\n");
        return;
    }
    printf("Hello message received with room_id: %d\n", helloMsg.room_id());
    RoomManager *manager = RoomManager::getInstance();
    message::RoomMessage roomMsg;
    if (helloMsg.room_id() == 0)
    {
        std::shared_ptr<Room> room = manager->createRoom();
        int room_id = room->getRoomId();
        int player_id = room->getNextPlayerId();
        uint32_t conv = generateConv();
        room->addPlayer(player_id, conv);
        player_room_map[conv] = std::make_pair(room_id, player_id);
        roomMsg.set_is_join(true);
        roomMsg.set_room_id(room_id);
        roomMsg.set_player_id(player_id);
        message::Character *character = roomMsg.add_characters();
        character->set_player_id(player_id);
        character->set_character_type(getRandomCharacterType());
        printf("New room created: %d, player_id: %d, conv: %u\n", room_id, player_id, conv);
        auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd);
        sessions[conv] = session;
        message::MessageWrapper wrapper_room;
        wrapper_room.mutable_room_message()->CopyFrom(roomMsg);
        session->sendMessage(wrapper_room);
        message::StringMessage maze_map_msg;
        maze_map_msg.set_message_type(message::StringMessageType::MAZE_MAP);
        maze_map_msg.set_message_content(room->getMazeMap().get_rle_compressed_maze());
        message::MessageWrapper wrapper_maze_map;
        wrapper_maze_map.mutable_string_message()->CopyFrom(maze_map_msg);
        session->sendMessage(wrapper_maze_map);
    }
    else
    {
        int room_id = helloMsg.room_id();
        std::shared_ptr<Room> room = manager->getRoom(room_id);
        if (!room)
        {
            roomMsg.set_is_join(false);
            roomMsg.set_room_id(-1);
            uint32_t conv = generateConv();
            auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd);
            session->sendMessage(roomMsg);
            return;
        }
        int player_id = room->getNextPlayerId();
        uint32_t conv = generateConv();
        room->addPlayer(player_id, conv);
        player_room_map[conv] = std::make_pair(room_id, player_id);
        roomMsg.set_is_join(true);
        roomMsg.set_room_id(room_id);
        roomMsg.set_player_id(player_id);
        std::vector<int> all_players = room->getAllPlayerIds();
        for (int pid : all_players)
        {
            message::Character *character = roomMsg.add_characters();
            character->set_player_id(pid);
            character->set_character_type(getRandomCharacterType());
        }
        printf("Player joined room: %d, player_id: %d, conv: %u\n", room_id, player_id, conv);
        auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd);
        sessions[conv] = session;
        session->sendMessage(roomMsg);
        broadcastToRoom(room_id, roomMsg, {player_id});
    }
}

void KcpServer::handleUdpRead()
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
        if (n < 4)
            continue;
        uint32_t conv = ikcp_getconv(buf);
        if (conv == 0)
        {
            handleHello(buf, n, cliAddr);
            continue;
        }
        auto it = sessions.find(conv);
        if (it == sessions.end())
        {
            auto map_it = player_room_map.find(conv);
            if (map_it != player_room_map.end())
            {
                int room_id = map_it->second.first;
                int player_id = map_it->second.second;
                RoomManager *manager = RoomManager::getInstance();
                std::shared_ptr<Room> room = manager->getRoom(room_id);
                if (room && room->hasPlayer(player_id))
                {
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
                printf("Unknown session conv=%u addr=%s:%d\n",
                       conv, inet_ntoa(cliAddr.sin_addr), ntohs(cliAddr.sin_port));
                continue;
            }
        }
        else
        {
            auto &sess = it->second;
            if (sess->peerAddr.sin_addr.s_addr != cliAddr.sin_addr.s_addr ||
                sess->peerAddr.sin_port != cliAddr.sin_port)
            {
                printf("Session conv=%u reconnected, old addr %s:%d -> new addr %s:%d\n",
                       conv,
                       inet_ntoa(sess->peerAddr.sin_addr), ntohs(sess->peerAddr.sin_port),
                       inet_ntoa(cliAddr.sin_addr), ntohs(cliAddr.sin_port));
                sessions.erase(it);
                auto newSess = std::make_shared<KcpSession>(conv, cliAddr, udpFd);
                sessions.emplace(conv, newSess);
                it = sessions.find(conv);
            }
        }
        it->second->input(buf, n);
    }
}

int KcpServer::calcNextTimeout()
{
    uint32_t now = currentMs();
    uint32_t next = 100;
    for (auto &kv : sessions)
    {
        uint32_t time_stamp = ikcp_check(kv.second->kcp, now);
        uint32_t diff = time_stamp > now ? time_stamp - now : 0;
        next = std::min(next, diff);
    }
    return next;
}

uint32_t KcpServer::currentMs()
{
    using namespace std::chrono;
    return (uint32_t)duration_cast<milliseconds>(steady_clock::now().time_since_epoch()).count();
}

void KcpServer::networkThreadFunc()
{
    const int MAX_EVENTS = 10;
    epoll_event events[MAX_EVENTS];
    while (running)
    {
        int timeoutMs = calcNextTimeout();
        int nfds = epoll_wait(epollFd, events, MAX_EVENTS, timeoutMs);
        uint32_t now = currentMs();
        for (int i = 0; i < nfds; ++i)
        {
            if (events[i].data.fd == udpFd)
            {
                handleUdpRead();
            }
        }
        for (auto &kv : sessions)
        {
            kv.second->update(now);
            kv.second->recvAll(onClientMessage);
        }
    }
}

void KcpServer::gameThreadFunc()
{
    uint32_t lastGameTick = currentMs();
    constexpr uint32_t GAME_TICK_INTERVAL = 16;
    while (running)
    {
        uint32_t now = currentMs();
        if (now >= lastGameTick + GAME_TICK_INTERVAL)
        {
            gameLogicTick(now);
            lastGameTick = now;
        }
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
    }
}
