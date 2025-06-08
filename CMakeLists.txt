cmake_minimum_required(VERSION 3.10)
project(HeavenOrHellServer)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Find required packages
find_package(Protobuf REQUIRED)

# Collect generated protobuf files
file(GLOB PROTO_SOURCES "src/message/gen/*.cc")
file(GLOB PROTO_HEADERS "src/message/gen/*.h")

# Server executable
add_executable(${PROJECT_NAME}
    src/main.cpp
    src/net/kcp/ikcp.c
    src/net/kcp/kcp_server.cpp
    src/net/kcp/kcp_session.cpp
    src/map/maze_map.cpp
    src/room/room.cpp
    src/room/room_manager.cpp
    ${PROTO_SOURCES}
)

target_include_directories(${PROJECT_NAME} PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/src
    ${CMAKE_CURRENT_SOURCE_DIR}/src/message/gen
    ${Protobuf_INCLUDE_DIRS}
)

target_link_libraries(${PROJECT_NAME} PRIVATE
    ${Protobuf_LIBRARIES}
    pthread
)

# Set compiler flags
if(MSVC)
    target_compile_options(${PROJECT_NAME} PRIVATE /W4)
else()
    target_compile_options(${PROJECT_NAME} PRIVATE -Wall -Wextra)
endif()