#!/bin/bash
# Build llama.cpp for Android ARM64
# Usage: ./scripts/build-llama-android.sh
# Requires: Android NDK installed (set ANDROID_NDK_HOME)

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
PLUGIN_DIR="$PROJECT_DIR/Assets/Plugins/Android/libs/arm64-v8a"
TEMP_DIR="$PROJECT_DIR/build_llama_temp"

echo "=== Building llama.cpp for Android ARM64 ==="

# Check NDK
if [ -z "$ANDROID_NDK_HOME" ]; then
    # Try common paths
    if [ -d "$HOME/Android/Sdk/ndk" ]; then
        ANDROID_NDK_HOME=$(ls -d "$HOME/Android/Sdk/ndk/"*/ | sort -V | tail -1)
        export ANDROID_NDK_HOME
    elif [ -d "/opt/android-sdk/ndk" ]; then
        ANDROID_NDK_HOME=$(ls -d "/opt/android-sdk/ndk/"*/ | sort -V | tail -1)
        export ANDROID_NDK_HOME
    else
        echo "ERROR: ANDROID_NDK_HOME not set and NDK not found in default locations."
        echo "Set ANDROID_NDK_HOME to your NDK path and re-run."
        echo "Example: export ANDROID_NDK_HOME=~/Android/Sdk/ndk/27.0.12077973"
        exit 1
    fi
fi

echo "NDK: $ANDROID_NDK_HOME"
echo "Plugin dir: $PLUGIN_DIR"

# Create plugin dir
mkdir -p "$PLUGIN_DIR"

# Clone llama.cpp if needed
if [ ! -d "$TEMP_DIR/llama.cpp" ]; then
    echo "Cloning llama.cpp..."
    git clone --depth 1 https://github.com/ggml-org/llama.cpp "$TEMP_DIR/llama.cpp"
fi

cd "$TEMP_DIR/llama.cpp"
echo "Building..."

mkdir -p build-android
cd build-android

cmake .. \
    -DCMAKE_TOOLCHAIN_FILE="$ANDROID_NDK_HOME/build/cmake/android.toolchain.cmake" \
    -DANDROID_ABI=arm64-v8a \
    -DANDROID_PLATFORM=android-34 \
    -DLLAMA_STATIC=ON \
    -DLLAMA_NATIVE=OFF \
    -DBUILD_SHARED_LIBS=ON \
    -DLLAMA_BUILD_TESTS=OFF \
    -DLLAMA_BUILD_EXAMPLES=OFF \
    -DLLAMA_BUILD_SERVER=OFF \
    -DLLAMA_BUILD_TRAINING=OFF

make -j$(nproc) llama

# Copy .so to Unity plugin dir
echo "Copying libraries..."
find . -name "*.so" -exec cp {} "$PLUGIN_DIR/" \;

echo "=== Done! ==="
echo "Libraries installed to: $PLUGIN_DIR"
ls -la "$PLUGIN_DIR/"
echo ""
echo "Now open the project in Unity, set Scripting Backend to IL2CPP,"
echo "Target Architecture to ARM64, and build the APK."
