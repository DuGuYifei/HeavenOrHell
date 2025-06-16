#!/bin/bash

# Build test client script

echo "Building test client..."

# Generate protobuf files if they don't exist
if [ ! -d "src/message/gen" ] || [ -z "$(ls -A src/message/gen 2>/dev/null)" ]; then
    echo "Generating protobuf files..."
    cd src/message
    if [ -f "proto2cpp.sh" ]; then
        ./proto2cpp.sh
    else
        # Generate manually if script doesn't exist
        mkdir -p gen
        protoc --cpp_out=gen message.proto
    fi
    cd ../..
fi

# Create build directory if it doesn't exist
mkdir -p build_test_client
cd build_test_client

# Copy CMake file for test client
cp ../test_client_CMakeLists.txt CMakeLists.txt

# Configure and build
cmake -DCMAKE_BUILD_TYPE=Release .
make -j$(nproc)

if [ $? -eq 0 ]; then
    echo "Build successful!"
    echo ""
    echo "Usage examples:"
    echo "  ./test_client           # Create new room (room_id=0)"
    echo "  ./test_client 1001      # Join room 1001"
    echo ""
    echo "Note: Make sure the server is running on localhost:8888"
else
    echo "Build failed!"
    exit 1
fi 