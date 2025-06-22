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
    message::CharacterState character_state;
    bool is_ready;
    bool is_start_rec_game_msg; // 防止疯狂给对方重发信息，但对方不收，会导致网络变卡 / Prevent sending too many messages to the opponent, but the opponent does not receive, resulting in a heavy network
    float weak_timer;           // 虚弱状态计时器，60秒后死亡

    // Constructors
    Player()
        : conv_id(-1), position{}, hp(100.0f), maxHp(100.0f), character_type(message::CharacterType::SOUL_DOG), animation_type(message::PlayerAnimationType::IDLE), character_state(message::CharacterState::Character_STATE_NORMAL), is_ready(false), is_start_rec_game_msg(false), weak_timer(0.0f) {}

    explicit Player(int p_conv_id)
        : conv_id(p_conv_id), position{}, hp(100.0f), maxHp(100.0f), character_type(message::CharacterType::SOUL_DOG), animation_type(message::PlayerAnimationType::IDLE), character_state(message::CharacterState::Character_STATE_NORMAL), is_ready(false), is_start_rec_game_msg(false), weak_timer(0.0f) {}

    Player(int p_conv_id, Position p_pos, float p_hp, float p_maxHp, message::CharacterType p_char_type, bool p_is_ready)
        : conv_id(p_conv_id), position(p_pos), hp(p_hp), maxHp(p_maxHp), character_type(p_char_type), animation_type(message::PlayerAnimationType::IDLE), character_state(message::CharacterState::Character_STATE_NORMAL), is_ready(p_is_ready), is_start_rec_game_msg(false), weak_timer(0.0f) {}

    // Virtual destructor to allow for inheritance
    virtual ~Player() = default;

    // 虚弱状态更新函数，当player是weak状态时开始计时，持续60秒后死亡
    void weak_update(float delta_time);

    // HP相关函数
    void checkHp();            // 检查HP状态并更新character_state
    void recoverHp(float hp);  // 恢复血量
    void decreaseHp(float hp); // 减少血量
};
