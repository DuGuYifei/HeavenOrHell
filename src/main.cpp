#include "net/kcp/kcp_server.h"

int main()
{
    GOOGLE_PROTOBUF_VERIFY_VERSION;

    KcpServer server(8888);

    server.setMessageCallback([&server](uint32_t conv, const google::protobuf::Message &msg)
                              {
        // Compare message types using Descriptor
        const auto* desc = msg.GetDescriptor();

        if (desc == message::MoveMessage::descriptor()) {
            // 1. Handle MoveMessage
            auto& mv = static_cast<const message::MoveMessage&>(msg);
            printf("[conv=%u] Move to (%.2f, %.2f)\n", conv, mv.x(), mv.y());

            // Construct response: PlayerBasicMessage
            message::PlayerBasicMessage reply;
            reply.set_player_id(conv);
            reply.set_position_x(mv.x());
            reply.set_position_y(mv.y());
            reply.set_hp(100);      // Assume full health
            reply.set_max_hp(100);

            server.sendTo(conv, reply);
        }
        else if (desc == message::AttackMessage::descriptor()) {
            // 2. Handle AttackMessage
            auto& atk = static_cast<const message::AttackMessage&>(msg);
            printf("[conv=%u] Attack target %d with skill %d\n",
                   conv, atk.target_id(), atk.skill_id());

            // Construct response: PropGetMessage (simulate getting an item)
            message::PropGetMessage reply;
            reply.set_prop_id(123);
            reply.set_prop_type(1);
            reply.set_amount(1);

            server.sendTo(conv, reply);
        }
        else {
            // 3. Other types (if any)
            printf("[conv=%u] Unknown message type: %s\n",
                   conv, desc->name().c_str());
        } });

    server.run();

    google::protobuf::ShutdownProtobufLibrary();
    return 0;
}
