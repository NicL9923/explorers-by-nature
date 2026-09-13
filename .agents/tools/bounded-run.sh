#!/usr/bin/env bash
# One resource-bounded validation workload; keep the shared desktop responsive.
set -euo pipefail
if (($# == 0)); then printf 'Usage: %s command [arguments...]\n' "$0" >&2; exit 2; fi
repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
mkdir -p "$repo_root/Logs"
exec 9>"$repo_root/Logs/heavy-workload.lock"
flock -n 9 || { printf 'Another project validation workload is running.\n' >&2; exit 1; }
validation_unit="explorers-validation-$$"
trap 'systemctl --user stop "$validation_unit.scope" >/dev/null 2>&1 || true' EXIT
systemd-run --user --scope --quiet --unit="$validation_unit" \
    -p CPUQuota=300% -p MemoryHigh=8G -p MemoryMax=10G -p MemorySwapMax=0 \
    nice -n 10 taskset -c 0-3 timeout --signal=TERM --kill-after=15s 25m "$@" 9>&-
