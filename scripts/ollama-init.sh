#!/bin/sh
set -e

OLLAMA_HOST="${OLLAMA_HOST:-http://ollama:11434}"
MODELS="${OLLAMA_MODELS:-nomic-embed-text}"

for model in $MODELS; do
  echo "Checking for model: $model"
  if curl -sf "$OLLAMA_HOST/api/show" -d "{\"name\":\"$model\"}" > /dev/null 2>&1; then
    echo "Model $model already exists, skipping."
  else
    echo "Pulling $model..."
    curl -sf "$OLLAMA_HOST/api/pull" -d "{\"name\":\"$model\"}"
    echo "Model $model pulled successfully."
  fi
done
