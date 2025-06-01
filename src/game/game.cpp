#include <game/game.h>

void Game::setPlayerPosition(int player_id, const Vector2& position) {
            player_positions_[player_id] = position;
}