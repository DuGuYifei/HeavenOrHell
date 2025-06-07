#include "net/kcp/kcp_server.h"
#include "map/maze_map.h"

int main()
{
    GOOGLE_PROTOBUF_VERIFY_VERSION;
    KcpServer server(8888);

    server.run();

    google::protobuf::ShutdownProtobufLibrary();
    return 0;
}
