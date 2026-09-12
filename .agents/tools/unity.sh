#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"
editor_version="$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)"
editor_binary="${UNITY_EDITOR:-$HOME/Unity/Hub/Editor/$editor_version/Editor/Unity}"
if [[ ! -x "$editor_binary" ]]; then
    printf 'Unity %s was not found. Set UNITY_EDITOR to its executable.\n' "$editor_version" >&2
    exit 1
fi
mkdir -p Logs
action="${1:-help}"
case "$action" in
    open) exec "$editor_binary" -projectPath "$repo_root" -logFile "$repo_root/Logs/editor.log" ;;
    setup) method=PrototypeProject.CreateScene ;;
    linux) method=PrototypeProject.BuildLinux ;;
    windows) method=PrototypeProject.BuildWindows ;;
    test)
        exec "$editor_binary" -batchmode -nographics -projectPath "$repo_root" -runTests -testPlatform EditMode -testResults "$repo_root/Logs/editmode.xml" -logFile "$repo_root/Logs/tests.log"
        ;;
    *) printf 'Usage: %s {open|setup|linux|windows|test}\n' "$0"; exit 0 ;;
esac
# Include uncommitted source in the stamp, without adding anything to Git's index.
source_digest="$(python3 - <<'PY'
import hashlib, pathlib, subprocess
names = subprocess.check_output(['git', 'ls-files', '-co', '--exclude-standard', '-z']).split(b'\0')
digest = hashlib.sha256()
for name in sorted(set(names)):
    if not name:
        continue
    path = pathlib.Path(name.decode())
    if path.is_file():
        digest.update(name + b'\0' + path.read_bytes() + b'\0')
print(digest.hexdigest())
PY
)"
export EXPLORERS_BUILD_REVISION="$(git rev-parse --short HEAD)/sha256:$source_digest"
exec "$editor_binary" -batchmode -nographics -quit -projectPath "$repo_root" -executeMethod "$method" -logFile "$repo_root/Logs/$action.log"
