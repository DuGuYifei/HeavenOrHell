#pragma once

#include <mutex>
#include <unordered_map>
#include <memory>
#include <atomic>
#include "room/room.h"

class RoomManager {
private:
    static constexpr int STARTING_ROOM_ID = 1015;  // Starting room ID
    
    std::mutex rooms_mutex_;                       // Mutex for room operations
    std::atomic<int> next_room_id_;                // Next room ID
    std::unordered_map<int, std::shared_ptr<Room>> rooms_;  // Map of room_id to Room

    // Singleton instance
    static RoomManager* instance_;
    static std::mutex instance_mutex_;

    // Private constructor for singleton
    RoomManager();

public:
    // No copy/move allowed
    RoomManager(const RoomManager&) = delete;
    RoomManager& operator=(const RoomManager&) = delete;
    RoomManager(RoomManager&&) = delete;
    RoomManager& operator=(RoomManager&&) = delete;
    
    // Get singleton instance
    static RoomManager* getInstance();
    
    // Create a new room
    std::shared_ptr<Room> createRoom();
    
    // Get a room by ID
    std::shared_ptr<Room> getRoom(int room_id);
    
    // Delete a room
    bool deleteRoom(int room_id);
    
    // Check if a room exists
    bool hasRoom(int room_id) const;
    
    // Get all room IDs
    std::vector<int> getAllRoomIds() const;
    
    // Get the number of rooms
    size_t getRoomCount() const;
}; 