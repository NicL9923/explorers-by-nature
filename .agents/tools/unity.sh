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
if ! command -v unity >/dev/null; then
    printf 'Install the official Unity CLI before running this tool.\n' >&2
    exit 1
fi
action="${1:-help}"
case "$action" in
    open) exec unity open "$repo_root" --editor-path "$editor_binary" ;;
    setup) method=PrototypeProject.CreateScene ;;
    linux) method=PrototypeProject.BuildLinux; target=StandaloneLinux64 ;;
    windows) method=PrototypeProject.BuildWindows; target=StandaloneWindows64 ;;
    test|playtest)
        mode=EditMode
        [[ "$action" == playtest ]] && mode=PlayMode
        exec unity test "$repo_root" --editor-path "$editor_binary" --mode "$mode" --output "$repo_root/Logs/$action.xml" -- -nographics -logFile "$repo_root/Logs/$action.log"
        ;;
    *) printf 'Usage: %s {open|setup|linux|windows|test|playtest}\n' "$0"; exit 0 ;;
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
if [[ "$action" == setup ]]; then
    exec unity run "$repo_root" --editor-path "$editor_binary" -- -nographics -executeMethod "$method" -logFile "$repo_root/Logs/$action.log"
fi
exec unity build "$repo_root" --editor-path "$editor_binary" --target "$target" --execute-method "$method" --allow-dirty-build --no-tail --log-file "$repo_root/Logs/$action.log"
