#pragma once

#include <mutex>
#include <vector>
#include <map>
#include <memory>
#include <atomic>
#include "map/maze_map.h"
#include "player.h"
#include "event/readerwriterqueue.h"
#include "event/client_message_event.hpp"

class Room {
private:
    int room_id_;                                       // Room number (starts from 1015)
    mutable std::mutex player_mutex_;                   // Mutex for player operations
    std::atomic<int> next_player_id_;                   // Next player ID (starts from 0)
    std::atomic<bool> start_game_;                      // Atomic flag for game start
    std::map<int, std::unique_ptr<Player>> players_;    // Map of player_id to unique_ptr<Player>
    MazeMap maze_map = MazeMap(31, 31);

public:
    moodycamel::ReaderWriterQueue<ClientMessageEvent> client_message_queue_;

    explicit Room(int room_id);
    ~Room() = default;

    // Get unique room ID
    int getRoomId() const;

    // Get next player ID thread-safely
    int getNextPlayerId();

    // Set start game flag
    void setStartGame(bool value);

    // Get start game flag
    bool getStartGame() const;

    // Get maze map
    MazeMap getMazeMap();

    int getPlayerConv(int player_id) const;

    // Add a player to the room with their connection ID
    bool addPlayer(int player_id, int conv);
    
    // Remove a player from the room
    bool removePlayer(int player_id);
    
    // Check if a player is in the room
    bool hasPlayer(int player_id) const;
    
    // Get the number of players in the room
    size_t getPlayerCount() const;
    
    // Get the player object for a player
    Player& getPlayer(int player_id);
    const Player& getPlayer(int player_id) const;
    
    // Get all player IDs in the room
    std::vector<int> getAllPlayerIds() const;
}; 