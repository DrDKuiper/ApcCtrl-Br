#!/usr/bin/env bash
set -euo pipefail
# Package the Swift menubar agent into a .app so notifications work
# Usage: scripts/macos/macos-modern-package.sh [Debug|Release]

CONFIG_INPUT=${1:-Release}
CONFIG=$(echo "$CONFIG_INPUT" | tr '[:upper:]' '[:lower:]')
case "$CONFIG" in
  release|debug) ;;
  *) echo "Invalid configuration '$CONFIG_INPUT' (use Release or Debug)" >&2; exit 1;;
esac

PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
APP_SRC="$PROJECT_ROOT/src/macos-modern"
APP_NAME="ApcCtrl"
BINARY_NAME="apcctrl-macos-modern"
APP_DIR="$PROJECT_ROOT/$APP_NAME.app"

cd "$APP_SRC"

echo "Building ($CONFIG)..."
# Build and capture binary path (swift 5.6+ supports --show-bin-path)
BIN_DIR=$(swift build -c "$CONFIG" --show-bin-path)
BIN_PATH="$BIN_DIR/$BINARY_NAME"

if [[ ! -x "$BIN_PATH" ]]; then
  echo "Built binary not found or not executable: $BIN_PATH" >&2
  exit 1
fi

echo "Creating app bundle at $APP_DIR"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources"

# Info.plist
if [[ -f "$APP_SRC/AppBundleInfo.plist" ]]; then
  /usr/libexec/PlistBuddy -c "Set :CFBundleExecutable $BINARY_NAME" "$APP_SRC/AppBundleInfo.plist" >/dev/null 2>&1 || true
  cp "$APP_SRC/AppBundleInfo.plist" "$APP_DIR/Contents/Info.plist"
else
  cat >"$APP_DIR/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>apcctrl-macos-modern</string>
  <key>CFBundleIdentifier</key><string>br.com.apcctrl.macosmodern</string>
  <key>CFBundleExecutable</key><string>apcctrl-macos-modern</string>
  <key>CFBundleVersion</key><string>1.0</string>
  <key>CFBundleShortVersionString</key><string>1.0</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>LSUIElement</key><true/>
  <key>NSPrincipalClass</key><string>NSApplication</string>
</dict>
</plist>
PLIST
fi

echo "Copying binary"
cp "$BIN_PATH" "$APP_DIR/Contents/MacOS/$BINARY_NAME"
chmod +x "$APP_DIR/Contents/MacOS/$BINARY_NAME"

# Optional: copy an icon if you add one later
# cp "$APP_SRC/Resources/AppIcon.icns" "$APP_DIR/Contents/Resources" && \
#   /usr/libexec/PlistBuddy -c "Add :CFBundleIconFile string AppIcon.icns" "$APP_DIR/Contents/Info.plist"

# Gatekeeper quarantine removal (optional when running locally)
xattr -dr com.apple.quarantine "$APP_DIR" || true

echo "Done. Launching app bundle"
open -a Finder "$APP_DIR" >/dev/null 2>&1 || true
open "$APP_DIR"

echo "If notifications don't appear, grant permission in System Settings → Notifications → ApcCtrl." 
