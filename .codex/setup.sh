#!/bin/bash
set -euo pipefail

# Ensure dotnet is available
if ! command -v dotnet sdk check >/dev/null; then
  apt update && apt install -y --no-install-recommends dotnet-sdk-8.0 # Replace by 9.0 when Codex image updated
fi

export PATH="$PATH:$HOME/.dotnet/tools"

# Restore local tools (including csharpier)
dotnet tool restore || true

# Restore the project dependencies
dotnet restore || true
