#!/bin/bash

# Exit on error.
set -e

# Get workspace directory.
WORKSPACE=$(git rev-parse --show-toplevel)

# Default directories.
INPUT_DIR="${1:-$WORKSPACE/configs/proto}"
OUTPUT_DIR="${2:-$WORKSPACE/Assets/Scripts/Protobuf}"

# Check if protoc is installed.
if ! command -v protoc &> /dev/null; then
  echo "Error: protoc is not installed."
  exit 1
fi

# Check if the input directory exists.
if [ ! -d "$INPUT_DIR" ]; then
  echo "Error: Input directory does not exist: $INPUT_DIR."
  exit 1
fi

# Remove all existing .cs files in the output directory if the output directory exists.
if [ -d "$OUTPUT_DIR" ]; then
  rm -f "$OUTPUT_DIR"/*.cs
fi

# Compile all .proto files in the input directory.
echo "Compiling .proto files from $INPUT_DIR to $OUTPUT_DIR."
protoc \
  --proto_path="$WORKSPACE" \
  --csharp_out="$OUTPUT_DIR" \
  "$INPUT_DIR"/*.proto

echo "Protobuf compilation completed."
