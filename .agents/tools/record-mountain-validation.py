#!/usr/bin/env python3
"""Collect matching mountain vista builds, tests and scenic player captures."""
import hashlib,json,re,shutil,struct,subprocess,sys
from pathlib import Path
import xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[2]
subprocess.run([sys.executable,str(root/'.agents/tools/source-manifest.py'),str(root/'Logs/mountain-source.json'),'--verify'],check=True)
out=root/'docs/validation';gallery=root/'docs/benchmarks';copies=[]
builds={}
for platform in ['Linux','Windows']:
 p=root/f'Builds/{platform}/ExplorersByNature_Data/level0'
 stamps=set(re.findall(rb'[a-f0-9]{7,40}/sha256:[a-f0-9]{64}',p.read_bytes()))
 assert len(stamps)==1
 log=root/f'Logs/{platform.lower()}.log'
 buildLog=log.read_text(errors='replace')
 assert 'Build Finished, Result: Success.' in buildLog and 'Shader error' not in buildLog
 builds[platform]=dict(stamp=stamps.pop().decode(),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
 copies.append((log,out/f'mountains-{platform.lower()}-build.txt'))
stamp=builds['Linux']['stamp'];assert stamp==builds['Windows']['stamp']
counts={}
for name,minimum in [('test',44),('playtest',25)]:
 p=root/f'Logs/{name}.xml';r=ET.parse(p).getroot()
 assert r.get('result')=='Passed' and int(r.get('passed'))>=minimum
 counts[name]=int(r.get('passed'));copies.append((p,out/f'mountains-{name}.xml'))
for quality,size in [('high',(1920,1080)),('low',(1280,720))]:
 folder=root/f'Logs/mountain-final/{quality}';log=folder/'player.log';text=log.read_text(errors='replace')
 assert 'GROVE_CAPTURE_REVISION '+stamp in text and 'GROVE_CAPTURE_COMPLETE ' in text and 'GROVE_CAPTURE_FAILED' not in text
 assert f'quality={quality} resolution={size[0]}x{size[1]}' in text
 for name in ['01-entrance','06-overlook','07-river','09-rainy-river']:
  p=folder/(name+'.png');header=p.read_bytes()[:24]
  assert header[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',header[16:24])==size
  copies.append((p,gallery/f'mountains-{quality}-{name}.png'))
 copies.append((log,out/f'mountains-{quality}-capture.txt'))
copies.append((root/'Logs/mountain-source.json',out/'mountains-source.json'))
for source,target in copies:shutil.copyfile(source,target)
record=dict(runtimeStamp=stamp,builds=builds,tests=counts,files={str(t.relative_to(root)):hashlib.sha256(t.read_bytes()).hexdigest() for _,t in copies},limitations=['Windows cross-build only; no native Windows execution.','High NVIDIA and Low AMD integrated captures use a 30 FPS cap, not performance benchmarks.','Mountain envelopes are visual scenery; authoritative ground and saved ranch state are unchanged.'])
(out/'mountains-builds.json').write_text(json.dumps(record,indent=2)+'\n')
print(json.dumps(dict(runtimeStamp=stamp,tests=counts,evidence=str(out/'mountains-builds.json')),indent=2))
