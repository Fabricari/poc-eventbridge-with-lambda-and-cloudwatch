#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "Packaging MessageModerationLambda..."
dotnet lambda package \
  --project-location "$REPO_ROOT/src/functions/MessageModerationLambda" \
  --configuration Release \
  --framework net10.0 \
  --output-package "$REPO_ROOT/packages/MessageModerationLambda/MessageModerationLambda.zip"

echo "Done. Output: packages/MessageModerationLambda/MessageModerationLambda.zip"
