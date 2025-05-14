#ifndef MESSAGE_TYPE_H
#define MESSAGE_TYPE_H

enum class ClientMessageType {
    MOVE = 1,
    ATTACK = 2
};

enum class ServerMessageType {
    PLAYER_BASIC = 1,
    PROP_GET = 2
};

#endif // MESSAGE_TYPE_H 