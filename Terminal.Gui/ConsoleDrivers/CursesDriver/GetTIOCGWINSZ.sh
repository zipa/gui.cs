#!/bin/bash

# Create output directory if it doesn't exist
mkdir -p ../../compiled-binaries

# Determine the output file extension based on the OS
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    OUTPUT_FILE="../../compiled-binaries/libGetTIOCGWINSZ.so"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    OUTPUT_FILE="../../compiled-binaries/libGetTIOCGWINSZ.dylib"
else
    echo "Unsupported OS: $OSTYPE"
    exit 1
fi

# Compile the C file
gcc -shared -fPIC -o "$OUTPUT_FILE" GetTIOCGWINSZ.c
