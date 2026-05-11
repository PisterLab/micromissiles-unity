#!/bin/bash

# Exit on error.
set -e

# Get workspace directory.
WORKSPACE=$(git rev-parse --show-toplevel)

# Default directories.
INPUT_DIR="${1:-$WORKSPACE/Assets/Proto}"
OUTPUT_DIR="${2:-$WORKSPACE/Assets/Scripts/Generated/Proto}"
PYTHON_OUTPUT_DIR="$WORKSPACE/Tools/pb"

# Check if protoc is installed.
if ! command -v protoc &> /dev/null; then
  echo "Error: protoc is not installed." >&2
  exit 1
fi

# Check if the input directory exists.
if [ ! -d "$INPUT_DIR" ]; then
  echo "Error: Input directory does not exist: $INPUT_DIR." >&2
  exit 1
fi

# Remove all existing .cs files in the output directory if the output directory exists.
if [ -d "$OUTPUT_DIR" ]; then
  rm -f "$OUTPUT_DIR"/*.cs
fi

# Remove the existing Python run config module, so the generated file always matches the current proto schema.
rm -f "$PYTHON_OUTPUT_DIR/run_config_pb2.py"

# Compile all .proto files needed by Unity from the input directory.
echo "Compiling Unity .proto files from $INPUT_DIR to $OUTPUT_DIR."
find "$INPUT_DIR/" -name '*.proto' ! -name 'run_config.proto' -exec protoc --proto_path="$WORKSPACE/Assets/Proto" --csharp_out="$OUTPUT_DIR" {} +

# Compile the Python run config module used by the batch run launcher.
echo "Compiling Python run_config.proto to $PYTHON_OUTPUT_DIR."
protoc \
  --proto_path="$WORKSPACE/Assets/Proto/Configs" \
  --python_out="$PYTHON_OUTPUT_DIR" \
  "$WORKSPACE/Assets/Proto/Configs/run_config.proto"

echo "Protobuf compilation completed."
