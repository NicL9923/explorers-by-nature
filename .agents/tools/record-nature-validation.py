#!/usr/bin/env python3
"""Record matching desktop builds, motion captures and Riverbend benchmarks."""
import argparse
from fractions import Fraction
import hashlib
import json
import math
from pathlib import Path
import re
import shutil
import subprocess


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_json(path):
    return json.loads(path.read_text())


def collect(root, prefix):
    """Validate every input before copying any release evidence."""
    builds = {}
    for platform in ('Linux', 'Windows'):
        level = root / f'Builds/{platform}/ExplorersByNature_Data/level0'
        data = level.read_bytes()
        stamps = set(re.findall(rb'[a-f0-9]{7,40}/sha256:[a-f0-9]{64}', data))
        require(len(stamps) == 1, f'{platform}: missing or ambiguous runtime stamp')
        require('Build Finished, Result: Success.' in (root / f'Logs/{platform.lower()}.log').read_text(errors='replace'), f'{platform}: build did not succeed')
        builds[platform.lower()] = dict(runtimeStamp=stamps.pop().decode(), level0SHA256=hashlib.sha256(data).hexdigest())
    stamp = builds['linux']['runtimeStamp']
    require(builds['windows']['runtimeStamp'] == stamp, 'Linux and Windows build stamps differ')
    validation = root / 'docs/validation'
    gallery = root / 'docs/benchmarks'
    copies, proofs, metrics, videos = [], {}, {}, {}
    for quality, clumps in (('high', 1800), ('low', 600)):
        source = root / f'Logs/nature-motion/{quality}'
        proof = read_json(source / 'proof.json')
        require(proof.get('revision') == stamp, f'{quality}: stale motion capture')
        require(proof.get('quality') == quality, f'{quality}: requested quality mismatch')
        require(proof.get('actualQualityName') == quality.title(), f'{quality}: actual player quality mismatch')
        require(proof.get('frames') == 480 and proof.get('fps') == 24, f'{quality}: expected 480 frames at 24 FPS')
        require(proof.get('activeFurClumps') == clumps, f'{quality}: incorrect fur budget')
        require(proof.get('waterImpacts', 0) >= 1, f'{quality}: missing water interaction')
        bend = proof.get('maximumFurBend', 0)
        require(math.isfinite(bend) and bend > .001, f'{quality}: missing measurable spring motion')
        log = (source / 'player.log').read_text(errors='replace')
        require('NATURE_MOTION_COMPLETE ' in log and 'NATURE_MOTION_FAILED ' not in log, f'{quality}: capture did not complete')
        # Validate the encoded clip itself, not merely a JSON assertion about it.
        result = subprocess.run(['ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_entries', 'stream=nb_frames,r_frame_rate,width,height,duration', '-of', 'json', str(source / 'motion.mp4')], check=True, capture_output=True, text=True)
        stream = json.loads(result.stdout)['streams'][0]
        require(int(stream['nb_frames']) == 480 and Fraction(stream['r_frame_rate']) == 24, f'{quality}: video frame count/rate mismatch')
        require((int(stream['width']), int(stream['height'])) == (1280, 720), f'{quality}: unexpected recording resolution')
        require(abs(float(stream['duration']) - 20) < .05, f'{quality}: unexpected clip duration')
        videos[quality] = stream
        copies.extend([(source / 'proof.json', validation / f'{prefix}-{quality}-motion.json'),
                       (source / 'player.log', validation / f'{prefix}-{quality}-motion.txt'),
                       (source / 'motion.mp4', gallery / f'{prefix}-{quality}-motion.mp4')])
        for frame, poster in ((48, 'wind'), (144, 'fur'), (240, 'tail'), (384, 'river')):
            image = source / f'{poster}.png'
            data = image.read_bytes()
            require(data.startswith(b'\x89PNG\r\n\x1a\n'), f'{quality}: invalid {poster} poster')
            # These are the exact middle frames exported by NatureMotionCapture.
            require(data == (source / f'frame-{frame:05d}.png').read_bytes(), f'{quality}: stale {poster} poster')
            copies.append((image, gallery / f'{prefix}-{quality}-{poster}.png'))
        candidates = []
        for path in (root / 'Logs/benchmarks').glob('*.json'):
            report = read_json(path)
            if report.get('revision') == stamp and report.get('route') == 'Riverbend' and report.get('quality', '').lower() == quality:
                candidates.append((path, report))
        require(candidates, f'{quality}: no Riverbend benchmark matches the current builds')
        benchmark, report = max(candidates, key=lambda pair: pair[0].stat().st_mtime_ns)
        copies.append((benchmark, validation / f'{prefix}-{quality}-river.json'))
        proofs[quality], metrics[quality] = proof, report
    hashes = {}
    for source, destination in copies:
        hashes[str(destination.relative_to(root))] = hashlib.sha256(source.read_bytes()).hexdigest()
    summary = dict(stamp=stamp, builds=builds, proofs=proofs, metrics=metrics, videos=videos, filesSHA256=hashes)
    return copies, summary


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--prefix', default='wind-water', help='Evidence filename prefix.')
    args = parser.parse_args()
    if not re.fullmatch(r'[a-z0-9-]+', args.prefix):
        parser.error('prefix must contain lowercase letters, digits or hyphens')
    root = Path(__file__).resolve().parents[2]
    try:
        copies, summary = collect(root, args.prefix)
    except (ValueError, KeyError, OSError, subprocess.CalledProcessError) as error:
        raise SystemExit(f'Nature evidence rejected: {error}') from error
    for source, destination in copies:
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, destination)
    (root / f'docs/validation/{args.prefix}-motion-builds.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps(summary, indent=2))


if __name__ == '__main__':
    main()
