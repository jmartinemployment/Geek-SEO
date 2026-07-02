#!/usr/bin/env bash
set -euo pipefail

# Import all manual research lanes from research/{topic}/ into Site Analyzer 2.
#
# Usage:
#   ./scripts/import-research-topic.sh \
#     --project-id <uuid> \
#     --target-url https://www.geekatyourspot.com/ \
#     --topic customer-journey
#
# Optional:
#   --run-id <uuid>   Skip keyword import; only import supplemental lanes
#   --api-url         Override SITE_ANALYZER2_API_URL

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
IMPORT="$ROOT/scripts/import-serp-html.sh"
TOPIC=""
PROJECT_ID=""
RUN_ID=""
TARGET_URL=""
API_URL="${SITE_ANALYZER2_API_URL:-}"

usage() {
  sed -n '2,14p' "$0"
  exit 1
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --project-id) PROJECT_ID="$2"; shift 2 ;;
    --run-id) RUN_ID="$2"; shift 2 ;;
    --topic) TOPIC="$2"; shift 2 ;;
    --target-url) TARGET_URL="$2"; shift 2 ;;
    --api-url) API_URL="$2"; shift 2 ;;
    -h|--help) usage ;;
    *) echo "Unknown argument: $1" >&2; usage ;;
  esac
done

if [[ -z "$TOPIC" ]]; then
  echo "--topic is required." >&2
  exit 1
fi

TOPIC_DIR="$ROOT/research/$TOPIC"
if [[ ! -d "$TOPIC_DIR" ]]; then
  echo "Research folder not found: $TOPIC_DIR" >&2
  exit 1
fi

find_lane_file() {
  local lane="$1"
  local dir="$TOPIC_DIR/$lane"
  [[ -d "$dir" ]] || return 1

  if [[ "$lane" == "paa" ]]; then
    local txt
    txt=$(find "$dir" -maxdepth 1 -type f -name '*.txt' ! -name '* copy*' | head -n1 || true)
    if [[ -n "$txt" ]]; then
      echo "$txt"
      return 0
    fi
  fi

  find "$dir" -maxdepth 1 -type f \( -name '*.html' -o -name '*.htm' \) ! -path '*/.*' | head -n1
}

import_args=(--topic "$TOPIC")
if [[ -n "$API_URL" ]]; then
  import_args+=(--api-url "$API_URL")
fi

if [[ -z "$RUN_ID" ]]; then
  keyword_file=$(find_lane_file keyword)
  if [[ -z "$keyword_file" ]]; then
    echo "No keyword HTML found in $TOPIC_DIR/keyword/" >&2
    exit 1
  fi
  if [[ -z "$TARGET_URL" ]]; then
    echo "Initial import requires --target-url." >&2
    exit 1
  fi
  echo "Importing keyword lane from $(basename "$keyword_file")..."
  output=$("$IMPORT" \
    --target-url "$TARGET_URL" \
    "${import_args[@]}" \
    --lane keyword \
    "$keyword_file")
  echo "$output"
  RUN_ID=$(echo "$output" | python3 -c "import sys,re; m=re.search(r'Run id: ([0-9a-f-]{36})', sys.stdin.read()); print(m.group(1) if m else '')")
  if [[ -z "$RUN_ID" ]]; then
    RUN_ID=$(echo "$output" | python3 -c "import sys,json,re; t=sys.stdin.read(); m=re.search(r'\{.*\}', t, re.S); print((json.loads(m.group(0)).get('keywordProjectId') or json.loads(m.group(0)).get('runId','')) if m else '')")
  fi
  if [[ -z "$RUN_ID" ]]; then
    echo "Could not parse run id from keyword import." >&2
    exit 1
  fi
  echo "Run id: $RUN_ID"
fi

for lane in paa edu gov local wiki; do
  lane_file=$(find_lane_file "$lane" || true)
  if [[ -z "$lane_file" ]]; then
    echo "Skipping $lane — no file in $TOPIC_DIR/$lane/"
    continue
  fi
  echo "Importing $lane from $(basename "$lane_file")..."
  "$IMPORT" --run-id "$RUN_ID" "${import_args[@]}" --lane "$lane" "$lane_file"
done

echo "Done. Run id: $RUN_ID"
