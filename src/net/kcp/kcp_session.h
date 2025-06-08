#ifndef KCP_SESSION_H
#define KCP_SESSION_H

#include <netinet/in.h>
#include <vector>
#include <chrono>
#include <mutex>
#include "ikcp.h"
#include "event/client_message_event.hpp"
#include "message/gen/message.pb.h"

class KcpSession
{
public:
    uint32_t conv;
    ikcpcb *kcp = nullptr;
    sockaddr_in peerAddr;
    std::mutex kcp_mutex;
    int roomId;
    int playerId;

    KcpSession(uint32_t _conv, const sockaddr_in &addr, int udpFd, int roomId, int playerId);
    ~KcpSession();
    // 定时调用 / Called periodically
    void update(uint32_t nowMs) const;

    // 收到UDP数据后输入 / Input after receiving UDP data
    void input(const char *data, const int len) const
    {
        ikcp_input(kcp, data, len);
    }

    // 从kcp中读取所有完整消息读取成event将会被放入SPSC队列 / Read all complete messages from kcp and put them into the SPSC queue
    void recvAll() const;

    // 发送任意Protobuf消息 / Send any Protobuf message
    void sendMessage(const google::protobuf::Message &msg)
    {
        std::string data;
        msg.SerializeToString(&data);
        std::lock_guard<std::mutex> lock(kcp_mutex);
        ikcp_send(kcp, data.data(), static_cast<int>(data.size()));
    }

private:
    int udpSocket;

    // ikcp_output回调：通过UDP发包 / ikcp_output callback: send packets via UDP
    static int kcpOutput(const char *buf, int len, ikcpcb *, void *user);
};



#endif //KCP_SESSION_H
