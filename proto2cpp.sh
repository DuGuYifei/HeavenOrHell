#!/bin/bash
set -e

if ! command -v protoc &> /dev/null; then
    echo "Error: protoc is not installed. Please install protobuf-compiler first."
    exit 1
fi

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROTO_DIR="$SCRIPT_DIR/src/message"
OUT_DIR="$PROTO_DIR/gen"

mkdir -p "$OUT_DIR"

# ONLY use .proto file names, not full paths
for proto_file in "$PROTO_DIR"/*.proto; do
    filename=$(basename "$proto_file")  # e.g. message.proto
    echo "Generating C++ code for $filename"
    protoc -I"$PROTO_DIR" --cpp_out="$OUT_DIR" "$filename"
done
