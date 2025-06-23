#include "kcp_session.h"

#include "room/room_manager.h"

KcpSession::KcpSession(const uint32_t _conv, const sockaddr_in &addr, const int udpFd, const int roomId, const int playerId, std::shared_ptr<Room> room)
    : conv(_conv), peerAddr(addr), roomId(roomId), playerId(playerId), room(std::move(room))
{
    kcp = ikcp_create(conv, this);
    ikcp_nodelay(kcp, 1, 15, 1, 0);
    kcp->rx_minrto = 15;
    ikcp_wndsize(kcp, 32 * 4 * 32, 32 * 4 * 32);
    // kcp->logmask = IKCP_LOG_OUTPUT | IKCP_LOG_INPUT;
    // kcp->writelog = [](const char *s, ikcpcb *, void *)
    // {
    //     printf("%s\n", s);
    // };
    ikcp_setoutput(kcp, &KcpSession::kcpOutput);
    udpSocket = udpFd;
}

KcpSession::~KcpSession()
{
    if (kcp)
        ikcp_release(kcp);
}

void KcpSession::update(const uint32_t nowMs) const
{
    ikcp_update(kcp, nowMs);
}

int KcpSession::kcpOutput(const char *buf, int len, ikcpcb *, void *user)
{
    auto *session = static_cast<KcpSession *>(user);
    int ret = static_cast<int>(sendto(session->udpSocket, buf, len, 0, reinterpret_cast<sockaddr *>(&session->peerAddr), sizeof(session->peerAddr)));
    if (ret < 0)
    {
        if (errno == EAGAIN || errno == EWOULDBLOCK)
        {
            printf("sendto: EAGAIN or EWOULDBLOCK\n");
            return 0; // 告诉 KCP：不是致命错误，稍后再试
        }
        perror("sendto"); // 其他错误打印出来
        return -1;        // 返回 -1 让你在日志里能看到
    }
    return ret;
}

void KcpSession::recvAll() const
{
    while (true)
    {
        const int peek = ikcp_peeksize(kcp);
        if (peek < 0)
            break; // 没有完整包 / No complete packet

        std::vector<char> buf(peek);
        const int n = ikcp_recv(kcp, buf.data(), peek);
        if (n <= 0)
            continue;

        // 解析Protobuf消息 / Parse Protobuf message
        if (auto msg = std::make_unique<message::MessageWrapper>(); msg->ParseFromArray(buf.data(), n))
        {
            room->client_message_queue_.enqueue(ClientMessageEvent(conv, roomId, playerId, std::move(msg)));
        }
    }
}
