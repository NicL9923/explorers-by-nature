#!/usr/bin/env python3
"""Package the tested desktop players and self-contained servers with checksums."""
import argparse, hashlib, pathlib, shutil, subprocess, tarfile, zipfile
parser=argparse.ArgumentParser();parser.add_argument('version');args=parser.parse_args()
if not args.version or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-' for c in args.version):parser.error('Use a simple release version.')
root=pathlib.Path(__file__).resolve().parents[2]
out=root/'Builds'/'Releases'/args.version;out.mkdir(parents=True,exist_ok=True)
revision=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()
artifacts=[]
for source,label,windows in [('Linux','linux-x64',False),('Windows','windows-x64',True),('Server/linux-x64','server-linux-x64',False),('Server/win-x64','server-windows-x64',True)]:
 folder=root/'Builds'/source
 if not folder.is_dir():raise SystemExit(f'Missing build: {folder}')
 shutil.copyfile(root/'docs/morning-playtest.md',folder/'PLAYTEST.md')
 (folder/'SOURCE.txt').write_text(f'Explorers by Nature {args.version}\nPackaging commit: {revision}\nClient runtime stamp is recorded by the benchmark report.\n')
 name=f'explorers-by-nature-{args.version}-{label}'
 archive=out/(name+('.zip' if windows else '.tar.gz'))
 if windows:
  with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as bundle:
   for path in sorted(folder.rglob('*')):
    if path.is_file():bundle.write(path,pathlib.Path(name)/path.relative_to(folder))
 else:
  with tarfile.open(archive,'w:gz',compresslevel=6) as bundle:bundle.add(folder,arcname=name)
 artifacts.append(archive)
(out/'SHA256SUMS').write_text(''.join(hashlib.sha256(path.read_bytes()).hexdigest()+'  '+path.name+'\n' for path in artifacts))
print(out)
for path in artifacts:print(f'{path.name}: {path.stat().st_size/1024/1024:.1f} MiB')
