cmake_minimum_required(VERSION 3.10)
project(HeavenOrHellServer)

set(CMAKE_CXX_STANDARD 11)
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

# Client executable
add_executable(kcp_client
    src/net/kcp/kcp_client.cpp
    src/net/kcp/ikcp.c
    ${PROTO_SOURCES}
)

target_include_directories(kcp_client PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/src
    ${CMAKE_CURRENT_SOURCE_DIR}/src/message/gen
    ${Protobuf_INCLUDE_DIRS}
)

target_link_libraries(kcp_client PRIVATE
    ${Protobuf_LIBRARIES}
    pthread
)

# Set compiler flags
if(MSVC)
    target_compile_options(${PROJECT_NAME} PRIVATE /W4)
    target_compile_options(kcp_client PRIVATE /W4)
else()
    target_compile_options(${PROJECT_NAME} PRIVATE -Wall -Wextra)
    target_compile_options(kcp_client PRIVATE -Wall -Wextra)
endif() 