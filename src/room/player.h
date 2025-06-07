#pragma once

#include "message/gen/message.pb.h" // For message::CharacterType

// Position data structure
struct Position {
    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;
    // Add constructors or helper methods if needed
};

// Player base class
class Player {
public:
    int conv_id;
    Position position;
    int health;
    message::CharacterType character_type;
    bool is_ready;

    // Constructors
    Player()
        : conv_id(-1), position{}, health(100), character_type(message::CharacterType::SOUL_DOG), is_ready(false) {}

    explicit Player(int p_conv_id)
        : conv_id(p_conv_id), position{}, health(100), character_type(message::CharacterType::SOUL_DOG), is_ready(false) {}

    Player(int p_conv_id, Position p_pos, int p_health, message::CharacterType p_char_type, bool p_is_ready)
        : conv_id(p_conv_id), position(p_pos), health(p_health), character_type(p_char_type), is_ready(p_is_ready) {}

    // Virtual destructor to allow for inheritance
    virtual ~Player() = default;
};
