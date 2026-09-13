#!/usr/bin/env python3
"""Record matching grove builds, tests, captures, benchmarks and multiplayer evidence."""
import argparse, hashlib, json, pathlib, re, shutil, subprocess, sys
import xml.etree.ElementTree as ET
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--prefix', default='grove', help='Evidence filename prefix; preserves previous release evidence.')
args = parser.parse_args()
if not re.fullmatch(r'[a-z0-9-]+', args.prefix):
    parser.error('prefix must contain lowercase letters, digits or hyphens')
prefix = args.prefix
root = pathlib.Path(__file__).resolve().parents[2]
out = root / 'docs/validation'
out.mkdir(exist_ok=True)
builds = {}
for platform in ['Linux', 'Windows']:
    key = platform.lower()
    assert 'Build Finished, Result: Success.' in (root / f'Logs/{key}.log').read_text(errors='replace')
    level = root / f'Builds/{platform}/ExplorersByNature_Data/level0'
    data = level.read_bytes()
    stamp = re.search(rb'[a-f0-9]{7,40}/sha256:[a-f0-9]{64}', data).group().decode()
    builds[key] = dict(result='Success', runtimeStamp=stamp, level0SHA256=hashlib.sha256(data).hexdigest(), nativeExecution=key == 'linux')
stamp = builds['linux']['runtimeStamp']
assert builds['windows']['runtimeStamp'] == stamp, 'Build workspace stamps differ'
(out / f'{prefix}-builds.json').write_text(json.dumps(builds, indent=2) + '\n')
counts = []
for source, label in [('test', 'editmode'), ('playtest', 'playmode')]:
    path = root / f'Logs/{source}.xml'
    result = ET.parse(path).getroot()
    assert result.get('failed') == '0' and int(result.get('passed')) > 0
    counts.append(result.get('passed'))
    shutil.copyfile(path, out / f'{prefix}-{label}.xml')
metrics = {}
for quality in ['low', 'high']:
    path = max((root / 'Logs/benchmarks').glob(f'*-{quality}.json'), key=lambda p: p.stat().st_mtime)
    result = json.loads(path.read_text())
    assert result['revision'] == stamp and result['route'] == 'Fern Hollow', 'Stale benchmark'
    metrics[quality] = result
    shutil.copyfile(path, out / f'{prefix}-{quality}.json')
    capture = root / f'Logs/grove-capture/{quality}'
    log = (capture / 'player.log').read_text(errors='replace')
    assert 'GROVE_CAPTURE_REVISION ' + stamp in log and 'GROVE_CAPTURE_COMPLETE ' in log, 'Stale capture'
    for name in ['01-entrance', '02-trail-and-doe', '03-fern-trail', '04-stump-and-floor', '05-doe-close', '06-overlook']:
        assert (capture / (name + '.png')).read_bytes()[:8] == b'\x89PNG\r\n\x1a\n'
    shutil.copyfile(capture / 'player.log', out / f'{prefix}-{quality}-capture.txt')
smoke = root / 'Logs/ranch-smoke'
for role in ['builder', 'observer']:
    log = (smoke / f'{role}.log').read_text(errors='replace')
    assert 'RANCH_SMOKE_REVISION ' + stamp in log and 'RANCH_SMOKE_PASSED 25' in log, 'Stale multiplayer run'
builder = json.loads((smoke / 'builder.json').read_text())
assert builder == json.loads((smoke / 'observer.json').read_text())
assert builder['revision'] == 25 and len(builder['pieces']) == 20 and builder['expeditionStage'] == 3
assert 'PASS:' in (root / 'Logs/grove-multiplayer.txt').read_text()
shutil.copyfile(smoke / 'builder.json', out / f'{prefix}-shared-ranch.json')
shutil.copyfile(root / 'Logs/grove-multiplayer.txt', out / f'{prefix}-multiplayer.txt')
for quality, source, target in [('high', '02-trail-and-doe', 'grove-high'), ('high', '01-entrance', 'grove-entrance'), ('high', '04-stump-and-floor', 'grove-detail'), ('low', '02-trail-and-doe', 'grove-low'), ('high', '05-doe-close', 'grove-doe'), ('high', '06-overlook', 'grove-overlook')]:
    shutil.copyfile(root / f'Logs/grove-capture/{quality}/{source}.png', root / f'docs/benchmarks/{target.replace("grove-", prefix + "-", 1)}.png')
subprocess.run([sys.executable, str(root / '.agents/tools/source-manifest.py'), str(out / f'{prefix}-source-files.json')], check=True)
print(json.dumps(dict(stamp=stamp, editmode=counts[0], playmode=counts[1], metrics=metrics), indent=2))
