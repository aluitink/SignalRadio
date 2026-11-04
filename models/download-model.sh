#!/usr/bin/env bash
# Downloads the all-MiniLM-L6-v2 ONNX model + tokenizer for local embeddings.
set -euo pipefail

MODEL_DIR="$(cd "$(dirname "$0")/minilm" && pwd)"
mkdir -p "$MODEL_DIR"

echo "Downloading ONNX model (~80MB)..."
wget -q --show-progress -O "$MODEL_DIR/model.onnx" \
  "https://huggingface.co/Xenova/all-MiniLM-L6-v2/resolve/main/onnx/model.onnx"

echo "Downloading tokenizer..."
wget -q --show-progress -O "$MODEL_DIR/tokenizer.json" \
  "https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/tokenizer.json"

echo ""
echo "Done! Files in $MODEL_DIR:"
ls -lh "$MODEL_DIR"
