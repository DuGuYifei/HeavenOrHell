#include "room/room.h"
#include <stdexcept> // For std::runtime_error in getPlayer

Room::Room(int room_id) : room_id_(room_id), next_player_id_(0) {
    maze_map.generate();
}

int Room::getRoomId() const {
    return room_id_;
}

int Room::getNextPlayerId() {
    // Atomically increments and returns the previous value
    return next_player_id_++;
}

bool Room::addPlayer(int player_id, int conv) {
    std::lock_guard<std::mutex> lock(player_mutex_);
    
    // Check if player already exists
    if (players_.count(player_id)) { // .count is fine for checking existence
        return false;
    }
    
    // Create Player object using std::make_unique and emplace it
    players_.emplace(player_id, std::make_unique<Player>(conv)); 
    return true;
}

bool Room::removePlayer(int player_id) {
    std::lock_guard<std::mutex> lock(player_mutex_);
    
    auto it = players_.find(player_id);
    if (it == players_.end()) {
        return false;
    }
    
    players_.erase(it);
    return true;
}

bool Room::hasPlayer(int player_id) const {
    std::lock_guard<std::mutex> lock(const_cast<std::mutex&>(player_mutex_));
    return players_.find(player_id) != players_.end();
}

size_t Room::getPlayerCount() const {
    std::lock_guard<std::mutex> lock(const_cast<std::mutex&>(player_mutex_));
    return players_.size();
}

MazeMap Room::getMazeMap() {
    return maze_map;
}

int Room::getPlayerConv(int player_id) const {
    std::lock_guard<std::mutex> lock(player_mutex_); 
    
    auto it = players_.find(player_id);
    if (it == players_.end()) {
        return -1;  // Return -1 for invalid player
    }
    
    return it->second->conv_id; // Access conv_id via unique_ptr
}

std::vector<int> Room::getAllPlayerIds() const {
    std::lock_guard<std::mutex> lock(const_cast<std::mutex&>(player_mutex_));
    
    std::vector<int> player_ids;
    player_ids.reserve(players_.size());
    
    for (const auto& pair : players_) {
        player_ids.push_back(pair.first);
    }
    
    return player_ids;
}

Player& Room::getPlayer(int player_id) {
    std::lock_guard<std::mutex> lock(player_mutex_);
    auto it = players_.find(player_id);
    if (it == players_.end()) {
        throw std::runtime_error("Player not found in getPlayer (non-const)");
    }
    return *(it->second); // Dereference unique_ptr to get Player&
}

const Player& Room::getPlayer(int player_id) const {
    std::lock_guard<std::mutex> lock(player_mutex_); 
    auto it = players_.find(player_id);
    if (it == players_.end()) {
        throw std::runtime_error("Player not found in getPlayer (const)");
    }
    return *(it->second); // Dereference unique_ptr to get const Player&
}