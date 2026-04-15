#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "Packaging MessageSubmissionLambda..."
dotnet lambda package \
  --project-location "$REPO_ROOT/src/functions/MessageSubmissionLambda" \
  --configuration Release \
  --framework net10.0 \
  --output-package "$REPO_ROOT/packages/MessageSubmissionLambda/MessageSubmissionLambda.zip"

echo "Done. Output: packages/MessageSubmissionLambda/MessageSubmissionLambda.zip"
