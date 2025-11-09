#!/usr/bin/env bash
set -euo pipefail

# apcctrl macOS bootstrap: checks deps, regenerates configure, builds daemon and Swift agent
# Usage: ./scripts/macos/install.sh [--device <path>] [--start] [--skip-swift]
# Requires: macOS 12+, Xcode CLT, optional Homebrew for deps

DEVICE_ARG=""
START_AFTER=false
BUILD_SWIFT=true

while [[ $# -gt 0 ]]; do
  case "$1" in
    --device)
      DEVICE_ARG="$2"; shift 2;;
    --start)
      START_AFTER=true; shift;;
    --skip-swift)
      BUILD_SWIFT=false; shift;;
    -h|--help)
      echo "Usage: $0 [--device <path>] [--start] [--skip-swift]"; exit 0;;
    *) echo "Unknown arg: $1"; exit 2;;
  esac
done

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "This script must be run on macOS (Darwin)." >&2; exit 1
fi

PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$PROJECT_ROOT"
echo "Project root: $PROJECT_ROOT"

# Do not run as root (Homebrew forbids running as root)
if [[ ${EUID:-$(id -u)} -eq 0 ]]; then
  echo "Please run this script WITHOUT sudo. Use sudo only when starting the daemon (apcctrl)." >&2
  exit 1
fi

# 1) Ensure Xcode CLT
if ! xcode-select -p >/dev/null 2>&1; then
  echo "Xcode Command Line Tools not found. Please run: xcode-select --install" >&2
  exit 1
fi

# 2) Optional: Homebrew and packages (autoconf/automake/libtool/pkg-config/gettext/libusb/hidapi)
if command -v brew >/dev/null 2>&1; then
  echo "Homebrew detected. Ensuring required packages..."
  pkgs=(autoconf automake libtool pkg-config gettext libusb hidapi)
  for p in "${pkgs[@]}"; do
    if ! brew list --versions "$p" >/dev/null 2>&1; then
      echo "Installing $p ..."
      brew install "$p"
    fi
  done
else
  echo "Homebrew not found. Ensure autoconf, automake, libtool, pkg-config, gettext, libusb, hidapi are installed." >&2
fi

# 3) Regenerate ./configure from autoconf/configure.in
if [[ ! -f ./configure ]]; then
  echo "Generating ./configure from autoconf/configure.in ..."
  make configure
fi
# Ensure ./configure is executable even if checked out without +x
chmod +x ./configure || true

# 4) Clean caches/old vars and reconfigure (force fallback for gethostbyname_r)
rm -f config.cache config.log config.status include/apcconfig.h autoconf/variables.mak || true
export SED=/usr/bin/sed
set +e
ac_cv_func_which_gethostbyname_r=no ./configure
CFG_RC=$?
set -e
if [[ $CFG_RC -ne 0 ]]; then
  echo "./configure failed (rc=$CFG_RC). See config.log for details." >&2
  exit $CFG_RC
fi

# 5) Build daemon
CORES=1
if command -v sysctl >/dev/null 2>&1; then CORES=$(sysctl -n hw.ncpu); fi
if command -v getconf >/dev/null 2>&1; then CORES=$(getconf _NPROCESSORS_ONLN); fi
CORES=${CORES:-1}

echo "Building with -j$CORES ..."
make -j"$CORES"

echo "Build complete. Binary should be at $PROJECT_ROOT/src/apcctrl"

# 6) Detect USB modem device and optionally write to apcctrl.conf
CANDIDATE_DEVICE="${DEVICE_ARG}"
if [[ -z "$CANDIDATE_DEVICE" ]]; then
  CANDIDATE_DEVICE=$(ls /dev/cu.usbmodem* 2>/dev/null | head -n1 || true)
fi
if [[ -n "$CANDIDATE_DEVICE" ]]; then
  echo "Detected device: $CANDIDATE_DEVICE"
  if grep -q "^DEVICE" "$PROJECT_ROOT/apcctrl.conf"; then
    echo "apcctrl.conf has a DEVICE line. Leaving as-is."
  else
    echo "Hint: set DEVICE $CANDIDATE_DEVICE in apcctrl.conf"
  fi
else
  echo "No /dev/cu.usbmodem* detected. You can set DEVICE manually in apcctrl.conf later."
fi

# 7) Optionally start daemon
if $START_AFTER; then
  echo "Starting apcctrl (sudo) ..."
  (cd "$PROJECT_ROOT/src" && sudo ./apcctrl -d || true)
  echo "Test NIS: echo 'status' | nc 127.0.0.1 3551"
fi

# 8) Build Swift menubar agent (optional)
if $BUILD_SWIFT && command -v swift >/dev/null 2>&1; then
  echo "Building Swift menubar agent ..."
  (cd "$PROJECT_ROOT/src/macos-modern" && swift build -c release) || echo "Swift build failed (non-fatal)."
fi

echo "Done. Next steps:"
echo "  - Edit $PROJECT_ROOT/apcctrl.conf and set UPSTYPE/DEVICE conforme seu UPS (ex.: usb + DEVICE em branco ou apcsmart + /dev/cu.usbmodemXXXXX)"
echo "  - Inicie: (cd src && sudo ./apcctrl -d) e teste: echo 'status' | nc 127.0.0.1 3551"
echo "  - Rode o app Swift: (cd src/macos-modern && swift run) ou abra o .app criado manualmente"
