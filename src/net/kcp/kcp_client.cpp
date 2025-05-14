// kcp_client.cpp

#include <arpa/inet.h>
#include <fcntl.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <unistd.h>
#include <chrono>
#include <iostream>
#include <thread>
#include <vector>
#include "ikcp.h"
#include "message.pb.h"

using namespace std::chrono;

// Context passed into KCP output callback
struct KcpClientContext
{
    int sockfd;
    sockaddr_in servAddr;
};

// Helpers to get current time in ms
static uint32_t now_ms()
{
    return (uint32_t)duration_cast<milliseconds>(
               steady_clock::now().time_since_epoch())
        .count();
}

// KCP output callback: Send packets to server via UDP socket
int kcp_output(const char *buf, int len, ikcpcb * /*kcp*/, void *user)
{
    auto *ctx = static_cast<KcpClientContext *>(user);
    return sendto(ctx->sockfd, buf, len, 0,
                  (sockaddr *)&ctx->servAddr, sizeof(ctx->servAddr));
}

// Send a Protobuf message to KCP (4-byte little-endian length + body)
void kcp_send_proto(ikcpcb *kcp, const google::protobuf::Message &msg)
{
    std::string body;
    msg.SerializeToString(&body);
    uint32_t len = body.size();
    std::vector<char> pkt(4 + len);
    pkt[0] = len & 0xFF;
    pkt[1] = (len >> 8) & 0xFF;
    pkt[2] = (len >> 16) & 0xFF;
    pkt[3] = (len >> 24) & 0xFF;
    memcpy(pkt.data() + 4, body.data(), len);
    ikcp_send(kcp, pkt.data(), int(pkt.size()));
}

// Read and parse all complete messages from KCP
void kcp_recv_all(ikcpcb *kcp)
{
    while (true)
    {
        int peek = ikcp_peeksize(kcp);
        if (peek < 0)
            break;
        std::vector<char> buf(peek);
        int n = ikcp_recv(kcp, buf.data(), peek);
        if (n <= 4)
            continue;
        uint32_t mlen = (uint8_t)buf[0] | ((uint8_t)buf[1] << 8) | ((uint8_t)buf[2] << 16) | ((uint8_t)buf[3] << 24);
        if (mlen != uint32_t(n - 4))
            continue;

        // Parse two types of server messages
        message::PlayerBasicMessage pbm;
        if (pbm.ParseFromArray(buf.data() + 4, mlen))
        {
            std::cout << "[Server→Client] PlayerBasicMessage: "
                      << "player_id=" << pbm.player_id()
                      << ", pos=(" << pbm.position_x()
                      << "," << pbm.position_y() << ")"
                      << ", hp=" << pbm.hp() << "/" << pbm.max_hp()
                      << "\n";
            continue;
        }
        message::PropGetMessage pgm;
        if (pgm.ParseFromArray(buf.data() + 4, mlen))
        {
            std::cout << "[Server→Client] PropGetMessage: "
                      << "prop_id=" << pgm.prop_id()
                      << ", type=" << pgm.prop_type()
                      << ", amount=" << pgm.amount()
                      << "\n";
            continue;
        }
        std::cout << "[Server→Client] Unknown message of length " << mlen << "\n";
    }
}

int main(int argc, char **argv)
{
    if (argc != 3)
    {
        std::cerr << "Usage: " << argv[0] << " <server_ip> <server_port>\n";
        return 1;
    }
    GOOGLE_PROTOBUF_VERIFY_VERSION;

    const char *serv_ip = argv[1];
    int serv_port = std::atoi(argv[2]);

    // 1. Create UDP Socket
    int sockfd = socket(AF_INET, SOCK_DGRAM, 0);
    fcntl(sockfd, F_SETFL, O_NONBLOCK);
    sockaddr_in servAddr{};
    servAddr.sin_family = AF_INET;
    servAddr.sin_port = htons(serv_port);
    inet_pton(AF_INET, serv_ip, &servAddr.sin_addr);

    // 2. Prepare Context, pass to ikcp_create
    KcpClientContext ctx;
    ctx.sockfd = sockfd;
    ctx.servAddr = servAddr;

    // 3. Create KCP, ctx pointer will be saved in kcp->user
    // uint32_t conv = uint32_t(time(nullptr)) ^ uint32_t(getpid());
    // conv ^= (uint32_t)rand();
    uint32_t conv = 123;
    ikcpcb *kcp = ikcp_create(conv, &ctx);
    ikcp_nodelay(kcp, 1, 10, 2, 1);
    ikcp_wndsize(kcp, 128, 128);
    ikcp_setoutput(kcp, kcp_output);

    // 4. Send first packet to trigger server session creation
    {
        message::MoveMessage mv;
        mv.set_x(1.0f);
        mv.set_y(2.0f);
        std::cout << "[Client→Server] Sending MoveMessage\n";
        kcp_send_proto(kcp, mv);
    }

    uint32_t start = now_ms();
    bool sentAttack = false;

    // 5. Main loop: Update time, receive UDP packets, process KCP, send Attack after 1s
    while (true)
    {
        uint32_t ts = now_ms();
        ikcp_update(kcp, ts);

        // 收 UDP
        char buf[3000];
        sockaddr_in raddr{};
        socklen_t rlen = sizeof(raddr);
        int n = recvfrom(sockfd, buf, sizeof(buf), 0,
                         (sockaddr *)&raddr, &rlen);
        if (n > 0)
        {
            ikcp_input(kcp, buf, n);
        }

        // 处理所有完整响应
        kcp_recv_all(kcp);

        // 1s 后发第二条
        if (!sentAttack && ts - start > 1000)
        {
            message::AttackMessage atk;
            atk.set_target_id(42);
            atk.set_skill_id(7);
            std::cout << "[Client→Server] Sending AttackMessage\n";
            kcp_send_proto(kcp, atk);
            sentAttack = true;
        }

        std::this_thread::sleep_for(std::chrono::milliseconds(20));
    }

    ikcp_release(kcp);
    google::protobuf::ShutdownProtobufLibrary();
    close(sockfd);
    return 0;
}
