#include "room_manager.h"

#include <ranges>

// Initialize static members
RoomManager* RoomManager::instance_ = nullptr;
std::mutex RoomManager::instance_mutex_;

RoomManager::RoomManager() : next_room_id_(STARTING_ROOM_ID) {
}

RoomManager* RoomManager::getInstance() {
    // Double-checked locking pattern for thread safety
    if (instance_ == nullptr) {
        std::lock_guard<std::mutex> lock(instance_mutex_);
        instance_ = new RoomManager();
    }
    return instance_;
}

std::shared_ptr<Room> RoomManager::createRoom() {
    int room_id = next_room_id_++;
    auto room = std::make_shared<Room>(room_id);
    
    {
        std::lock_guard<std::mutex> lock(rooms_mutex_);
        rooms_[room_id] = room;
    }
    
    return room;
}

std::shared_ptr<Room> RoomManager::getRoom(int room_id) {
    std::lock_guard<std::mutex> lock(rooms_mutex_);
    
    auto it = rooms_.find(room_id);
    if (it == rooms_.end()) {
        return nullptr;
    }
    
    return it->second;
}

bool RoomManager::deleteRoom(int room_id) {
    std::lock_guard<std::mutex> lock(rooms_mutex_);
    
    auto it = rooms_.find(room_id);
    if (it == rooms_.end()) {
        return false;
    }
    
    rooms_.erase(it);
    return true;
}

bool RoomManager::hasRoom(const int room_id) const {
    std::lock_guard<std::mutex> lock(const_cast<std::mutex&>(rooms_mutex_));
    return rooms_.contains(room_id);
}

std::vector<int> RoomManager::getAllRoomIds() const {
    std::lock_guard<std::mutex> lock(const_cast<std::mutex&>(rooms_mutex_));
    
    std::vector<int> room_ids;
    room_ids.reserve(rooms_.size());
    
    for (const auto &room_id: rooms_ | std::views::keys) {
        room_ids.push_back(room_id);
    }
    
    return room_ids;
}

size_t RoomManager::getRoomCount() const {
    std::lock_guard<std::mutex> lock(const_cast<std::mutex&>(rooms_mutex_));
    return rooms_.size();
} 