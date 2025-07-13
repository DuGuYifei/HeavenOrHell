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
    close(epollFd);
    close(udpFd);
}

void KcpServer::run()
{
    running = true;
    mainLoop();
}

void KcpServer::sendTo(const uint32_t conv, const google::protobuf::Message &msg)
{
    auto it = sessions.find(conv);
    if (it != sessions.end())
    {
        it->second->sendMessage(msg);
    }
}

void KcpServer::gameLogicTick(float delta_time)
{
    updateAllRooms();
    iterateBroadcastAllRooms(delta_time);
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

void KcpServer::iterateBroadcastAllRooms(float delta_time)
{
    std::vector<int> finishedRoomIds;
    for (const std::vector<int> roomIds = room_manager->getAllRoomIds(); const int roomId : roomIds)
    {
        const std::shared_ptr<Room> room = room_manager->getRoom(roomId);
        if (!room || !room->getStartGame())
            continue;

        // PlayerBasicMessage
        for (std::vector<int> all_players = room->getAllPlayerIds(); int player_id : all_players)
        {
            room->getPlayer(player_id).weak_update(delta_time);
            message::PlayerBasicMessage playerMsg;
            Position position = room->getPlayer(player_id).position;
            playerMsg.set_player_id(player_id);
            playerMsg.set_position_x(position.x);
            playerMsg.set_position_y(position.y);
            playerMsg.set_hp(room->getPlayer(player_id).hp);
            playerMsg.set_max_hp(room->getPlayer(player_id).maxHp);
            playerMsg.set_character_state(room->getPlayer(player_id).character_state);
            playerMsg.set_animation_type(room->getPlayer(player_id).animation_type);
            playerMsg.set_weak_timer(room->getPlayer(player_id).weak_timer);
            message::MessageWrapper wrapper;
            wrapper.mutable_player_basic_message()->CopyFrom(playerMsg);
            broadcastToRoom(roomId, wrapper, {}, true);
        }

        // TODO: other messages to broadcast each frame
        if (checkGameResult(room))
            finishedRoomIds.push_back(roomId);
    }

    for (const int roomId : finishedRoomIds)
        room_manager->deleteRoom(roomId);
}

bool KcpServer::checkGameResult(std::shared_ptr<Room> room)
{
    // 统计各种状态的玩家数量
    int in_game_count = 0;
    int heaven_count = 0;
    int total_players = 0;

    for (const auto &playerId : room->getAllPlayerIds())
    {
        const Player &player = room->getPlayer(playerId);
        total_players++;

        switch (player.player_result)
        {
        case PlayerResult::IN_GAME:
            in_game_count++;
            break;
        case PlayerResult::HEAVEN:
            heaven_count++;
            break;
        case PlayerResult::DIE_BY_HIT:
        case PlayerResult::HELL:
            // 这些状态不需要特殊处理
            break;
        }
    }

    // 游戏结束条件：只剩一个人IN_GAME
    if (in_game_count <= 1)
    {
        message::GameResultMessage game_result_message;
        message::GameResult game_result;

        // 根据总玩家数和HEAVEN玩家数判断胜负
        if (total_players == 2)
        {
            // 两人游戏：一个人HEAVEN就soul win，其他reaper win
            if (heaven_count >= 1)
            {
                game_result = message::GameResult::GAME_RESULT_SOUL_WIN;
            }
            else
            {
                game_result = message::GameResult::GAME_RESULT_REAPER_WIN;
            }
        }
        else if (total_players == 3)
        {
            // 三人游戏：两个人HEAVEN就soul win，其他reaper win
            if (heaven_count >= 2)
            {
                game_result = message::GameResult::GAME_RESULT_SOUL_WIN;
            }
            else
            {
                game_result = message::GameResult::GAME_RESULT_REAPER_WIN;
            }
        }
        else if (total_players == 4)
        {
            // 四人游戏：0-1个HEAVEN = reaper win，2个HEAVEN = tie，3个HEAVEN = soul win
            if (heaven_count <= 1)
            {
                game_result = message::GameResult::GAME_RESULT_REAPER_WIN;
            }
            else if (heaven_count == 2)
            {
                game_result = message::GameResult::GAME_RESULT_TIE;
            }
            else // heaven_count >= 3
            {
                game_result = message::GameResult::GAME_RESULT_SOUL_WIN;
            }
        }
        else
        {
            // 其他玩家数量，默认reaper win
            game_result = message::GameResult::GAME_RESULT_REAPER_WIN;
        }

        game_result_message.set_game_result(game_result);

        // 添加所有玩家的结果信息
        for (const auto &playerId : room->getAllPlayerIds())
        {
            const Player &player = room->getPlayer(playerId);
            message::PlayerResultMessage *player_result_msg = game_result_message.add_player_result_messages();
            player_result_msg->set_player_id(playerId);

            // 转换PlayerResult到message::PlayerResult
            switch (player.player_result)
            {
            case PlayerResult::DIE_BY_HIT:
                player_result_msg->set_player_result(message::PlayerResult::PLAYER_RESULT_DIE_BY_HIT);
                break;
            case PlayerResult::HELL:
                player_result_msg->set_player_result(message::PlayerResult::PLAYER_RESULT_HELL);
                break;
            case PlayerResult::HEAVEN:
                player_result_msg->set_player_result(message::PlayerResult::PLAYER_RESULT_HEAVEN);
                break;
            case PlayerResult::IN_GAME:
                // IN_GAME的玩家在游戏结束时可能是最后的幸存者，根据角色类型判断
                if (player.character_type == message::CharacterType::REAPER)
                {
                    // 判断结果是reper win还是其他
                    if (game_result == message::GameResult::GAME_RESULT_REAPER_WIN)
                    {
                        player_result_msg->set_player_result(message::PlayerResult::PLAYER_RESULT_REAPER_HAPPY);
                    }
                    else
                    {
                        player_result_msg->set_player_result(message::PlayerResult::PLAYER_RESULT_REAPER_SAD);
                    }
                }
            }
        }

        message::MessageWrapper wrapper;
        wrapper.mutable_game_result_message()->CopyFrom(game_result_message);
        broadcastToRoom(room->getRoomId(), wrapper, {}, true);

        printf("Room %d: Game ended! Result: %d, Total players: %d, Heaven count: %d, In-game count: %d\n",
               room->getRoomId(), static_cast<int>(game_result), total_players, heaven_count, in_game_count);
        return true;
    }
    return false;
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
                // printf("Player %d in room %d updated via queue: char_type=%d, player position(%f, %f)", player_id, room->getRoomId(), static_cast<int>(player.character_type), player.position.x, player.position.y);
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
                    Player &player = room->getPlayer(wrapper.integer_message().value());
                    if (player.character_state == message::CharacterState::Character_STATE_WEAK)
                    {
                        player.character_state = message::CharacterState::Character_STATE_NORMAL;
                        player.recoverHp(player.maxHp * 0.5f);
                    }
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
                    int victim_id = wrapper.integer_message().value();
                    Player &soul = room->getPlayer(victim_id);
                    soul.animation_type = message::PlayerAnimationType::HIT;
                    // decrease Hp after set to Hit, becasue in case soul directly die
                    soul.decreaseHp(soul.maxHp * 0.5f);
                    printf("Player %d in room %d attacked soul %d, soul hp decreased to %f\n", player_id, room->getRoomId(), wrapper.integer_message().value(), soul.hp);
                    // send this msg to the soul who attacked
                    message::IntegerMessage attack_result_msg;
                    attack_result_msg.set_message_type(message::IntegerMessageType::REAPER_ATTACK_RESULT);
                    attack_result_msg.set_value(victim_id);
                    message::MessageWrapper wrapper_attack_result;
                    wrapper_attack_result.mutable_integer_message()->CopyFrom(attack_result_msg);
                    // broadcast to all players, then can play hit animation or sfx
                    broadcastToRoom(room->getRoomId(), wrapper_attack_result, {}, true);
                }
                break;
            }
            default:;
            }
            break;
        }
        case message::MessageWrapper::kEnterGateMessage:
        {
            if (room->hasPlayer(player_id))
            {
                Player &player = room->getPlayer(player_id);
                message::GateType gate_type = room->getGateTypes().at(wrapper.enter_gate_message().gate_direction());
                printf("Player %d in room %d entered gate %d, gate type: %d\n", player_id, room->getRoomId(), wrapper.enter_gate_message().gate_direction(), static_cast<int>(gate_type));
                if (gate_type == message::GateType::GATE_HEAVEN)
                {
                    player.player_result = PlayerResult::HEAVEN;
                }
                else if (gate_type == message::GateType::GATE_HELL)
                {
                    player.hp = 0;
                    player.character_state = message::CharacterState::Character_STATE_DIE;
                    player.animation_type = message::PlayerAnimationType::DIE;
                    player.player_result = PlayerResult::HELL;
                }
                broadcastToRoom(room->getRoomId(), wrapper, {}, true);
            }
            break;
        }
        case message::MessageWrapper::kChatMessage:
        {
            if (room->hasPlayer(player_id))
            {
                if (wrapper.chat_message().is_to_all())
                {
                    printf("Player %d in room %d sent chat message: %s\n", player_id, room->getRoomId(), wrapper.chat_message().content().c_str());
                    broadcastToRoom(room->getRoomId(), wrapper, {}, true);
                }
                else
                {
                    // find reaper and skip reaper
                    for (const std::vector<int> all_players = room->getAllPlayerIds(); const int pid : all_players)
                    {
                        if (room->getPlayer(pid).character_type != message::CharacterType::REAPER)
                            continue;
                        printf("Player %d in room %d sent chat message to non-reaper: %s\n", player_id, room->getRoomId(), wrapper.chat_message().content().c_str());
                        broadcastToRoom(room->getRoomId(), wrapper, {pid}, true);
                    }
                }
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
        message::CharacterType player_character_type = getRandomCharacterType();
        room->getPlayer(player_id).character_type = player_character_type;
        player_room_map[conv] = std::make_pair(room_id, player_id);
        roomMsg.set_is_join(true);
        roomMsg.set_room_id(room_id);
        roomMsg.set_player_id(player_id);
        message::Character *character = roomMsg.add_characters();
        character->set_player_id(player_id);
        character->set_character_type(player_character_type);
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
        printf("Player joined room: %d, player_id: %d, conv: %u\n", room_id, player_id, conv);
        auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd, room_id, player_id, room);
        sessions[conv] = session;

        std::vector<int> all_players = room->getAllPlayerIds();
        for (int pid : all_players)
        {
            message::Character *character = roomMsg.add_characters();
            character->set_player_id(pid);
            if (pid != player_id)
            {
                // for room message
                character->set_character_type(room->getPlayer(pid).character_type);
            }
            else
            {
                // for room message
                message::CharacterType player_character_type = getRandomCharacterType();
                room->getPlayer(player_id).character_type = player_character_type;
                character->set_character_type(player_character_type);
            }
        }

        message::MessageWrapper wrapper_room;
        wrapper_room.mutable_room_message()->CopyFrom(roomMsg);
        session->sendMessage(wrapper_room);
        broadcastToRoom(room_id, wrapper_room, {player_id}, false);

        for (int pid : all_players)
        {
            if (pid != player_id && room->getPlayer(pid).is_ready)
            {
                // for lobby message of ready
                message::LobbyMessage lobby_message;
                lobby_message.set_player_id(pid);
                lobby_message.set_character_type(room->getPlayer(pid).character_type);
                lobby_message.set_is_ready(true);
                message::MessageWrapper wrapper_lobby_message;
                wrapper_lobby_message.mutable_lobby_message()->CopyFrom(lobby_message);
                session->sendMessage(wrapper_lobby_message);
            }
        }

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
        // 从buf中获取conv 前4个字节转成uint32_t
        uint32_t conv = *reinterpret_cast<uint32_t *>(buf);
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
    uint32_t next = 15;
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

void KcpServer::mainLoop()
{
    constexpr int MAX_EVENTS = 10;
    constexpr uint32_t GAME_TICK_INTERVAL = 10; // 游戏逻辑10ms一次
    epoll_event events[MAX_EVENTS];
    uint32_t lastGameTick = currentMs();

    while (running)
    {
        const uint32_t now = currentMs();

        // 计算下次超时时间，但不能超过游戏逻辑间隔
        int timeoutMs = calcNextTimeout();
        uint32_t nextGameTick = lastGameTick + GAME_TICK_INTERVAL;
        if (now < nextGameTick)
        {
            uint32_t gameTickDelay = nextGameTick - now;
            timeoutMs = std::min(timeoutMs, static_cast<int>(gameTickDelay));
        }

        // 处理网络事件
        const int nfds = epoll_wait(epollFd, events, MAX_EVENTS, timeoutMs);
        const uint32_t currentTime = currentMs();

        for (int i = 0; i < nfds; ++i)
        {
            if (events[i].data.fd == udpFd)
            {
                handleUdpRead();
            }
        }

        // 更新所有KCP会话
        for (auto &session : sessions | std::views::values)
        {
            session->update(currentTime);
            session->recvAll();
        }

        // 检查是否需要执行游戏逻辑
        if (currentTime >= lastGameTick + GAME_TICK_INTERVAL)
        {
            gameLogicTick((currentTime - lastGameTick) / 1000.0f);
            lastGameTick = currentTime;
        }

        for (auto &session : sessions | std::views::values)
        {
            session->update(currentMs());
        }
    }
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
        constexpr uint32_t GAME_TICK_INTERVAL = 15;
        if (const uint32_t now = currentMs(); now >= lastGameTick + GAME_TICK_INTERVAL)
        {
            gameLogicTick((now - lastGameTick) / 1000.0f);
            lastGameTick = now;
        }
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
    }
}
