#ifndef EVENT_SYSTEM_H
#define EVENT_SYSTEM_H

#include <google/protobuf/message.h>
#include <memory>
#include <vector>
#include <queue>
#include <mutex>
#include <condition_variable>

// Basic structure for events carrying client messages
struct ClientMessageEvent {
    uint32_t conv_id;
    int room_id;
    int player_id;
    std::unique_ptr<google::protobuf::Message> message; // The actual parsed message

    // Constructor
    ClientMessageEvent(uint32_t c_id, int r_id, int p_id, std::unique_ptr<google::protobuf::Message> msg)
        : conv_id(c_id), room_id(r_id), player_id(p_id), message(std::move(msg)) {}
};

// A simple thread-safe queue for events
template<typename T>
class ThreadSafeQueue {
public:
    void push(T value) {
        std::lock_guard<std::mutex> lock(mutex_);
        queue_.push(std::move(value));
        cv_.notify_one();
    }

    bool try_pop(T& value) {
        std::lock_guard<std::mutex> lock(mutex_);
        if (queue_.empty()) {
            return false;
        }
        value = std::move(queue_.front());
        queue_.pop();
        return true;
    }

    bool empty() const {
        std::lock_guard<std::mutex> lock(mutex_);
        return queue_.empty();
    }

private:
    mutable std::mutex mutex_;
    std::queue<T> queue_;
    std::condition_variable cv_; // Not strictly used for try_pop, but good for blocking pop
};

#endif // EVENT_SYSTEM_H