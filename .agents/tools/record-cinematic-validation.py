#!/usr/bin/env python3
"""Verify frozen cinematic builds and rendered evidence, then collect the release ledger."""
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

SHOTS = ('01-entrance', '02-trail-and-doe', '03-fern-trail', '04-stump-and-floor',
         '05-doe-close', '06-overlook', '07-river', '08-sunset-clouds', '09-rainy-river')
STAMP = r'[a-f0-9]{7,40}/sha256:[a-f0-9]{64}'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read_json(path):
    return json.loads(path.read_text())


def png(path, size):
    with path.open('rb') as stream:
        header = stream.read(24)
    require(header[:8] == b'\x89PNG\r\n\x1a\n' and header[12:16] == b'IHDR', f'Invalid PNG: {path}')
    require(struct.unpack('>II', header[16:24]) == size, f'Wrong PNG size: {path}')


def log_stamp(path, marker, stamp, failure):
    text = path.read_text(errors='replace')
    require(marker + ' ' + stamp in text and failure not in text, f'Stale or failed evidence: {path}')
    return text


def collect(root, baseline):
    require(baseline.is_file(), f'Missing pre-validation source manifest: {baseline}')
    subprocess.run([sys.executable, str(root / '.agents/tools/source-manifest.py'), str(baseline), '--verify'], check=True)
    copies = []
    validation, gallery = root / 'docs/validation', root / 'docs/benchmarks'
    def evidence(source, name):
        copies.append((source, validation / ('cinematic-' + name)))
    def picture(source, name, size):
        png(source, size)
        copies.append((source, gallery / ('cinematic-' + name + '.png')))
    builds = {}
    for platform in ('Linux', 'Windows'):
        level = root / f'Builds/{platform}/ExplorersByNature_Data/level0'
        stamps = set(re.findall(STAMP.encode(), level.read_bytes()))
        require(len(stamps) == 1, f'{platform}: missing or ambiguous embedded build stamp')
        log = root / f'Logs/{platform.lower()}.log'
        require('Build Finished, Result: Success.' in log.read_text(errors='replace'), f'{platform}: unsuccessful build')
        builds[platform.lower()] = dict(runtimeStamp=stamps.pop().decode(), level0SHA256=digest(level), nativeExecution=platform == 'Linux')
        evidence(log, platform.lower() + '-build.txt')
    stamp = builds['linux']['runtimeStamp']
    require(builds['windows']['runtimeStamp'] == stamp, 'Linux and Windows stamps differ')
    tests = {}
    for name, minimum in (('test', 44), ('playtest', 25)):
        path = root / f'Logs/{name}.xml'
        result = ET.parse(path).getroot()
        count = int(result.get('passed', '0'))
        require(result.get('result') == 'Passed' and result.get('failed') == '0' and count >= minimum, f'{name}: expected at least {minimum} passing tests')
        tests[name] = count
        evidence(path, name + '.xml')
    groves, motion = {}, {}
    for quality, size in (('high', (1920, 1080)), ('low', (1280, 720))):
        source = root / f'Logs/cinematic-final/{quality}'
        text = log_stamp(source / 'player.log', 'GROVE_CAPTURE_REVISION', stamp, 'GROVE_CAPTURE_FAILED')
        require(re.search(r'GROVE_CAPTURE_COMPLETE .* quality=' + quality + r' resolution=' + str(size[0]) + 'x' + str(size[1]) + r'(?:\s|$)', text), f'{quality}: incomplete grove capture or wrong quality/resolution')
        for shot in SHOTS:
            picture(source / (shot + '.png'), quality + '-' + shot, size)
        evidence(source / 'player.log', quality + '-grove.txt')
        groves[quality] = dict(width=size[0], height=size[1], shots=list(SHOTS))

    def film(source, name, marker, frames, filename, posters, poster_frames):
        proof = read_json(source / 'proof.json')
        require(proof.get('revision') == stamp and proof.get('frames') == frames and proof.get('fps') == 24, f'{name}: stale or incomplete motion proof')
        log_stamp(source / 'player.log', marker + '_COMPLETE', stamp, marker + '_FAILED')
        expected = [f'frame-{frame:05d}.png' for frame in range(frames)]
        require(sorted(p.name for p in source.glob('frame-*.png')) == expected, f'{name}: missing or unexpected frames')
        for frame in expected:
            png(source / frame, (1280, 720))
        video = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_entries', 'stream=nb_frames,r_frame_rate,width,height,duration', '-of', 'json', str(source / filename)], text=True, timeout=20))['streams']
        require(len(video) == 1, f'{name}: expected one video stream')
        video = video[0]
        require(int(video['nb_frames']) == frames and Fraction(video['r_frame_rate']) == 24 and (int(video['width']), int(video['height'])) == (1280, 720) and abs(float(video['duration']) - frames / 24) < .05, f'{name}: encoded movie differs from capture')
        for poster, frame in zip(posters, poster_frames):
            path = source / (poster + '.png')
            picture(path, name + '-' + poster, (1280, 720))
            require(digest(path) == digest(source / f'frame-{frame:05d}.png'), f'{name}: poster is not the recorded frame: {poster}')
        copies.append((source / filename, gallery / f'cinematic-{name}-{filename}'))
        evidence(source / 'proof.json', name + '-motion.json')
        evidence(source / 'player.log', name + '-motion.txt')
        return proof, video

    for quality, clumps in (('high', 5400), ('low', 600)):
        proof, video = film(root / f'Logs/cinematic-motion-final/{quality}', quality, 'NATURE_MOTION', 480, 'motion.mp4', ('wind', 'fur', 'tail', 'river'), (48, 144, 240, 384))
        require(proof.get('actualQualityName', '').lower() == quality and proof.get('quality') == quality and proof.get('actualQualityIndex') == (1 if quality == 'high' else 0), f'{quality}: wrong actual quality')
        require(proof.get('activeFurClumps') == clumps and math.isfinite(proof.get('maximumFurBend', 0)) and proof['maximumFurBend'] > .001 and proof.get('waterImpacts', 0) > 0, f'{quality}: missing fur or water response')
        motion[quality] = dict(proof=proof, video=video)
    proof, video = film(root / 'Logs/western-hair-capture/high', 'western-hair', 'WESTERN_HAIR', 240, 'hair.mp4', ('horse-mane', 'horse-tail', 'explorer-temple', 'explorer-back'), (30, 90, 150, 210))
    require(proof.get('quality') == 'High' and (proof.get('width'), proof.get('height')) == (1280, 720) and (proof.get('horseGuides'), proof.get('playerGuides')) == (370, 110), 'Incorrect western hair quality, size or groom counts')
    for field in ('horseMaximumBend', 'playerMaximumBend'):
        require(math.isfinite(proof.get(field, 0)) and proof[field] > .001, f'Missing western hair movement: {field}')
    hair = dict(proof=proof, video=video)

    smoke = root / 'Logs/ranch-smoke'
    ranch = read_json(smoke / 'builder.json')
    require(ranch == read_json(smoke / 'observer.json'), 'Multiplayer clients diverged')
    require(ranch['revision'] == 25 and len(ranch['pieces']) == 20 and ranch['expeditionStage'] == 3 and ranch['milk'] == 1 and ranch['eggs'] == 3, 'Incomplete multiplayer ranch')
    for role in ('builder', 'observer'):
        text = log_stamp(smoke / f'{role}.log', 'RANCH_SMOKE_REVISION', stamp, 'RANCH_SMOKE_FAILED')
        require('RANCH_SMOKE_PASSED 25' in text, f'{role}: multiplayer incomplete')
        evidence(smoke / f'{role}.log', role + '.txt')
    multiplayer = root / 'Logs/cinematic-multiplayer.txt'
    require('PASS: two rendered Unity clients and dedicated save agree at revision 25, 20 pieces, milk=1, eggs=3' in multiplayer.read_text(errors='replace'), 'Missing dedicated save comparison')
    evidence(multiplayer, 'multiplayer.txt')
    evidence(smoke / 'builder.json', 'shared-ranch.json')
    before = root / 'Logs/cinematic-before'
    before_log = (before / 'player.log').read_text(errors='replace')
    prior = re.findall(r'GROVE_CAPTURE_REVISION (' + STAMP + ')', before_log)
    require(len(set(prior)) == 1 and prior[0] != stamp and 'GROVE_CAPTURE_COMPLETE ' in before_log and 'GROVE_CAPTURE_FAILED' not in before_log, 'Baseline is not a completed prior build capture')
    for shot in ('01-entrance', '06-overlook'):
        picture(before / (shot + '.png'), 'before-' + shot, (1920, 1080))
    evidence(before / 'player.log', 'before-grove.txt')
    evidence(baseline, 'source-files.json')
    summary = dict(runtimeStamp=stamp, builds=builds, tests=tests, grove=groves, natureMotion=motion, westernHair=hair, baseline=dict(runtimeStamp=prior[0], purpose='Prior build visual comparison only; not final-state validation'), sourceFiles=len(read_json(baseline)), limitations=['Recordings use fixed 24 FPS simulation and capped rendering; no performance measurements or minimum hardware claims.', 'Windows was cross-built, not run natively.', 'The temporary dedicated save was compared during multiplayer smoke; only matching client state and success log are retained.', 'Recorded guide counts and movement verify simulation activity, not photographic realism.'])
    summary['filesSHA256'] = {str(destination.relative_to(root)): digest(source) for source, destination in copies}
    return copies, summary


def main():
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-manifest', type=Path, default=Path('Logs/cinematic-source-files.json'))
    args = parser.parse_args()
    baseline = args.source_manifest if args.source_manifest.is_absolute() else root / args.source_manifest
    try:
        copies, summary = collect(root, baseline)
    except (OSError, ValueError, KeyError, ET.ParseError, subprocess.SubprocessError) as error:
        raise SystemExit(f'Cinematic evidence rejected: {error}') from error
    for source, destination in copies:
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, destination)
    output = root / 'docs/validation/cinematic-builds.json'
    output.write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps(dict(runtimeStamp=summary['runtimeStamp'], tests=summary['tests'], sourceFiles=summary['sourceFiles'], evidence=str(output)), indent=2))


if __name__ == '__main__':
    main()
