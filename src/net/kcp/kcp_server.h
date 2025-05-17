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
#include "ikcp.h"
#include "message.pb.h" // Protobuf 生成的头

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
        ikcp_nodelay(kcp, 1, 10, 2, 1);
        kcp->rx_minrto = 10;
        ikcp_wndsize(kcp, 128, 128);
        ikcp_setoutput(kcp, &KcpSession::kcpOutput);

        udpSocket = udpFd;
    }

    ~KcpSession()
    {
        if (kcp)
            ikcp_release(kcp);
    }

    // 定时调用
    void update(uint32_t nowMs)
    {
        ikcp_update(kcp, nowMs);
    }

    // 收到 UDP 数据后输入
    void input(const char *data, int len)
    {
        ikcp_input(kcp, data, len);
    }

    // 从 kcp 中读取所有完整消息并分发
    template <typename F>
    void recvAll(F &&onMessage)
    {
        while (true)
        {
            int peek = ikcp_peeksize(kcp);
            if (peek < 0)
                break; // 没有完整包
            std::vector<char> buf(peek);
            int n = ikcp_recv(kcp, buf.data(), peek);
            if (n <= 4)
                continue; // 不可能
            uint32_t msgLen = 0;
            // 小端解析前 4 字节长度
            msgLen = (uint8_t)buf[0] | ((uint8_t)buf[1] << 8) | ((uint8_t)buf[2] << 16) | ((uint8_t)buf[3] << 24);
            if (msgLen != (uint32_t)n - 4)
                continue;

            // 反序列化 Protobuf
            message::MoveMessage moveMsg;
            message::AttackMessage atkMsg;
            // 你可以按实际类型尝试解析
            if (moveMsg.ParseFromArray(buf.data() + 4, msgLen))
            {
                onMessage(conv, moveMsg);
            }
            else if (atkMsg.ParseFromArray(buf.data() + 4, msgLen))
            {
                onMessage(conv, atkMsg);
            }
            // …其他类型
        }
    }

    // 发送任意 Protobuf 消息（带 4 字节长度前缀）
    void sendMessage(const google::protobuf::Message &msg)
    {
        std::string data;
        msg.SerializeToString(&data);
        uint32_t len = data.size();
        std::vector<char> packet(4 + len);
        packet[0] = len & 0xFF;
        packet[1] = (len >> 8) & 0xFF;
        packet[2] = (len >> 16) & 0xFF;
        packet[3] = (len >> 24) & 0xFF;
        memcpy(packet.data() + 4, data.data(), len);
        ikcp_send(kcp, packet.data(), packet.size());
    }

private:
    int udpSocket;

    // ikcp_output 回调：通过 UDP 发包
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
        : listenPort(port)
    {
        initSocket();
        initEpoll();
    }

    ~KcpServer()
    {
        close(epollFd);
        close(udpFd);
    }

    // 启动主循环（阻塞）
    void run()
    {
        const int MAX_EVENTS = 10;
        epoll_event events[MAX_EVENTS];

        while (true)
        {
            // 计算距离下一次 kcp 更新的最小超时时间
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

            // 2. Update all sessions
            for (auto &kv : sessions)
            {
                kv.second->update(now);
                // Read complete messages from kcp
                kv.second->recvAll(onClientMessage);
            }
        }
    }

    // 用户注册回调：收到客户端消息
    template <typename F>
    void setMessageCallback(F &&cb)
    {
        onClientMessage = std::forward<F>(cb);
    }

    // 用户注册回调：新连接建立
    template <typename F>
    void setConnectionCallback(F &&cb)
    {
        onConnection = std::forward<F>(cb);
    }

    void sendTo(uint32_t conv, const google::protobuf::Message &msg)
    {
        auto it = sessions.find(conv);
        if (it != sessions.end())
        {
            it->second->sendMessage(msg);
        }
    }

private:
    uint16_t listenPort;
    int udpFd = -1;
    int epollFd = -1;

    // conv -> Session
    std::unordered_map<uint32_t, std::shared_ptr<KcpSession>> sessions;

    // 回调：conv + protobuf 消息
    std::function<void(uint32_t, const google::protobuf::Message &)> onClientMessage;
    // 回调：新连接建立
    std::function<void(uint32_t)> onConnection;

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
            if (n < 4)
                continue;

            // 解析 KCP 会话 ID
            uint32_t conv = ikcp_getconv(buf);
            auto it = sessions.find(conv);
            if (it == sessions.end())
            {
                // 新会话
                auto session = std::make_shared<KcpSession>(conv, cliAddr, udpFd);
                sessions[conv] = session;
                if (onConnection) {
                    onConnection(conv);
                }
                it = sessions.find(conv);
                printf("New session conv=%u addr=%s:%d\n",
                       conv, inet_ntoa(cliAddr.sin_addr), ntohs(cliAddr.sin_port));
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
        uint32_t next = 100; // 最多 100ms
        for (auto &kv : sessions)
        {
            uint32_t ts = ikcp_check(kv.second->kcp, now);
            uint32_t diff = ts > now ? ts - now : 0;
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
};
