#include "kcp_session.h"

#include "room/room_manager.h"

KcpSession::KcpSession(const uint32_t _conv, const sockaddr_in &addr, const int udpFd, const int roomId, const int playerId, std::shared_ptr<Room> room)
    : conv(_conv), peerAddr(addr), roomId(roomId), playerId(playerId), room(std::move(room))
{
    kcp = ikcp_create(conv, this);
    ikcp_nodelay(kcp, 1, 1, 2, 1);
    kcp->rx_minrto = 10;
    ikcp_wndsize(kcp, 32 * 4, 32 * 4);
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
    return static_cast<int>(sendto(session->udpSocket, buf, len, 0, reinterpret_cast<sockaddr *>(&session->peerAddr), sizeof(session->peerAddr)));
}

void KcpSession::recvAll() const {
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
        if (auto wrapper = std::make_unique<google::protobuf::Message>(); wrapper->ParseFromArray(buf.data(), n))
        {
            room->client_message_queue_.enqueue(ClientMessageEvent(conv, roomId, playerId, std::move(wrapper)));
        }
    }
}
