#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"
case "${1:-help}" in
 test) exec dotnet run --project Server.Tests ;;
 publish)
  for server_runtime in linux-x64 win-x64; do
   dotnet publish Server -c Release -r "$server_runtime" --self-contained true -o "Builds/Server/$server_runtime"
   cp docs/morning-playtest.md Server/explorers-ranch.service.example "Builds/Server/$server_runtime/"
   cp Assets/Resources/terrain.bytes "Builds/Server/$server_runtime/terrain.bytes"
  done ;;
 run) shift; exec dotnet run --project Server -- --terrain "$repo_root/Assets/Resources/terrain.bytes" "$@" ;;
 *) printf 'Usage: %s {test|publish|run [server arguments]}\n' "$0" ;;
esac
