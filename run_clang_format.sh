#!/bin/bash

# Path to the clang-format executable.
CLANG_FORMAT="clang-format"

# Check if clang-format is available.
if ! command -v "$CLANG_FORMAT" &> /dev/null; then
  echo "Error: clang-format not found. Please ensure that it is installed and in your PATH."
  exit 1
fi

# Format all .cs files in Assets, excluding Scripts/Generated.
echo "Formatting Unity Assets..."
(
  cd Assets || exit
  find . -type d -path "./Scripts/Generated" -prune -o \
    -type f -name "*.cs" \
    -exec "$CLANG_FORMAT" -i -style=file {} +
)

# Format only C/C++ files under plugins, excluding any in bazel-* directories.
echo "Formatting Bazel project files..."
(
  cd plugins || exit
  find . -type d -name "bazel-\*" -prune -o \
    -type f \( -name "*.cc" -o -name "*.cpp" -o -name "*.h" -o -name "*.hpp" \) \
    -exec "$CLANG_FORMAT" -i -style=file {} +
)

echo "Formatting complete."
