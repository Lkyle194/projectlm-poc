#!/bin/bash
# Download the Gemma 4 E2B QAT Mobile GGUF model
# Usage: ./scripts/download-model.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
MODEL_DIR="$PROJECT_DIR/Assets/StreamingAssets/models"

mkdir -p "$MODEL_DIR"

MODEL_URL="https://huggingface.co/unsloth/gemma-4-E2B-it-qat-mobile-GGUF/resolve/main/gemma-4-E2B-it-qat-mobile-Q4_0.gguf"
MODEL_FILE="$MODEL_DIR/gemma-4-E2B-it-qat-mobile-Q4_0.gguf"

echo "=== Downloading Gemma 4 E2B QAT Mobile GGUF ==="
echo "URL: $MODEL_URL"
echo "Destination: $MODEL_FILE"
echo ""
echo "Size: ~1.1 GB"
echo ""

if [ -f "$MODEL_FILE" ]; then
    echo "Model already exists at $MODEL_FILE"
    echo "Delete it first if you want to re-download."
    exit 0
fi

echo "Downloading..."
wget -O "$MODEL_FILE" "$MODEL_URL"

echo ""
echo "=== Done! ==="
echo "Model saved to: $MODEL_FILE"
ls -lh "$MODEL_FILE"
