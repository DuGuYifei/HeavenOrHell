#include <iostream>
#include <cstring>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <string>
#include <chrono>
#include <stdexcept>
#include "src/message/gen/message.pb.h"
#include "src/net/kcp/ikcp.h"

class SimpleKcpClient
{
private:
    int sockfd;
    struct sockaddr_in serverAddr;
    ikcpcb *kcp;
    uint32_t conv;

    static int udp_output(const char *buf, int len, ikcpcb *kcp, void *user)
    {
        SimpleKcpClient *client = static_cast<SimpleKcpClient *>(user);
        return client->udpOutput(buf, len);
    }

    int udpOutput(const char *buf, int len)
    {
        return sendto(sockfd, buf, len, 0, (struct sockaddr *)&serverAddr, sizeof(serverAddr));
    }

public:
    SimpleKcpClient(const std::string &host, int port) : conv(0)
    {
        // Create UDP socket
        sockfd = socket(AF_INET, SOCK_DGRAM, 0);
        if (sockfd < 0)
        {
            throw std::runtime_error("Failed to create socket");
        }

        // Setup server address
        memset(&serverAddr, 0, sizeof(serverAddr));
        serverAddr.sin_family = AF_INET;
        serverAddr.sin_port = htons(port);
        inet_pton(AF_INET, host.c_str(), &serverAddr.sin_addr);
    }

    ~SimpleKcpClient()
    {
        if (kcp)
        {
            ikcp_release(kcp);
        }
        close(sockfd);
    }

    bool connectWithRoomId(int room_id)
    {
        // Create HelloMessage
        message::HelloMessage helloMsg;
        helloMsg.set_room_id(room_id);

        // Serialize message
        std::string serialized;
        if (!helloMsg.SerializeToString(&serialized))
        {
            std::cerr << "Failed to serialize HelloMessage" << std::endl;
            return false;
        }

        // Prepare buffer with conv=0 (HelloMessage identifier)
        char buffer[4096];
        uint32_t hello_conv = 0;
        memcpy(buffer, &hello_conv, 4);
        memcpy(buffer + 4, serialized.data(), serialized.size());

        // Send HelloMessage
        int sent = sendto(sockfd, buffer, 4 + serialized.size(), 0,
                          (struct sockaddr *)&serverAddr, sizeof(serverAddr));

        if (sent < 0)
        {
            perror("Failed to send HelloMessage");
            return false;
        }

        std::cout << "Sent HelloMessage with room_id=" << room_id << std::endl;

        // Receive response
        char recvBuf[4096];
        struct sockaddr_in fromAddr;
        socklen_t fromLen = sizeof(fromAddr);

        int received = recvfrom(sockfd, recvBuf, sizeof(recvBuf), 0,
                                (struct sockaddr *)&fromAddr, &fromLen);

        if (received < 0)
        {
            perror("Failed to receive response");
            return false;
        }

        if (received < 4)
        {
            std::cerr << "Invalid response size" << std::endl;
            return false;
        }

        // Extract conv from response
        memcpy(&conv, recvBuf, 4);

        // Initialize KCP with received conv
        kcp = ikcp_create(conv, this);
        ikcp_setoutput(kcp, udp_output);
        ikcp_nodelay(kcp, 1, 10, 2, 1);
        ikcp_wndsize(kcp, 128, 128);

        // Process KCP data
        ikcp_input(kcp, recvBuf, received);

        // Try to receive data from KCP
        char kcpBuf[4096];
        int kcpLen = ikcp_recv(kcp, kcpBuf, sizeof(kcpBuf));

        if (kcpLen > 0)
        {
            // Parse RoomMessage
            message::MessageWrapper wrapper;
            if (wrapper.ParseFromArray(kcpBuf, kcpLen))
            {
                if (wrapper.has_room_message())
                {
                    const auto &roomMsg = wrapper.room_message();
                    std::cout << "Received RoomMessage:" << std::endl;
                    std::cout << "  is_join: " << (roomMsg.is_join() ? "true" : "false") << std::endl;
                    std::cout << "  room_id: " << roomMsg.room_id() << std::endl;
                    std::cout << "  player_id: " << roomMsg.player_id() << std::endl;
                    std::cout << "  characters count: " << roomMsg.characters_size() << std::endl;

                    for (const auto &character : roomMsg.characters())
                    {
                        std::cout << "    player_id: " << character.player_id()
                                  << ", character_type: " << character.character_type() << std::endl;
                    }

                    return roomMsg.is_join();
                }
            }
        }

        return false;
    }

    void sendLobbyMessage(int player_id, bool is_ready, message::CharacterType char_type)
    {
        if (!kcp)
        {
            std::cerr << "Not connected" << std::endl;
            return;
        }

        message::LobbyMessage lobbyMsg;
        lobbyMsg.set_player_id(player_id);
        lobbyMsg.set_is_ready(is_ready);
        lobbyMsg.set_character_type(char_type);

        message::MessageWrapper wrapper;
        wrapper.mutable_lobby_message()->CopyFrom(lobbyMsg);

        std::string serialized;
        if (wrapper.SerializeToString(&serialized))
        {
            ikcp_send(kcp, serialized.data(), serialized.size());
            ikcp_update(kcp, getCurrentMs());
            std::cout << "Sent LobbyMessage: player_id=" << player_id
                      << ", is_ready=" << (is_ready ? "true" : "false")
                      << ", character_type=" << char_type << std::endl;
        }
    }

    uint32_t getCurrentMs()
    {
        auto now = std::chrono::steady_clock::now();
        return std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()).count();
    }
};

void printUsage(const char *program_name)
{
    std::cout << "Usage: " << program_name << " [room_id]" << std::endl;
    std::cout << "  room_id: Room ID to join (default: 0 to create new room)" << std::endl;
    std::cout << "Examples:" << std::endl;
    std::cout << "  " << program_name << "        # Create new room" << std::endl;
    std::cout << "  " << program_name << " 1001   # Join room 1001" << std::endl;
}

int main(int argc, char *argv[])
{
    // Parse command line arguments
    int room_id = 0; // Default to 0 (create new room)

    if (argc == 2)
    {
        try
        {
            room_id = std::stoi(argv[1]);
        }
        catch (const std::exception &e)
        {
            std::cerr << "Invalid room_id: " << argv[1] << std::endl;
            printUsage(argv[0]);
            return 1;
        }
    }
    else if (argc > 2)
    {
        std::cerr << "Too many arguments" << std::endl;
        printUsage(argv[0]);
        return 1;
    }

    try
    {
        SimpleKcpClient client("127.0.0.1", 8888);

        std::cout << "Connecting to server with room_id=" << room_id << std::endl;

        if (client.connectWithRoomId(room_id))
        {
            std::cout << "Successfully connected!" << std::endl;

            // Simulate lobby interaction
            std::cout << "Sending lobby message..." << std::endl;
            client.sendLobbyMessage(1, true, message::CharacterType::SOUL_DOG);

            // Keep connection alive for a bit
            sleep(2);
        }
        else
        {
            std::cout << "Failed to connect or join room" << std::endl;
            return 1;
        }
    }
    catch (const std::exception &e)
    {
        std::cerr << "Error: " << e.what() << std::endl;
        return 1;
    }

    return 0;
}