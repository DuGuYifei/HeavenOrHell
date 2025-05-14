#!/bin/bash

set -e

# 进入脚本所在目录
cd "$(dirname "$0")"

# 生成 protobuf 文件（可选，确保最新）
./proto2cpp.sh

# 创建 build 目录
mkdir -p build
cd build

# 运行 cmake 和 make
cmake ..
make -j$(nproc)

echo "Build finished!"