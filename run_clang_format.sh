#!/bin/bash

# Path to clang-format executable (if it's already on PATH, just "clang-format")
CLANG_FORMAT="clang-format"

# Check if clang-format is available
if ! command -v "$CLANG_FORMAT" &> /dev/null; then
    echo "Error: clang-format not found. Please ensure it's installed and in your PATH."
    exit 1
fi

# 1) Format all .cs files in Assets, excluding Scripts/Generated.
echo "Formatting Unity Assets..."
cd Assets
find . -type d -path "./Scripts/Generated" -prune -o \
  -type f -name "*.cs" \
  -exec "$CLANG_FORMAT" -i -style=file {} +
cd ..

# 2) Format only C/C++ files under plugins/, excluding any in bazel-* directories
echo "Formatting Bazel project files..."
cd plugins
find . -type d -name "bazel-\*" -prune -o \
  -type f \( -name "*.cc" -o -name "*.cpp" -o -name "*.h" -o -name "*.hpp" \) \
  -exec "$CLANG_FORMAT" -i -style=file {} +
cd ..

echo "Formatting complete."
