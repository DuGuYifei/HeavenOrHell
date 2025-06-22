#pragma once

#include "message/gen/message.pb.h" // For message::CharacterType

// Position data structure
struct Position
{
    float x = 0.0f;
    float y = 0.0f;
    // Add constructors or helper methods if needed
};

// Player base class
class Player
{
public:
    int conv_id;
    Position position;
    float hp;
    float maxHp;
    message::CharacterType character_type;
    message::PlayerAnimationType animation_type;
    bool is_ready;
    bool is_start_rec_game_msg; // 防止疯狂给对方重发信息，但对方不收，会导致网络变卡 / Prevent sending too many messages to the opponent, but the opponent does not receive, resulting in a heavy network

    // Constructors
    Player()
        : conv_id(-1), position{}, hp(100.0f), maxHp(100.0f), character_type(message::CharacterType::SOUL_DOG), animation_type(message::PlayerAnimationType::IDLE), is_ready(false), is_start_rec_game_msg(false) {}

    explicit Player(int p_conv_id)
        : conv_id(p_conv_id), position{}, hp(100.0f), maxHp(100.0f), character_type(message::CharacterType::SOUL_DOG), animation_type(message::PlayerAnimationType::IDLE), is_ready(false), is_start_rec_game_msg(false) {}

    Player(int p_conv_id, Position p_pos, float p_hp, float p_maxHp, message::CharacterType p_char_type, bool p_is_ready)
        : conv_id(p_conv_id), position(p_pos), hp(p_hp), maxHp(p_maxHp), character_type(p_char_type), animation_type(message::PlayerAnimationType::IDLE), is_ready(p_is_ready), is_start_rec_game_msg(false) {}

    // Virtual destructor to allow for inheritance
    virtual ~Player() = default;
};
