// KcpServer.h

#pragma once


#include <netinet/in.h>
#include <functional>
#include <unordered_map>
#include <vector>
#include <memory>
#include <random>
#include <mutex>
#include <thread>
#include <atomic>
#include "kcp_session.h"
#include "message/gen/message.pb.h"
#include "room/room_manager.h"

// 定义KCP相关常量 / Define KCP related constants
#define KCP_HEADER_SIZE 24 // KCP头部大小 / KCP header size


class KcpServer
{
public:
    explicit KcpServer(uint16_t port);
    ~KcpServer();
    // 启动主循环（阻塞） / Start main loop (blocking)
    void run();

    // 用户注册回调：收到客户端消息 / User register callback: receive client message
    template <typename F>
    void setMessageCallback(F &&cb)
    {
        onClientMessage = std::forward<F>(cb);
    }

    void sendTo(uint32_t conv, const google::protobuf::Message &msg);
    // 向指定房间广播消息 / Broadcast message to specified room
    void broadcastToRoom(int room_id, const google::protobuf::Message &msg,
                         const std::vector<int> &skip_player_ids = {});
    void gameLogicTick(uint32_t now);
    void updateAllRooms(uint32_t now);
    void broadcastAllRooms(uint32_t now);
    void updateRoomLogic(std::shared_ptr<Room> room, uint32_t now);

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
    uint32_t generateConv();
    void initSocket();
    void initEpoll();
    // 随机生成角色类型 / Randomly generate character type
    message::CharacterType getRandomCharacterType();
    void handleHello(const char *buf, int len, const sockaddr_in &cliAddr);
    void handleUdpRead();
    // Calculate how many milliseconds until the next kcp update
    int calcNextTimeout();
    static uint32_t currentMs();
    // 网络处理线程 / Network handling thread
    void networkThreadFunc();
    // 游戏逻辑线程 / Game logic thread
    void gameThreadFunc();

    std::thread networkThread;
    std::thread gameThread;
    std::atomic<bool> running;
};
