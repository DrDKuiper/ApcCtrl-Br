#!/usr/bin/env bash
set -euo pipefail
# Run apcctrl daemon in foreground on macOS for debug and NIS verification
# Usage: scripts/macos/run-apcctrl-foreground.sh [/path/to/apcctrl.conf]

PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
CONF=${1:-}

if [[ -z "${CONF}" ]]; then
  # Prefer system location if present, else examples, else repo root
  if [[ -f "/usr/local/etc/apcctrl/apcctrl.conf" ]]; then
    CONF="/usr/local/etc/apcctrl/apcctrl.conf"
  elif [[ -f "$PROJECT_ROOT/examples/apcctrl.macos.conf" ]]; then
    CONF="$PROJECT_ROOT/examples/apcctrl.macos.conf"
  elif [[ -f "$PROJECT_ROOT/apcctrl.conf" ]]; then
    CONF="$PROJECT_ROOT/apcctrl.conf"
  else
    echo "No config file found. Create one or copy examples/apcctrl.macos.conf" >&2
    exit 1
  fi
fi

# Locate daemon binary
DAEMON="/usr/local/sbin/apcctrl"
if [[ ! -x "$DAEMON" ]]; then
  if [[ -x "$PROJECT_ROOT/src/apcctrl" ]]; then
    DAEMON="$PROJECT_ROOT/src/apcctrl"
  else
    echo "Could not find apcctrl binary. Build it first (./configure && make)." >&2
    exit 1
  fi
fi

# Sanity hints
if ! grep -qi '^NETSERVER\s\+on' "$CONF"; then
  echo "Warning: NETSERVER is not 'on' in $CONF; NIS (port 3551) will not listen." >&2
fi

LOCKFILE_LINE=$(grep -i '^LOCKFILE' "$CONF" || true)
if [[ -n "$LOCKFILE_LINE" ]] && ! echo "$LOCKFILE_LINE" | grep -q '/tmp/'; then
  echo "Hint: On macOS prefer LOCKFILE /tmp/apcctrl.lock to avoid permission issues." >&2
fi

echo "Using config: $CONF"
cmd=(sudo "$DAEMON" -f "$CONF" -d)
echo "Running: ${cmd[*]}"
"${cmd[@]}"
