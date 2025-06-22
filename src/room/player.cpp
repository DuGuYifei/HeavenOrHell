#include "player.h"

// Currently, all Player methods are simple enough to be inlined in player.h.
// If Player methods (like constructors or a non-defaulted destructor)
// become more complex, their implementations would go here.

void Player::weak_update(float delta_time)
{
    if (character_state == message::CharacterState::Character_STATE_WEAK)
    {
        weak_timer += delta_time;
        if (weak_timer >= 60.0f)
        {
            character_state = message::CharacterState::Character_STATE_DIE;
            player_result = PlayerResult::DIE_BY_HIT;
            weak_timer = 0.0f; // 重置计时器
        }
    }
    else
    {
        weak_timer = 0.0f; // 非虚弱状态时计时器归0
    }
}

void Player::checkHp()
{
    // 如果当前是死亡状态，不进行状态改变
    if (character_state == message::CharacterState::Character_STATE_DIE)
    {
        return;
    }

    // 如果hp为0，且当前不是死亡状态，则进入虚弱状态
    if (hp <= 0.0f)
    {
        character_state = message::CharacterState::Character_STATE_WEAK;
    }
    else
    {
        // 如果hp大于0，则进入正常状态
        character_state = message::CharacterState::Character_STATE_NORMAL;
    }
}

void Player::recoverHp(float recover_amount)
{
    hp += recover_amount;
    // 确保hp不超过最大血量
    if (hp > maxHp)
    {
        hp = maxHp;
    }
    checkHp();
}

void Player::decreaseHp(float damage)
{
    hp -= damage;
    // 确保hp不小于0
    if (hp < 0.0f)
    {
        hp = 0.0f;
    }
    checkHp();
}
