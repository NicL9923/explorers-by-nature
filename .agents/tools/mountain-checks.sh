#!/usr/bin/env bash
# Refresh one frozen mountain build and its scenic evidence on NicolasDESKTOP.
set -euo pipefail
repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"
run() { .agents/tools/bounded-run.sh "$@"; }
python3 .agents/tools/source-manifest.py Logs/mountain-source.json
run .agents/tools/unity.sh test
run .agents/tools/unity.sh playtest
run .agents/tools/unity.sh linux
run .agents/tools/unity.sh windows
run python3 .agents/tools/grove-capture.py --quality high --cinematic-extra --vulkan-device-index 0 --output Logs/mountain-final/high
run python3 .agents/tools/grove-capture.py --quality low --cinematic-extra --vulkan-device-index 1 --width 1280 --height 720 --output Logs/mountain-final/low
python3 .agents/tools/record-mountain-validation.py
