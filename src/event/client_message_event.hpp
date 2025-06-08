#ifndef EVENT_SYSTEM_H
#define EVENT_SYSTEM_H

#include <google/protobuf/message.h>
#include <memory>
#include <condition_variable>

// Basic structure for events carrying client messages
struct ClientMessageEvent {
    uint32_t conv_id;
    int room_id;
    int player_id;
    std::unique_ptr<google::protobuf::Message> message; // The actual parsed message

    // Constructor
    ClientMessageEvent(const uint32_t c_id, const int r_id, const int p_id, std::unique_ptr<google::protobuf::Message> msg)
        : conv_id(c_id), room_id(r_id), player_id(p_id), message(std::move(msg)) {}
};

#endif // EVENT_SYSTEM_H