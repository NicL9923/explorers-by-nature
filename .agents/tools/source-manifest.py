#!/usr/bin/env python3
"""Record or verify the exact runtime, asset and test files used by a validation run."""
import argparse, hashlib, json
from pathlib import Path
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('output',type=Path)
parser.add_argument('--verify',action='store_true')
args=parser.parse_args()
root=Path(__file__).resolve().parents[2]
files={}
for folder in ['Assets','Packages','ProjectSettings','Server','Server.Tests']:
    for path in (root/folder).rglob('*'):
        if path.is_file() and not any(part in ['bin','obj'] for part in path.relative_to(root).parts):
            files[str(path.relative_to(root))]=hashlib.sha256(path.read_bytes()).hexdigest()
if args.verify:
    previous=json.loads(args.output.read_text())
    changed=[name for name in sorted(set(files)|set(previous)) if files.get(name)!=previous.get(name)]
    if changed:raise SystemExit('Validation source changed:\n'+'\n'.join(changed))
    print(f'Verified {len(files)} source files unchanged.')
else:
    missing=[str(p.relative_to(root)) for p in (root/'Assets').rglob('*') if p.is_file() and p.suffix!='.meta' and not Path(str(p)+'.meta').exists()]
    if missing:raise SystemExit('Missing asset metadata:\n'+'\n'.join(missing))
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(files,sort_keys=True,indent=2)+'\n')
    print(f'Recorded {len(files)} source files in {args.output}.')
