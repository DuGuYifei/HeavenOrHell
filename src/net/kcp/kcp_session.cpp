#include "kcp_session.h"

KcpSession::KcpSession(const uint32_t _conv, const sockaddr_in &addr, const int udpFd)
    : conv(_conv), peerAddr(addr)
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
