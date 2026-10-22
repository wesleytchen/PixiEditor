#!/bin/zsh
set -euo pipefail

SCRIPT_DIR=${0:A:h}
APP_DLL="$SCRIPT_DIR/src/PixiEditor.Desktop/bin/Debug/net10.0/osx-arm64/PixiEditor.Desktop.dll"

if ! command -v dotnet >/dev/null 2>&1; then
  print -u2 "The .NET 10 SDK is required. Install it with: brew install dotnet"
  exit 1
fi

dotnet build "$SCRIPT_DIR/src/PixiEditor.Desktop/PixiEditor.Desktop.csproj" -c Debug -r osx-arm64

cd "${APP_DLL:h}"
exec dotnet "$APP_DLL" "$@"
