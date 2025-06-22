#include <arpa/inet.h>
#include <unistd.h>
#include "kcp_server.h"
#include <sys/epoll.h>
#include <sys/socket.h>
#include <sys/fcntl.h>
#include <chrono>
#include <algorithm>
#include <ranges>

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

void KcpServer::gameLogicTick()
{
    updateAllRooms();
    iterateBroadcastAllRooms();
}

void KcpServer::updateAllRooms()
{
    for (const std::vector<int> roomIds = room_manager->getAllRoomIds(); const int roomId : roomIds)
    {
        if (const std::shared_ptr<Room> room = room_manager->getRoom(roomId))
        {
            if (!room)
                continue;
            if (!room->getStartGame())
                updateLobbyLogic(room);
            else
                updateRoomLogic(room);
        }
    }
}

void KcpServer::iterateBroadcastAllRooms()
{
    for (const std::vector<int> roomIds = room_manager->getAllRoomIds(); const int roomId : roomIds)
    {
        const std::shared_ptr<Room> room = room_manager->getRoom(roomId);
        if (!room || !room->getStartGame())
            continue;

        // PlayerBasicMessage
        for (std::vector<int> all_players = room->getAllPlayerIds(); int player_id : all_players)
        {
            message::PlayerBasicMessage playerMsg;
            Position position = room->getPlayer(player_id).position;
            playerMsg.set_player_id(player_id);
            playerMsg.set_position_x(position.x);
            playerMsg.set_position_y(position.y);
            playerMsg.set_hp(room->getPlayer(player_id).hp);
            playerMsg.set_max_hp(room->getPlayer(player_id).maxHp);
            playerMsg.set_character_state(room->getPlayer(player_id).character_state);
            playerMsg.set_animation_type(room->getPlayer(player_id).animation_type);
            message::MessageWrapper wrapper;
            wrapper.mutable_player_basic_message()->CopyFrom(playerMsg);
            broadcastToRoom(roomId, wrapper, {}, true);
        }

        // TODO: other messages to broadcast each frame
    }
}

// 向指定房间广播消息 / Broadcast message to specified room
void KcpServer::broadcastToRoom(const int room_id, const google::protobuf::Message &msg, const std::vector<int> &skip_player_ids, const bool in_game)
{
    const std::shared_ptr<Room> room = room_manager->getRoom(room_id);
    if (!room)
        return;

    for (const std::vector<int> all_players = room->getAllPlayerIds(); int pid : all_players)
    {
        // 跳过指定的玩家ID / Skip specified player IDs
        if (std::ranges::find(skip_player_ids, pid) != skip_player_ids.end())
            continue;

        if (const int conv = room->getPlayerConv(pid); conv > 0)
        {
            if (in_game && !room->getPlayer(pid).is_start_rec_game_msg)
                continue;
            sendTo(conv, msg);
        }
    }
}

void KcpServer::updateLobbyLogic(std::shared_ptr<Room> room)
{
    // Process messages from the room's queue
    ClientMessageEvent event;
    int length_to_dequeue = room->client_message_queue_.size_approx();
    while (length_to_dequeue-- > 0 && room->client_message_queue_.try_dequeue(event)) // Assuming popMessage returns true if a message was popped
    {
        if (!event.message)
        {
            continue; // Skip if message is null
        }

        // Attempt to cast the generic protobuf message to MessageWrapper
        const message::MessageWrapper *wrapper_ptr = dynamic_cast<const message::MessageWrapper *>(event.message.get());

        if (!wrapper_ptr)
        {
            printf("Failed to cast message to MessageWrapper in updateLobbyLogic for room %d\n", room->getRoomId());
            continue; // Skip if cast fails
        }

        const message::MessageWrapper &wrapper = *wrapper_ptr;
        const int player_id = event.player_id;

        switch (wrapper.payload_case())
        {
        case message::MessageWrapper::kLobbyMessage:
        {
            const message::LobbyMessage &lobbyMsg = wrapper.lobby_message();
            if (room->hasPlayer(player_id))
            {
                Player &player = room->getPlayer(player_id);
                player.character_type = lobbyMsg.character_type();
                player.is_ready = lobbyMsg.is_ready();

                printf("Player %d in room %d updated via queue: char_type=%d, is_ready=%s\n", player_id, room->getRoomId(), static_cast<int>(player.character_type), player.is_ready ? "true" : "false");

                // Broadcast the LobbyMessage to other players in the room
                // The original wrapper already contains the LobbyMessage with the correct player_id from the sender
                broadcastToRoom(room->getRoomId(), wrapper, {player_id}, false);
            }
            break;
        }
        // TODO: other message to the lobby here
        default:;
        }
    }

    // Check game start conditions
    if (room->canStartGame())
    {
        room->setStartGame(true);
        printf("Room %d: Game starting! All players ready, %zu players, 1 reaper.\n", room->getRoomId(), room->getPlayerCount());
        // Optionally, broadcast a "GameStarting" message to all players in the room
        // message::GameStartingMessage gameStartMsg;
        // gameStartMsg.set_room_id(room->getRoomId());
        // message::MessageWrapper startWrapper;
        // startWrapper.mutable_game_starting_message()->CopyFrom(gameStartMsg);
        // broadcastToRoom(room->getRoomId(), startWrapper, {}, false); // Broadcast to everyone, not in_game yet
        // TODO: not use this is just because not necessary, but better.
        //       It's like everyone wait loading of other players in LOL.
        //       What we do is next step, like when someone not load in for so long time,
        //       others directly start without him/her.
    }
}

void KcpServer::updateRoomLogic(std::shared_ptr<Room> room)
{
    // TODO: Real game logic
    ClientMessageEvent event;
    int length_to_dequeue = room->client_message_queue_.size_approx();
    while (length_to_dequeue-- > 0 && room->client_message_queue_.try_dequeue(event)) // Assuming popMessage returns true if a message was popped
    {
        if (!event.message)
        {
            continue; // Skip if message is null
        }

        // Attempt to cast the generic protobuf message to MessageWrapper
        auto wrapper_ptr = dynamic_cast<const message::MessageWrapper *>(event.message.get());

        if (!wrapper_ptr)
        {
            printf("Failed to cast message to MessageWrapper in updateRoomLogic for room %d\n", room->getRoomId());
            continue; // Skip if cast fails
        }

        const message::MessageWrapper &wrapper = *wrapper_ptr;
        const int player_id = event.player_id;

        switch (wrapper.payload_case())
        {
        case message::MessageWrapper::kStartReceiveMsgMessage:
        {
            if (room->hasPlayer(player_id))
            {
                Player &player = room->getPlayer(player_id);
                player.is_start_rec_game_msg = true;
                printf("Player %d in room %d updated via queue: is_start_rec_game_msg=%s\n", player_id, room->getRoomId(),
                       player.is_start_rec_game_msg ? "true" : "false");
            }
            break;
        }
        case message::MessageWrapper::kPlayerBasicMessage:
        {
            if (room->hasPlayer(player_id))
            {
                Player &player = room->getPlayer(player_id);
                player.position.x = wrapper.player_basic_message().position_x();
                player.position.y = wrapper.player_basic_message().position_y();
                player.animation_type = wrapper.player_basic_message().animation_type();
                printf("Player %d in room %d updated via queue: char_type=%d, is_ready=%s\n", player_id, room->getRoomId(),
                       static_cast<int>(player.character_type), player.is_ready ? "true" : "false");
            }
            break;
        }
        case message::MessageWrapper::kIntegerMessage:
        {
            switch (wrapper.integer_message().message_type())
            {
            case message::IntegerMessageType::ALTAR_MINI_GAME_SUCCESS:
            {
                if (room->hasPlayer(player_id))
                {
                    Player &player = room->getPlayer(player_id);
                    player.recoverHp(player.maxHp * 0.5f);
                    printf("Player %d in room %d recovered %f hp by altar mini game success\n", player_id, room->getRoomId(), player.maxHp * 0.5f);
                    // send this msg to all players
                    broadcastToRoom(room->getRoomId(), wrapper, {}, true);
                }
                break;
            }
            // attack result after player basic message in case of reaper attack to change the animation type of soul
            case message::IntegerMessageType::REAPER_ATTACK_RESULT:
            {
                if (room->hasPlayer(player_id))
                {
                    Player &soul = room->getPlayer(wrapper.integer_message().value());
                    soul.animation_type = message::PlayerAnimationType::HIT;
                    // decrease Hp after set to Hit, becasue in case soul directly die
                    soul.decreaseHp(soul.maxHp * 0.5f);
                    printf("Player %d in room %d attacked soul %d, soul hp decreased to %f\n", player_id, room->getRoomId(), wrapper.integer_message().value(), soul.hp);
                }
                break;
            }
            default:;
            }
        }
        // TODO: other message to the game here
        default:;
        }
    }
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
    if (const int ret = bind(udpFd, reinterpret_cast<sockaddr *>(&addr), sizeof(addr)); ret < 0)
    {
        perror("bind");
        exit(1);
    }
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
    message::RoomMessage roomMsg;
    if (helloMsg.room_id() == 0)
    {
        std::shared_ptr<Room> room = room_manager->createRoom();
        int room_id = room->getRoomId();
        int player_id = room->getNextPlayerId();
        uint32_t conv = generateConv();
        room->addPlayer(player_id, static_cast<int>(conv));
        player_room_map[conv] = std::make_pair(room_id, player_id);
        roomMsg.set_is_join(true);
        roomMsg.set_room_id(room_id);
        roomMsg.set_player_id(player_id);
        message::Character *character = roomMsg.add_characters();
        character->set_player_id(player_id);
        character->set_character_type(getRandomCharacterType());
        printf("New room created: %d, player_id: %d, conv: %u\n", room_id, player_id, conv);
        auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd, room_id, player_id, room);
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
        message::MessageWrapper wrapper_gate_msg;
        message::GateMessage gate_msg;
        for (const auto &[direction, type] : room->getGateTypes())
        {
            message::Gate *gate = gate_msg.add_gates();
            gate->set_gate_direction(direction);
            gate->set_gate_type(type);
        }
        wrapper_gate_msg.mutable_gate_message()->CopyFrom(gate_msg);
        session->sendMessage(wrapper_gate_msg);
    }
    else
    {
        int room_id = helloMsg.room_id();
        std::shared_ptr<Room> room = room_manager->getRoom(room_id);
        if (!room)
        {
            roomMsg.set_is_join(false);
            roomMsg.set_room_id(-1);
            uint32_t conv = generateConv();
            auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd, -1, -1, nullptr);
            session->sendMessage(roomMsg);
            return;
        }
        int player_id = room->getNextPlayerId();
        uint32_t conv = generateConv();
        room->addPlayer(player_id, static_cast<int>(conv));
        player_room_map[conv] = std::make_pair(room_id, player_id);
        roomMsg.set_is_join(true);
        roomMsg.set_room_id(room_id);
        roomMsg.set_player_id(player_id);
        std::vector<int> all_players = room->getAllPlayerIds();
        for (int pid : all_players)
        {
            message::Character *character = roomMsg.add_characters();
            character->set_player_id(pid);
            if (pid != player_id)
                character->set_character_type(room->getPlayer(pid).character_type);
            else
                character->set_character_type(getRandomCharacterType());
        }
        printf("Player joined room: %d, player_id: %d, conv: %u\n", room_id, player_id, conv);
        auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd, room_id, player_id, room);
        sessions[conv] = session;
        message::MessageWrapper wrapper_room;
        wrapper_room.mutable_room_message()->CopyFrom(roomMsg);
        session->sendMessage(wrapper_room);
        broadcastToRoom(room_id, wrapper_room, {player_id}, false);
        message::StringMessage maze_map_msg;
        maze_map_msg.set_message_type(message::StringMessageType::MAZE_MAP);
        maze_map_msg.set_message_content(room->getMazeMap().get_rle_compressed_maze());
        message::MessageWrapper wrapper_maze_map;
        wrapper_maze_map.mutable_string_message()->CopyFrom(maze_map_msg);
        session->sendMessage(wrapper_maze_map);
        message::MessageWrapper wrapper_gate_msg;
        message::GateMessage gate_msg;
        for (const auto &[direction, type] : room->getGateTypes())
        {
            message::Gate *gate = gate_msg.add_gates();
            gate->set_gate_direction(direction);
            gate->set_gate_type(type);
        }
        wrapper_gate_msg.mutable_gate_message()->CopyFrom(gate_msg);
        session->sendMessage(wrapper_gate_msg);
    }
}

void KcpServer::handleUdpRead()
{
    char buf[4096];
    sockaddr_in cliAddr{};
    socklen_t cliLen = sizeof(cliAddr);
    while (true)
    {
        const int n = static_cast<int>(recvfrom(udpFd, buf, sizeof(buf), 0, reinterpret_cast<sockaddr *>(&cliAddr), &cliLen));
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
        // Hello消息 / Hello message
        if (conv == 0)
        {
            handleHello(buf, n, cliAddr);
            continue;
        }
        // 其他kcp消息 / Other kcp messages
        auto it = sessions.find(conv);
        // 新连接 / New connection
        if (it == sessions.end())
        {
            auto map_it = player_room_map.find(conv);
            if (map_it != player_room_map.end())
            {
                int room_id = map_it->second.first;
                int player_id = map_it->second.second;
                std::shared_ptr<Room> room = room_manager->getRoom(room_id);
                if (room && room->hasPlayer(player_id))
                {
                    const auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd, room_id, player_id, std::move(room));
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
        // 重连 / Reconnection
        else
        {
            if (const auto &sess = it->second; sess->peerAddr.sin_addr.s_addr != cliAddr.sin_addr.s_addr ||
                                               sess->peerAddr.sin_port != cliAddr.sin_port)
            {
                printf("Session conv=%u reconnected, old addr %s:%d -> new addr %s:%d\n",
                       conv,
                       inet_ntoa(sess->peerAddr.sin_addr), ntohs(sess->peerAddr.sin_port),
                       inet_ntoa(cliAddr.sin_addr), ntohs(cliAddr.sin_port));
                sessions.erase(it);
                auto newSess = std::make_shared<KcpSession>(conv, cliAddr, udpFd, sess->roomId, sess->playerId, sess->room);
                sessions.emplace(conv, newSess);
                it = sessions.find(conv);
            }
        }
        it->second->input(buf, n);
    }
}

int KcpServer::calcNextTimeout() const
{
    const uint32_t now = currentMs();
    uint32_t next = 100;
    for (const auto &session : sessions | std::views::values)
    {
        const uint32_t time_stamp = ikcp_check(session->kcp, now);
        uint32_t diff = time_stamp > now ? time_stamp - now : 0;
        next = std::min(next, diff);
    }
    return static_cast<int>(next);
}

uint32_t KcpServer::currentMs()
{
    using namespace std::chrono;
    return static_cast<uint32_t>(duration_cast<milliseconds>(steady_clock::now().time_since_epoch()).count());
}

void KcpServer::networkThreadFunc()
{
    constexpr int MAX_EVENTS = 10;
    epoll_event events[MAX_EVENTS];
    while (running)
    {
        const int timeoutMs = calcNextTimeout();
        const int nfds = epoll_wait(epollFd, events, MAX_EVENTS, timeoutMs);
        const uint32_t now = currentMs();
        for (int i = 0; i < nfds; ++i)
        {
            if (events[i].data.fd == udpFd)
            {
                handleUdpRead();
            }
        }
        for (const auto &session : sessions | std::views::values)
        {
            session->update(now);
            session->recvAll();
        }
    }
}

void KcpServer::gameThreadFunc()
{
    uint32_t lastGameTick = currentMs();
    while (running)
    {
        constexpr uint32_t GAME_TICK_INTERVAL = 16;
        if (const uint32_t now = currentMs(); now >= lastGameTick + GAME_TICK_INTERVAL)
        {
            gameLogicTick();
            lastGameTick = now;
        }
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
    }
}
