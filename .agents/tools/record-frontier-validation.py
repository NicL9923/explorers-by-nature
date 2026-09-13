#!/usr/bin/env python3
"""Verify matching frontier builds, tests and rendered evidence before collecting it."""
import argparse
from fractions import Fraction
import hashlib
import json
import math
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys
import xml.etree.ElementTree as ET

MODELS = ('ranch-hand', 'trail-scout', 'homesteader', 'frontiersman')
POSTERS = ('axe', 'pickaxe', 'riding', 'muzzle-flash', 'reload')


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_json(path):
    return json.loads(path.read_text())


def sha256(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def png(path, dimensions):
    with path.open('rb') as stream:
        header = stream.read(24)
    require(header[:8] == b'\x89PNG\r\n\x1a\n' and header[12:16] == b'IHDR', f'Invalid PNG: {path}')
    require(struct.unpack('>II', header[16:24]) == dimensions, f'Wrong PNG dimensions: {path}')


def completion(path, marker, stamp, failure):
    log = path.read_text(errors='replace')
    require(marker + ' ' + stamp in log and failure not in log, f'Stale or failed capture: {path}')
    return log


def collect(root, baseline, require_small_ui):
    validation, gallery = root / 'docs/validation', root / 'docs/benchmarks'
    copies, builds, tests, portraits, captures = [], {}, {}, {}, {}
    for platform in ('Linux', 'Windows'):
        key = platform.lower()
        level = root / f'Builds/{platform}/ExplorersByNature_Data/level0'
        data = level.read_bytes()
        stamps = set(re.findall(rb'[a-f0-9]{7,40}/sha256:[a-f0-9]{64}', data))
        require(len(stamps) == 1, f'{platform}: missing or ambiguous build stamp')
        require('Build Finished, Result: Success.' in (root / f'Logs/{key}.log').read_text(errors='replace'), f'{platform}: build did not succeed')
        builds[key] = dict(runtimeStamp=stamps.pop().decode(), level0SHA256=hashlib.sha256(data).hexdigest(), result='Success', nativeExecution=key == 'linux')
    stamp = builds['linux']['runtimeStamp']
    require(builds['windows']['runtimeStamp'] == stamp, 'Desktop build stamps differ')
    for filename, label in (('test', 'editmode'), ('playtest', 'playmode')):
        path = root / f'Logs/{filename}.xml'
        result = ET.parse(path).getroot()
        require(result.get('result') == 'Passed' and result.get('failed') == '0' and int(result.get('passed', '0')) > 0, f'{label}: tests did not pass')
        if label == 'playmode':
            cases = {case.get('methodname'): case.get('result') for case in result.iter('test-case')}
            require(cases.get('ToolsGatherReloadAndHuntThroughTheSharedAuthority') == 'Passed', 'Missing successful frontier gameplay test')
            for name in ('EveryExplorerImportsAtHumanScaleWithArticulatedLimbs', 'RemoteExplorerSwapRetiresPreviousModelAndDefaultsUnknownIds', 'WardrobeConfirmsCancelsAndReleasesPreviewResources'):
                require(cases.get(name) == 'Passed', f'Missing successful player test: {name}')
        tests[label] = int(result.get('passed'))
        copies.append((path, validation / f'frontier-{label}.xml'))
    server = root / 'Logs/frontier-server-tests.txt'
    server_log = server.read_text(errors='replace')
    tests['serverAssertions'] = len(re.findall(r'^PASS:', server_log, re.MULTILINE))
    require(tests['serverAssertions'] == 130 and 'FAIL:' not in server_log, 'Expected 130 passing server assertions')
    copies.append((server, validation / 'frontier-server-tests.txt'))

    portrait_runs = [('low', 1280, 720), ('high', 1280, 720)]
    small = root / 'Logs/player-capture/low-960x600'
    require(not require_small_ui or small.is_dir(), 'Missing required low-960x600 UI capture')
    if small.is_dir():
        portrait_runs.append(('low', 960, 600))
    for quality, width, height in portrait_runs:
        label = f'{quality}-{width}x{height}'
        source = root / 'Logs/player-capture' / label
        completion(source / 'player.log', 'PLAYER_CAPTURE_COMPLETE', stamp, 'PLAYER_CAPTURE_FAILED')
        suffix = quality if width == 1280 else label
        for model in MODELS:
            path = source / f'{model}.png'
            png(path, (width, height))
            copies.append((path, gallery / f'frontier-{suffix}-{model}.png'))
        copies.append((source / 'player.log', validation / f'frontier-{label}-wardrobe.txt'))
        portraits[label] = dict(width=width, height=height, models=list(MODELS), completionStamp=stamp)

    for quality in ('low', 'high'):
        source = root / f'Logs/frontier-capture/{quality}'
        proof = read_json(source / 'proof.json')
        require(proof.get('revision') == stamp, f'{quality}: stale gameplay proof')
        require((proof.get('woodGained'), proof.get('stoneGained'), proof.get('meatGained')) == (8, 6, 2), f'{quality}: incorrect gathering or hunting rewards')
        require(math.isfinite(proof.get('riddenMetres', 0)) and proof['riddenMetres'] > 3, f'{quality}: horse did not travel three metres')
        require(proof.get('reloaded') is True and proof.get('frames') == 240 and proof.get('fps') == 24, f'{quality}: incomplete reload recording')
        completion(source / 'player.log', 'FRONTIER_CAPTURE_COMPLETE', stamp, 'FRONTIER_CAPTURE_FAILED')
        expected_frames = [f'frame-{frame:05d}.png' for frame in range(240)]
        require(sorted(path.name for path in source.glob('frame-*.png')) == expected_frames, f'{quality}: missing or unexpected reload frames')
        for frame in expected_frames:
            png(source / frame, (1280, 720))
        result = subprocess.run(['ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_entries', 'stream=nb_frames,r_frame_rate,width,height,duration', '-of', 'json', str(source / 'reload.mp4')], capture_output=True, text=True, check=True)
        streams = json.loads(result.stdout)['streams']
        require(len(streams) == 1, f'{quality}: expected one video stream')
        video = streams[0]
        require(int(video['nb_frames']) == 240 and Fraction(video['r_frame_rate']) == 24, f'{quality}: encoded reload frame count or rate mismatch')
        require((int(video['width']), int(video['height'])) == (1280, 720) and abs(float(video['duration']) - 10) < .05, f'{quality}: encoded reload size or duration mismatch')
        for poster in POSTERS:
            image = source / f'{poster}.png'
            png(image, (1280, 720))
            copies.append((image, gallery / f'frontier-{quality}-{poster}.png'))
        require(sha256(source / 'reload.png') == sha256(source / 'frame-00110.png'), f'{quality}: reload poster is not the captured frame')
        copies.extend([(source / 'proof.json', validation / f'frontier-{quality}-gameplay.json'), (source / 'player.log', validation / f'frontier-{quality}-gameplay.txt'), (source / 'reload.mp4', gallery / f'frontier-{quality}-reload.mp4')])
        captures[quality] = dict(proof=proof, video=video)

    smoke = root / 'Logs/ranch-smoke'
    builder = read_json(smoke / 'builder.json')
    require(builder == read_json(smoke / 'observer.json'), 'Multiplayer clients diverged')
    require(builder['revision'] == 25 and len(builder['pieces']) == 20 and builder['expeditionStage'] == 3 and builder['milk'] == 1 and builder['eggs'] == 3, 'Incomplete multiplayer ranch')
    appearances = {}
    for role, initial, peer, final, final_peer in [('builder', 'ranch-hand', 'homesteader', 'frontiersman', 'homesteader'), ('observer', 'homesteader', 'ranch-hand', 'homesteader', 'frontiersman')]:
        log = completion(smoke / f'{role}.log', 'RANCH_SMOKE_REVISION', stamp, 'RANCH_SMOKE_FAILED')
        require('RANCH_SMOKE_PASSED 25' in log, f'{role}: multiplayer did not finish')
        appearance = read_json(smoke / f'{role}-appearance.json')
        require(tuple(appearance.get(key) for key in ('initialOwn', 'initialPeer', 'finalOwn', 'finalPeer')) == (initial, peer, final, final_peer), f'{role}: appearance choices mismatch')
        require(appearance.get('initialAvatar') is True and appearance.get('finalAvatar') is True, f'{role}: peer avatar was not rendered')
        require(appearance['revisionBefore'] == appearance['revisionAfter'] == builder['revision'], f'{role}: model change altered ranch revision')
        appearances[role] = appearance
        copies.extend([(smoke / f'{role}-appearance.json', validation / f'frontier-{role}-appearance.json'), (smoke / f'{role}.log', validation / f'frontier-{role}.txt')])
    multiplayer = root / 'Logs/frontier-multiplayer.txt'
    multiplayer_log = multiplayer.read_text(errors='replace')
    require('PASS: distinct explorer models rendered on both peers; live builder change preserved ranch state' in multiplayer_log and 'PASS: two rendered Unity clients and dedicated save agree at revision 25, 20 pieces, milk=1, eggs=3' in multiplayer_log, 'Missing successful shared-save and appearance comparison')
    copies.extend([(multiplayer, validation / 'frontier-multiplayer.txt'), (smoke / 'builder.json', validation / 'frontier-shared-ranch.json')])
    # The baseline is recorded before validation, never manufactured after a run.
    require(baseline.is_file(), f'Missing frozen source manifest: {baseline}')
    subprocess.run([sys.executable, str(root / '.agents/tools/source-manifest.py'), str(baseline), '--verify'], check=True)
    copies.append((baseline, validation / 'frontier-source-files.json'))
    summary = dict(runtimeStamp=stamp, builds=builds, tests=tests, wardrobe=portraits, gameplay=captures, appearances=appearances,
                   sourceFiles=len(read_json(baseline)), qualityEvidence='Requested quality is identified by capture directory; these capture protocols do not record actual QualitySettings names.',
                   savedStateEvidence='ranch-smoke.py compared the temporary dedicated save with both clients before its success line; the temporary save itself is not retained.')
    summary['filesSHA256'] = {str(destination.relative_to(root)): sha256(source) for source, destination in copies}
    return copies, summary


def main():
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-manifest', type=Path, default=Path('Logs/frontier-source-files.json'), help='Frozen pre-validation manifest to verify and retain.')
    parser.add_argument('--require-small-ui', action='store_true', help='Require all four Low wardrobe captures at 960x600 as well.')
    args = parser.parse_args()
    baseline = args.source_manifest if args.source_manifest.is_absolute() else root / args.source_manifest
    try:
        copies, summary = collect(root, baseline, args.require_small_ui)
    except (OSError, ValueError, KeyError, ET.ParseError, subprocess.CalledProcessError) as error:
        raise SystemExit(f'Frontier evidence rejected: {error}') from error
    for source, destination in copies:
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, destination)
    output = root / 'docs/validation/frontier-builds.json'
    output.write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps(dict(runtimeStamp=summary['runtimeStamp'], tests=summary['tests'], sourceFiles=summary['sourceFiles'], evidence=str(output)), indent=2))


if __name__ == '__main__':
    main()
