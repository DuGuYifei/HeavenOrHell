#include "room/room.h"

#include <ranges>
#include <stdexcept> // For std::runtime_error in getPlayer
#include <random>
#include <algorithm>

Room::Room(const int room_id) : room_id_(room_id), next_player_id_(0)
{
    maze_map.generate();

    // 使用水塘算法生成4个gate：2个heaven gate，2个hell gate
    std::random_device rd;
    std::mt19937 gen(rd());

    // 创建包含2个heaven和2个hell的gate类型集合
    std::vector<message::GateType> gate_pool = {
        message::GateType::GATE_HEAVEN,
        message::GateType::GATE_HEAVEN,
        message::GateType::GATE_HELL,
        message::GateType::GATE_HELL};

    // 使用洗牌算法打乱顺序
    std::shuffle(gate_pool.begin(), gate_pool.end(), gen);

    // 分配给四个方向
    gate_types_[message::GateDirection::GATE_DIRECTION_UP] = gate_pool[0];
    gate_types_[message::GateDirection::GATE_DIRECTION_DOWN] = gate_pool[1];
    gate_types_[message::GateDirection::GATE_DIRECTION_LEFT] = gate_pool[2];
    gate_types_[message::GateDirection::GATE_DIRECTION_RIGHT] = gate_pool[3];
}

int Room::getRoomId() const
{
    return room_id_;
}

int Room::getNextPlayerId()
{
    // Atomically increments and returns the previous value
    return next_player_id_++;
}

void Room::setStartGame(const bool value)
{
    start_game_ = value;
}

bool Room::getStartGame() const
{
    return start_game_;
}

bool Room::addPlayer(int player_id, int conv)
{
    std::lock_guard<std::mutex> lock(player_mutex_);

    // Check if player already exists
    if (players_.contains(player_id))
    { // .count is fine for checking existence
        return false;
    }

    // Create Player object using std::make_unique and emplace it
    players_.emplace(player_id, std::make_unique<Player>(conv));
    return true;
}

bool Room::removePlayer(const int player_id)
{
    std::lock_guard<std::mutex> lock(player_mutex_);

    const auto it = players_.find(player_id);
    if (it == players_.end())
    {
        return false;
    }

    players_.erase(it);
    return true;
}

bool Room::hasPlayer(int player_id) const
{
    return players_.contains(player_id);
}

size_t Room::getPlayerCount() const
{
    return players_.size();
}

MazeMap Room::getMazeMap()
{
    return maze_map;
}

const std::map<message::GateDirection, message::GateType> &Room::getGateTypes() const
{
    return gate_types_;
}

int Room::getPlayerConv(const int player_id) const
{
    const auto it = players_.find(player_id);
    if (it == players_.end())
    {
        return -1; // Return -1 for invalid player
    }

    return it->second->conv_id; // Access conv_id via unique_ptr
}

std::vector<int> Room::getAllPlayerIds() const
{
    std::vector<int> player_ids;
    player_ids.reserve(players_.size());

    for (const auto &player_id : players_ | std::views::keys)
    {
        player_ids.push_back(player_id);
    }

    return player_ids;
}

Player &Room::getPlayer(const int player_id)
{
    const auto it = players_.find(player_id);
    if (it == players_.end())
    {
        throw std::runtime_error("Player not found in getPlayer (non-const)");
    }
    return *(it->second); // Dereference unique_ptr to get Player&
}

const Player &Room::getPlayer(const int player_id) const
{
    const auto it = players_.find(player_id);
    if (it == players_.end())
    {
        throw std::runtime_error("Player not found in getPlayer (const)");
    }
    return *(it->second); // Dereference unique_ptr to get const Player&
}

bool Room::canStartGame() const
{
    if (players_.size() <= 1) // Need more than one player
    {
        return false;
    }

    bool all_ready = true;
    int reaper_count = 0;
    for (const auto &player : players_ | std::views::values)
    {
        const Player &p = *(player);
        if (!p.is_ready)
        {
            all_ready = false;
            break;
        }
        if (p.character_type == message::CharacterType::REAPER)
        {
            reaper_count++;
        }
    }

    return all_ready && reaper_count == 1;
}
