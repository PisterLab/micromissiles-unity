#!/bin/bash

# Path to clang-format executable (if it's already on PATH, just "clang-format")
CLANG_FORMAT="clang-format"

# Check if clang-format is available
if ! command -v "$CLANG_FORMAT" &> /dev/null; then
    echo "Error: clang-format not found. Please ensure it's installed and in your PATH."
    exit 1
fi


# Run clang-format on .cs and .json files in Assets/ folder
cd Assets && find . -type f \( -name "*.cs" -o -name "*.json" \) -exec "$CLANG_FORMAT" -i -style=file {} + && cd ..

echo "Formatting complete."
