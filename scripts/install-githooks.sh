#!/bin/sh
set -eu

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"

git config core.hooksPath .githooks
chmod +x .githooks/pre-commit

echo "Installed git hooks from .githooks/"
echo "Pre-commit will block deletion of research/ and run protected-path checks."
