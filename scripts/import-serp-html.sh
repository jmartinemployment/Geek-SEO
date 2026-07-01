#!/usr/bin/env bash
set -euo pipefail

# Import saved Google SERP HTML into sa2 research lanes.
# Usage:
#   ./scripts/import-serp-html.sh --project-id <uuid> --target-url https://www.example.com/ \
#     --topic customer-journey --lane keyword research/customer-journey/keyword/file.html
#
# Supplemental lanes (same run id after keyword import):
#   ./scripts/import-serp-html.sh --run-id <uuid> --topic customer-journey --lane gov path/to/gov.html

API_URL="${SITE_ANALYZER2_API_URL:-http://localhost:5051/api/seo/sa2}"
PROJECT_ID=""
RUN_ID=""
TOPIC=""
LANE="keyword"
TARGET_URL=""
HTML_FILE=""

usage() {
  sed -n '2,12p' "$0"
  exit 1
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --project-id) PROJECT_ID="$2"; shift 2 ;;
    --run-id) RUN_ID="$2"; shift 2 ;;
    --topic) TOPIC="$2"; shift 2 ;;
    --lane) LANE="$2"; shift 2 ;;
    --target-url) TARGET_URL="$2"; shift 2 ;;
    --api-url) API_URL="$2"; shift 2 ;;
    -h|--help) usage ;;
    *)
      if [[ -z "$HTML_FILE" ]]; then
        HTML_FILE="$1"
        shift
      else
        echo "Unexpected argument: $1" >&2
        usage
      fi
      ;;
  esac
done

if [[ -z "$HTML_FILE" || ! -f "$HTML_FILE" ]]; then
  echo "HTML file path required." >&2
  usage
fi

if [[ -z "$TOPIC" ]]; then
  echo "--topic is required." >&2
  exit 1
fi

if [[ "$LANE" == "keyword" && -z "$RUN_ID" ]]; then
  if [[ -z "$PROJECT_ID" || -z "$TARGET_URL" ]]; then
    echo "Initial keyword import requires --project-id and --target-url." >&2
    exit 1
  fi
  RESP=$(curl -sS -w "\n%{http_code}" -X POST \
    "${API_URL}/imports/serp-html?projectId=${PROJECT_ID}&targetSiteUrl=$(python3 -c "import urllib.parse; print(urllib.parse.quote('''$TARGET_URL'''))")" \
    -H "Content-Type: text/html" \
    --data-binary @"$HTML_FILE")
  BODY=$(echo "$RESP" | sed '$d')
  CODE=$(echo "$RESP" | tail -n1)
  echo "$BODY" | python3 -m json.tool 2>/dev/null || echo "$BODY"
  if [[ "$CODE" -ge 400 ]]; then
    echo "Import failed (HTTP $CODE)" >&2
    exit 1
  fi
  RUN_ID=$(echo "$BODY" | python3 -c "import sys,json; print(json.load(sys.stdin).get('runId',''))")
  if [[ -n "$RUN_ID" ]]; then
    echo "Run id: $RUN_ID — import supplemental lanes with --run-id $RUN_ID --topic $TOPIC --lane <edu|gov|local|wiki> ..."
  fi
  exit 0
fi

if [[ -z "$RUN_ID" ]]; then
  echo "--run-id is required for lane imports after the initial keyword import." >&2
  exit 1
fi

RESP=$(curl -sS -w "\n%{http_code}" -X POST \
  "${API_URL}/analysis-runs/${RUN_ID}/serp/import-html?lane=${LANE}&topic=${TOPIC}" \
  -H "Content-Type: text/html" \
  --data-binary @"$HTML_FILE")
BODY=$(echo "$RESP" | sed '$d')
CODE=$(echo "$RESP" | tail -n1)
echo "$BODY" | python3 -m json.tool 2>/dev/null || echo "$BODY"
if [[ "$CODE" -ge 400 ]]; then
  echo "Import failed (HTTP $CODE)" >&2
  exit 1
fi
