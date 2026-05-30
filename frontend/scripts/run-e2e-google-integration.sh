#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
FRONTEND="$ROOT/frontend"
PROD_API="${PLAYWRIGHT_API_URL:-https://geekseobackend-production.up.railway.app}"

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

FRONT_PID=""

kill_port() {
  local port=$1
  if lsof -ti:"$port" >/dev/null 2>&1; then
    echo "Stopping process on :${port}..."
    lsof -ti:"$port" | xargs kill 2>/dev/null || true
    sleep 2
  fi
}

cleanup() {
  if [[ -n "${FRONT_PID}" ]] && kill -0 "${FRONT_PID}" 2>/dev/null; then
    kill "${FRONT_PID}" 2>/dev/null || true
  fi
  kill_port 3000
}
trap cleanup EXIT

echo "Running Google API integration checks against ${PROD_API}..."
cd "$FRONTEND"
PLAYWRIGHT_API_URL="$PROD_API" node scripts/test-google-integration.mjs

kill_port 3000
echo "Starting Next.js on :3000 with NEXT_PUBLIC_SEO_API_URL=${PROD_API}..."
(
  cd "$FRONTEND"
  export NEXT_PUBLIC_SEO_API_URL="$PROD_API"
  npm run dev -- --hostname localhost
) &
FRONT_PID=$!
wait_http http://localhost:3000/ "Next.js" 180

export PLAYWRIGHT_USE_DEV_USER=true
export PLAYWRIGHT_BASE_URL=http://localhost:3000
export PLAYWRIGHT_API_URL="$PROD_API"
npx playwright test e2e/google-integration.spec.ts --project=authenticated "$@"
exit $?
