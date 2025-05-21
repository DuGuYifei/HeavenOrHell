#include "room/room.h"

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
    if (players_.find(player_id) != players_.end()) {
        return false;
    }
    
    players_[player_id] = conv;
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
    std::lock_guard<std::mutex> lock(const_cast<std::mutex&>(player_mutex_));
    
    auto it = players_.find(player_id);
    if (it == players_.end()) {
        return -1;  // Return -1 for invalid player
    }
    
    return it->second;
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