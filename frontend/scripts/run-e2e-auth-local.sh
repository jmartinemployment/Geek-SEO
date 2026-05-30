#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
FRONTEND="$ROOT/frontend"
BACKEND="$ROOT/GeekSeoBackend"

wait_http() {
  local url=$1
  local label=$2
  local max=${3:-90}
  for ((i = 1; i <= max; i++)); do
    if curl -sf --max-time 5 "$url" >/dev/null 2>&1; then
      echo "OK $label"
      return 0
    fi
    sleep 1
  done
  echo "Timed out waiting for $label ($url)" >&2
  return 1
}

BACKEND_PID=""
FRONT_PID=""

cleanup() {
  if [[ -n "${FRONT_PID}" ]] && kill -0 "${FRONT_PID}" 2>/dev/null; then
    kill "${FRONT_PID}" 2>/dev/null || true
  fi
  if [[ -n "${BACKEND_PID}" ]] && kill -0 "${BACKEND_PID}" 2>/dev/null; then
    kill "${BACKEND_PID}" 2>/dev/null || true
  fi
}
trap cleanup EXIT

if ! curl -sf --max-time 2 http://127.0.0.1:5051/health >/dev/null 2>&1; then
  echo "Starting GeekSeoBackend on :5051..."
  (cd "$BACKEND" && dotnet run) &
  BACKEND_PID=$!
  wait_http http://127.0.0.1:5051/health "GeekSeoBackend" 120
else
  echo "GeekSeoBackend already running"
fi

if ! curl -sf --max-time 2 http://127.0.0.1:3000/ >/dev/null 2>&1; then
  echo "Starting Next.js on :3000 (uses .env.local + NEXT_PUBLIC_DEV_USER_ID)..."
  (cd "$FRONTEND" && npm run dev -- --hostname 127.0.0.1) &
  FRONT_PID=$!
  wait_http http://127.0.0.1:3000/ "Next.js" 180
else
  echo "Next.js already running"
fi

cd "$FRONTEND"
export PLAYWRIGHT_USE_DEV_USER=true
export PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000
export PLAYWRIGHT_API_URL=http://127.0.0.1:5051
exec npx playwright test --project=authenticated "$@"
