#!/usr/bin/env bash
# Standard UI testing workflow for Geek SEO frontend.
#
# Runs production smoke → Playwright button audits (with console checks) →
# optional agent-browser button audits when the CLI is installed.
#
# Usage:
#   cd frontend && bash scripts/run-ui-testing-workflow.sh
#   cd frontend && bash scripts/run-ui-testing-workflow.sh local
#
# Local mode starts Next.js with PLAYWRIGHT_USE_DEV_USER and runs the full
# local button-audit + walkthrough-capable fixtures.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MODE="${1:-production}"
AGENT_BROWSER="${RUN_AGENT_BROWSER:-auto}"

cd "$ROOT"

echo "==> 1/3 Production smoke (Playwright + console)"
npm run test:e2e:smoke -- --project=smoke

echo ""
echo "==> 2/3 Button audits (Playwright + console)"
if [[ "$MODE" == "local" ]]; then
  bash scripts/run-button-audit-playwright.sh local
else
  bash scripts/run-button-audit-playwright.sh production
fi

run_agent_browser() {
  if [[ "$AGENT_BROWSER" == "false" ]]; then
    echo "Skipping agent-browser (RUN_AGENT_BROWSER=false)"
    return 0
  fi
  if ! command -v agent-browser >/dev/null 2>&1; then
    if [[ "$AGENT_BROWSER" == "true" ]]; then
      echo "agent-browser required but not installed" >&2
      exit 1
    fi
    echo "Skipping agent-browser (not installed; npm i -g agent-browser && agent-browser install)"
    return 0
  fi
  echo ""
  echo "==> 3/3 Button audits (agent-browser + console)"
  bash scripts/run-button-audit-agent-browser.sh all
}

run_agent_browser

echo ""
echo "UI testing workflow complete."
