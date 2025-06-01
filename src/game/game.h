#pragma once
#include "utils.h"
#include <vector>
#include <unordered_map>
#include <memory>
#include <atomic>

class Game {
    private:
        std::unordered_map<int, Vector2> player_positions_; // Maps player_id to their position

    public:
        void setPlayerPosition(int player_id, const Vector2& position);
            
};