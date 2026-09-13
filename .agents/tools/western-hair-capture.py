#!/usr/bin/env python3
"""Capture four live High-quality western hair close-ups and a 10-second motion film."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--vulkan-device-index', type=int)
parser.add_argument('--output', type=Path)
args = parser.parse_args()
output = (args.output or root / 'Logs/western-hair-capture/high').resolve()
output.mkdir(parents=True, exist_ok=True)
posters = ['horse-mane', 'horse-tail', 'explorer-temple', 'explorer-back']
for old in output.glob('frame-*.png'):
    old.unlink()
for name in ['proof.json', 'player.log', 'hair.mp4'] + [name + '.png' for name in posters]:
    (output / name).unlink(missing_ok=True)
env = os.environ.copy()
env.pop('LD_LIBRARY_PATH', None)
with tempfile.TemporaryDirectory(prefix='explorers-western-hair-') as temporary:
    env.update(XDG_CONFIG_HOME=temporary + '/config', XDG_DATA_HOME=temporary + '/data', TMPDIR=temporary)
    command = [str(root / 'Builds/Linux/ExplorersByNature'), '--western-hair-capture',
               '--validation-fps', '30', '--hair-output', str(output), '--quality-high',
               '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
               '-logFile', str(output / 'player.log')]
    if args.vulkan_device_index is not None:
        command += ['-force-vulkan', '-force-device-index', str(args.vulkan_device_index)]
    result = subprocess.run(command, env=env, timeout=240, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    log = (output / 'player.log').read_text(errors='replace')
    if result.returncode or 'WESTERN_HAIR_COMPLETE ' not in log or 'WESTERN_HAIR_FAILED ' in log:
        raise SystemExit('Western hair capture failed; inspect ' + str(output / 'player.log'))
    proof = json.loads((output / 'proof.json').read_text())
    if proof['frames'] != 240 or proof['fps'] != 24 or proof['quality'] != 'High':
        raise SystemExit('Unexpected capture timing or quality')
    if proof['width'] != 1280 or proof['height'] != 720 or proof['horseGuides'] != 370 or proof['playerGuides'] != 110:
        raise SystemExit('Unexpected resolution or groom count')
    if min(proof['horseMaximumBend'], proof['playerMaximumBend']) < .001:
        raise SystemExit('Missing measured groom movement')
    if 'WESTERN_HAIR_COMPLETE ' + proof['revision'] not in log:
        raise SystemExit('Build stamp mismatch')
    for frame in range(240):
        if not (output / f'frame-{frame:05d}.png').is_file():
            raise SystemExit(f'Missing frame {frame}')
    for name in posters:
        if not (output / (name + '.png')).is_file():
            raise SystemExit('Missing poster ' + name)
    subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-framerate', '24',
                    '-i', str(output / 'frame-%05d.png'), '-c:v', 'libx264', '-threads', '2',
                    '-preset', 'medium', '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart',
                    str(output / 'hair.mp4')], env=env, check=True, timeout=120)
    video = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-select_streams', 'v:0',
                       '-show_entries', 'stream=width,height,nb_frames,r_frame_rate', '-of', 'json',
                       str(output / 'hair.mp4')], env=env, timeout=15))['streams'][0]
    if video['width'] != 1280 or video['height'] != 720 or int(video['nb_frames']) != 240 or video['r_frame_rate'] != '24/1':
        raise SystemExit('Encoded movie does not match capture')
print(json.dumps(proof, indent=2))
print(output / 'hair.mp4')
