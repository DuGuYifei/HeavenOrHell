#pragma once

#include <mutex>
#include <vector>
#include <unordered_map>
#include <memory>
#include <atomic>
#include "map/maze_map.h"
#include "game/game.h"

class Room {
private:
    int room_id_;                        // Room number (starts from 1015)
    std::mutex player_mutex_;            // Mutex for player operations
    std::atomic<int> next_player_id_;    // Next player ID (starts from 0)
    std::unordered_map<int, int> players_; // Map of player_id to conv (connection id)
    MazeMap maze_map = MazeMap(31, 31);
    Game game_; // Game instance for this room
    
public:
    explicit Room(int room_id);
    ~Room() = default;

    // Get unique room ID
    int getRoomId() const;

    // Get next player ID thread-safely
    int getNextPlayerId();

    // Get maze map
    MazeMap getMazeMap();
    
    // Add a player to the room with their connection ID
    bool addPlayer(int player_id, int conv);
    
    // Remove a player from the room
    bool removePlayer(int player_id);
    
    // Check if a player is in the room
    bool hasPlayer(int player_id) const;
    
    // Get the number of players in the room
    size_t getPlayerCount() const;
    
    // Get the connection ID for a player
    int getPlayerConv(int player_id) const;
    
    // Get all player IDs in the room
    std::vector<int> getAllPlayerIds() const;
}; 