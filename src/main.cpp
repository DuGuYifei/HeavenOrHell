#include "net/kcp/kcp_server.h"
#include "map/maze_map.h"

int main()
{
    GOOGLE_PROTOBUF_VERIFY_VERSION;
    KcpServer server(8888);

    server.setMessageCallback([&server](uint32_t conv, const google::protobuf::Message &msg)
                              {
        // Compare message types using Descriptor
        const auto* desc = msg.GetDescriptor();

        if (desc == message::ReaperAttackMessage::descriptor()) {
            const auto* reaper_attack_msg = static_cast<const message::ReaperAttackMessage*>(&msg);
            printf("[conv=%u] Reaper attack message received\n", conv);
            // 创建攻击结果消息
            message::ReaperAttackResultMessage result;
            result.set_soul_player_id(reaper_attack_msg->soul_player_id());
            result.set_is_hit(true);
            // 添加wrapper
            message::MessageWrapper wrapper;
            wrapper.mutable_reaper_attack_result_message()->CopyFrom(result);
            server.sendTo(conv, wrapper);
            printf("[conv=%u] Reaper attack result: %d\n", conv, result.is_hit());
        }
        else if (desc == message::SoulBasicMessage::descriptor()) {
            printf("[conv=%u] Soul basic message received\n", conv);
        }
        else {
            printf("[conv=%u] Unknown message type: %s\n", conv, desc->name().c_str());
        } });

    server.run();

    google::protobuf::ShutdownProtobufLibrary();
    return 0;
}
