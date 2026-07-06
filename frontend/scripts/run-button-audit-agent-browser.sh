#!/usr/bin/env bash
# Button audit via Vercel agent-browser against production (or BUTTON_AUDIT_BASE_URL).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MODE="${1:-all}"

if ! command -v agent-browser >/dev/null 2>&1; then
  echo "agent-browser not found. Install: npm i -g agent-browser && agent-browser install" >&2
  exit 1
fi

export BUTTON_AUDIT_BASE_URL="${BUTTON_AUDIT_BASE_URL:-https://seo.geekatyourspot.com}"
export CONTENT_WRITING_ANALYSIS_RUN_ID="${CONTENT_WRITING_ANALYSIS_RUN_ID:-d59e001c-d5e0-4703-9a7e-f2c7a829b515}"

cd "$ROOT"
node scripts/agent-browser-button-audit.mjs "$MODE"
