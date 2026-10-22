#!/bin/zsh
set -euo pipefail

SCRIPT_DIR=${0:A:h}
BUILD_DIR="$SCRIPT_DIR/src/PixiEditor.Desktop/bin/Debug/net10.0/osx-arm64"
APP_DIR="$SCRIPT_DIR/dist/PixiEditor Custom.app"

dotnet build "$SCRIPT_DIR/src/PixiEditor.Desktop/PixiEditor.Desktop.csproj" -c Debug -r osx-arm64

mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources/app"
ditto "$BUILD_DIR" "$APP_DIR/Contents/Resources/app"
cp "$SCRIPT_DIR/packaging/macos/Info.plist" "$APP_DIR/Contents/Info.plist"
cp "$SCRIPT_DIR/packaging/macos/PixiEditor Custom" "$APP_DIR/Contents/MacOS/PixiEditor Custom"
chmod +x "$APP_DIR/Contents/MacOS/PixiEditor Custom"

print "$APP_DIR"
