#!/usr/bin/env python3
"""Verify every local release archive against GitHub's uploaded size and SHA-256."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('version')
args = parser.parse_args()
if not args.version or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-' for c in args.version):
    parser.error('Use a simple release version.')
root = Path(__file__).resolve().parents[2]
folder = root / 'Builds' / 'Releases' / args.version
files = sorted(p for p in folder.iterdir() if p.is_file())
if len(files) != 5:
    raise SystemExit('Expected four archives and SHA256SUMS.')
release = json.loads(subprocess.check_output([
    'gh', 'api', f'repos/NicL9923/explorers-by-nature/releases/tags/{args.version}'
], text=True))
assets = {a['name']: a for a in release['assets']}
for path in files:
    asset = assets.get(path.name)
    digest = 'sha256:' + hashlib.file_digest(path.open('rb'), 'sha256').hexdigest()
    if not asset or asset['state'] != 'uploaded' or asset['size'] != path.stat().st_size or asset.get('digest') != digest:
        raise SystemExit(f'Upload verification failed: {path.name}')
    print(f'Verified {path.name}')
print(release['html_url'])
