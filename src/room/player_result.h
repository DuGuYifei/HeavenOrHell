#pragma once

enum class PlayerResult
{
    IN_GAME = 0,    // 玩家仍在游戏中
    DIE_BY_HIT = 1, // 被攻击死亡
    HELL = 2,       // 进入地狱门死亡
    HEAVEN = 3      // 进入天堂门逃脱
};