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
#include "message/gen/message.pb.h"

using namespace std::chrono;

// Context passed into KCP output callback
struct KcpClientContext
{
    int sockfd;
    sockaddr_in servAddr;
};

// Client state to keep track of session
struct ClientState
{
    int room_id = 0;    // 0 means create new room, otherwise join existing
    int player_id = -1; // Assigned by server
    bool connected = false;
    bool room_joined = false;
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

// Send raw packet (for initial hello packet with conv=0)
void send_raw_packet(int sockfd, const sockaddr_in &servAddr, const char *data, int len)
{
    sendto(sockfd, data, len, 0, (sockaddr *)&servAddr, sizeof(servAddr));
}

// Send a Protobuf message to KCP
void kcp_send_proto(ikcpcb *kcp, const google::protobuf::Message &msg)
{
    std::string data;
    msg.SerializeToString(&data);
    ikcp_send(kcp, data.data(), data.size());
}

// Create a new MessageWrapper with a specific message type
void kcp_send_wrapped_proto(ikcpcb *kcp, const google::protobuf::Message &msg,
                            const google::protobuf::Descriptor *descriptor)
{
    message::MessageWrapper wrapper;

    if (descriptor == message::StringMessage::descriptor())
    {
        *(wrapper.mutable_string_message()) = dynamic_cast<const message::StringMessage &>(msg);
    }
    else if (descriptor == message::SoulBasicMessage::descriptor())
    {
        *(wrapper.mutable_soul_basic_message()) = dynamic_cast<const message::SoulBasicMessage &>(msg);
    }
    else if (descriptor == message::ReaperAttackMessage::descriptor())
    {
        *(wrapper.mutable_reaper_attack_message()) = dynamic_cast<const message::ReaperAttackMessage &>(msg);
    }
    else if (descriptor == message::PropTryGetMessage::descriptor())
    {
        *(wrapper.mutable_prop_try_get_message()) = dynamic_cast<const message::PropTryGetMessage &>(msg);
    }
    else if (descriptor == message::PropGetMessage::descriptor())
    {
        *(wrapper.mutable_prop_get_message()) = dynamic_cast<const message::PropGetMessage &>(msg);
    }
    else if (descriptor == message::ReaperAttackMessage::descriptor())
    {
        *(wrapper.mutable_reaper_attack_message()) = dynamic_cast<const message::ReaperAttackMessage &>(msg);
    }
    else
    {
        std::cerr << "Unsupported message type for wrapping!" << std::endl;
        return;
    }

    kcp_send_proto(kcp, wrapper);
}

// Send HelloMessage to create or join a room
void send_hello_message(int sockfd, const sockaddr_in &servAddr, int room_id)
{
    message::HelloMessage hello;
    hello.set_room_id(room_id);

    std::string body;
    hello.SerializeToString(&body);

    // 添加4字节conv头（值为0）
    std::vector<char> raw_pkt(body.size() + 4);
    uint32_t conv = 0;
    memcpy(raw_pkt.data(), &conv, 4); // First 4 bytes are conv=0
    memcpy(raw_pkt.data() + 4, body.data(), body.size());

    send_raw_packet(sockfd, servAddr, raw_pkt.data(), raw_pkt.size());
    std::cout << "[Client→Server] Sent HelloMessage with room_id=" << room_id << std::endl;
}

// Read and parse all complete messages from KCP
void kcp_recv_all(ikcpcb *kcp, ClientState &state)
{
    while (true)
    {
        int peek = ikcp_peeksize(kcp);
        if (peek < 0)
            break;

        std::vector<char> buf(peek);
        int n = ikcp_recv(kcp, buf.data(), peek);
        if (n <= 0)
            continue;

        // Try to parse a MessageWrapper
        message::MessageWrapper wrapper;
        if (wrapper.ParseFromArray(buf.data(), n))
        {
            if (wrapper.has_room_message())
            {
                auto &msg = wrapper.room_message();
                std::cout << "[Server→Client] RoomMessage via wrapper: "
                          << "room_id=" << msg.room_id()
                          << ", player_id=" << msg.player_id()
                          << ", is_join=" << (msg.is_join() ? "true" : "false")
                          << std::endl;
                if (msg.is_join())
                {
                    state.room_joined = true;
                    state.player_id = msg.player_id();
                }
            }
            else if (wrapper.has_string_message())
            {
                auto &msg = wrapper.string_message();
                std::cout << "[Server→Client] StringMessage via wrapper, type="
                          << msg.message_type() << std::endl;
                if (msg.message_type() == message::StringMessageType::MAZE_MAP)
                {
                    std::cout << "[Server→Client] MazeMap via wrapper: "
                              << msg.message_content() << std::endl;
                }
            }
            else if (wrapper.has_soul_basic_message())
            {
                auto &msg = wrapper.soul_basic_message();
                std::cout << "[Server→Client] SoulBasicMessage via wrapper: "
                          << "player_id=" << msg.player_id()
                          << ", pos=(" << msg.position_x() << "," << msg.position_y() << ")"
                          << std::endl;
            }
            // Handle any other message types that are in the MessageWrapper
            // but not explicitly handled above
            else if (wrapper.has_prop_try_get_message())
            {
                auto &msg = wrapper.prop_try_get_message();
                std::cout << "[Server→Client] PropTryGetMessage via wrapper: "
                          << "player_id=" << msg.player_id()
                          << ", prop_id=" << msg.prop_id()
                          << std::endl;
            }
            else if (wrapper.has_prop_get_message())
            {
                auto &msg = wrapper.prop_get_message();
                std::cout << "[Server→Client] PropGetMessage via wrapper: "
                          << "player_id=" << msg.player_id()
                          << ", prop_id=" << msg.prop_id()
                          << std::endl;
            }
            else if (wrapper.has_reaper_attack_result_message())
            {
                auto &msg = wrapper.reaper_attack_result_message();
                std::cout << "[Server→Client] ReaperAttackResultMessage via wrapper: "
                          << "player_id=" << msg.soul_player_id()
                          << ", is_hit=" << (msg.is_hit() ? "true" : "false")
                          << std::endl;
            }
            continue;
        }

        std::cout << "[Server→Client] Unknown message of length " << n << std::endl;
    }
}

int main(int argc, char **argv)
{
    if (argc < 3 || argc > 4)
    {
        std::cerr << "Usage: " << argv[0] << " <server_ip> <server_port> [room_id]\n";
        return 1;
    }
    GOOGLE_PROTOBUF_VERIFY_VERSION;

    const char *serv_ip = argv[1];
    int serv_port = std::atoi(argv[2]);

    // Optional room_id to join (0 = create new room)
    int room_id = 0;
    if (argc == 4)
    {
        room_id = std::atoi(argv[3]);
    }

    ClientState state;
    state.room_id = room_id;

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

    // 3. Send HelloMessage to create/join room (with conv=0)
    send_hello_message(sockfd, servAddr, room_id);

    // 4. Main loop: Wait for server response, establish KCP session once we have a conv
    uint32_t ts_start = now_ms();
    ikcpcb *kcp = nullptr;
    bool move_sent = false;
    bool attack_sent = false;

    // Buffer for incoming UDP packets
    char buf[4096];
    sockaddr_in raddr{};
    socklen_t rlen = sizeof(raddr);

    while (true)
    {
        uint32_t ts = now_ms();

        // If we already have a KCP session, update it
        if (kcp)
        {
            ikcp_update(kcp, ts);
        }

        // Receive UDP packets
        int n = recvfrom(sockfd, buf, sizeof(buf), 0, (sockaddr *)&raddr, &rlen);

        if (n > 0)
        {
            if (!kcp && n >= 8)
            {
                // If we don't have a KCP session yet, check if this is a response with a conv
                uint32_t conv;
                memcpy(&conv, buf, 4);

                if (conv != 0)
                {
                    // We got a conv from server, initialize KCP
                    std::cout << "Received conv=" << conv << " from server, establishing KCP session" << std::endl;
                    kcp = ikcp_create(conv, &ctx);
                    ikcp_nodelay(kcp, 1, 10, 2, 1);
                    ikcp_wndsize(kcp, 128, 128);
                    ikcp_setoutput(kcp, kcp_output);
                    state.connected = true;

                    // Feed this initial packet to KCP
                    ikcp_input(kcp, buf, n);
                }
            }
            else if (kcp)
            {
                // Normal packet for existing KCP session
                ikcp_input(kcp, buf, n);
            }
        }

        // Process all messages from KCP
        if (kcp)
        {
            kcp_recv_all(kcp, state);

            // After joining room, send move and attack messages
            if (state.room_joined)
            {
                // Send Move message 1 second after joining
                if (!move_sent && ts - ts_start > 1000)
                {
                    message::SoulBasicMessage move;
                    move.set_player_id(state.player_id);
                    move.set_position_x(1.0f);
                    move.set_position_y(2.0f);
                    move.set_hp(100.0f);
                    move.set_max_hp(100.0f);
                    std::cout << "[Client→Server] Sending SoulBasicMessage\n";
                    kcp_send_wrapped_proto(kcp, move, message::SoulBasicMessage::descriptor());
                    move_sent = true;
                }

                // Send Attack message 2 seconds after joining
                if (move_sent && !attack_sent && ts - ts_start > 2000)
                {
                    message::ReaperAttackMessage attack;
                    attack.set_soul_player_id(state.player_id == 0 ? 1 : 0); // Target another player
                    attack.set_skill_id(7);
                    std::cout << "[Client→Server] Sending ReaperAttackMessage\n";
                    kcp_send_wrapped_proto(kcp, attack, message::ReaperAttackMessage::descriptor());
                    attack_sent = true;
                }
            }
        }
        else
        {
            // If we haven't received a response in 3 seconds, resend hello
            if (ts - ts_start > 3000)
            {
                std::cout << "No response from server, resending HelloMessage" << std::endl;
                send_hello_message(sockfd, servAddr, room_id);
                ts_start = ts;
            }
        }

        std::this_thread::sleep_for(std::chrono::milliseconds(20));
    }

    if (kcp)
    {
        ikcp_release(kcp);
    }

    google::protobuf::ShutdownProtobufLibrary();
    close(sockfd);
    return 0;
}
