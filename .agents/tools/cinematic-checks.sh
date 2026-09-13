#!/usr/bin/env bash
# Freeze runtime inputs, validate desktop builds and collect matching cinematic evidence.
set -euo pipefail
repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"
run() { .agents/tools/bounded-run.sh "$@"; }
python3 .agents/tools/source-manifest.py Logs/cinematic-source-files.json
run .agents/tools/unity.sh test
run .agents/tools/unity.sh playtest
run .agents/tools/unity.sh linux
run .agents/tools/unity.sh windows
run python3 .agents/tools/western-hair-capture.py --vulkan-device-index 0
run python3 .agents/tools/grove-capture.py --quality high --vulkan-device-index 0 --cinematic-extra --output Logs/cinematic-final/high
run python3 .agents/tools/grove-capture.py --quality low --vulkan-device-index 1 --width 1280 --height 720 --cinematic-extra --output Logs/cinematic-final/low
run python3 .agents/tools/nature-motion.py --quality high --vulkan-device-index 0 --output Logs/cinematic-motion-final/high
run python3 .agents/tools/nature-motion.py --quality low --vulkan-device-index 1 --output Logs/cinematic-motion-final/low
run python3 .agents/tools/ranch-smoke.py --vulkan-device-index 0 --validation-fps 30 > Logs/cinematic-multiplayer.txt
run python3 .agents/tools/record-cinematic-validation.py
