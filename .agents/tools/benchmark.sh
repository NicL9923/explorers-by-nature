#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"
preset="${1:-high}"
case "$preset" in
    low) width=1280; height=720 ;;
    high) width=1920; height=1080 ;;
    *) printf 'Usage: %s {low|high} [Vulkan-device-index] [rain]\n' "$0" >&2; exit 1 ;;
esac
player="$repo_root/Builds/Linux/ExplorersByNature"
if [[ ! -x "$player" ]]; then
    printf 'Build the Linux player first with .agents/tools/unity.sh linux\n' >&2
    exit 1
fi
mkdir -p Logs/benchmarks
graphics_args=()
weather_args=()
if [[ "${3:-}" == rain ]]; then weather_args=(--benchmark-rain); elif [[ -n "${3:-}" ]]; then printf "Third argument must be rain.\n" >&2; exit 1; fi
if [[ -n "${2:-}" ]]; then
    [[ "$2" =~ ^[0-9]+$ ]] || { printf 'Device index must be a nonnegative integer.\n' >&2; exit 1; }
    graphics_args=(-force-vulkan -force-device-index "$2")
fi
exec env -u LD_LIBRARY_PATH "$player" --benchmark "--quality-$preset" \
    --benchmark-output "$repo_root/Logs/benchmarks" \
    -screen-width "$width" -screen-height "$height" -screen-fullscreen 0 \
    -logFile "$repo_root/Logs/player-$preset-device-${2:-default}.log" "${graphics_args[@]}" "${weather_args[@]}"
