#ifndef KCP_SESSION_H
#define KCP_SESSION_H

#include <netinet/in.h>
#include <vector>
#include <chrono>
#include <mutex>
#include "ikcp.h"
#include "message/gen/message.pb.h"

class KcpSession
{
public:
    uint32_t conv;
    ikcpcb *kcp = nullptr;
    sockaddr_in peerAddr;
    std::mutex kcp_mutex;

    KcpSession(uint32_t _conv, const sockaddr_in &addr, int udpFd);
    ~KcpSession();
    // 定时调用 / Called periodically
    void update(uint32_t nowMs) const;

    // 收到UDP数据后输入 / Input after receiving UDP data
    void input(const char *data, const int len) const
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
        std::lock_guard<std::mutex> lock(kcp_mutex);
        ikcp_send(kcp, data.data(), static_cast<int>(data.size()));
    }

private:
    int udpSocket;

    // ikcp_output回调：通过UDP发包 / ikcp_output callback: send packets via UDP
    static int kcpOutput(const char *buf, int len, ikcpcb *, void *user);
};




#endif //KCP_SESSION_H
