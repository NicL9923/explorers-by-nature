#!/usr/bin/env python3
"""Capture six real first-person grove views using an already built Linux player."""
import argparse
import os
import pathlib
import subprocess
import tempfile


def main():
    root = pathlib.Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--quality', choices=['low', 'high'], default='high')
    parser.add_argument('--output', type=pathlib.Path)
    parser.add_argument('--player', type=pathlib.Path, default=root / 'Builds/Linux/ExplorersByNature')
    parser.add_argument('--width', type=int, default=1920)
    parser.add_argument('--height', type=int, default=1080)
    parser.add_argument('--timeout', type=float, default=150)
    parser.add_argument('--vulkan-device-index', type=int)
    options = parser.parse_args()
    if options.width < 1 or options.height < 1 or options.timeout <= 0:
        parser.error('dimensions and timeout must be positive')
    if options.vulkan_device_index is not None and options.vulkan_device_index < 0:
        parser.error('Vulkan device index must be nonnegative')
    output = (options.output or root / 'Logs/grove-capture' / options.quality).resolve()
    output.mkdir(parents=True, exist_ok=True)
    player = options.player.resolve()
    if not player.is_file():
        parser.error(f'build the Linux player first: {player}')
    names = ['01-entrance.png', '02-trail-and-doe.png', '03-fern-trail.png', '04-stump-and-floor.png', '05-doe-close.png', '06-overlook.png']
    for name in names:
        (output / name).unlink(missing_ok=True)
    log = output / 'player.log'
    log.unlink(missing_ok=True)
    env = os.environ.copy()
    env.pop('LD_LIBRARY_PATH', None)
    # Unity preferences and persistentDataPath are isolated, including early scene initialization.
    with tempfile.TemporaryDirectory(prefix='explorers-grove-capture-') as temporary:
        env['XDG_CONFIG_HOME'] = str(pathlib.Path(temporary) / 'config')
        env['XDG_DATA_HOME'] = str(pathlib.Path(temporary) / 'data')
        env['TMPDIR'] = temporary
        command = [str(player), '--grove-capture', '--grove-capture-dir', str(output),
                   '--grove-quality', options.quality, '--quality-' + options.quality,
                   '-screen-fullscreen', '0', '-screen-width', str(options.width),
                   '-screen-height', str(options.height), '-logFile', str(log)]
        if options.vulkan_device_index is not None:
            command.extend(['-force-vulkan', '-force-device-index', str(options.vulkan_device_index)])
        try:
            result = subprocess.run(command, env=env, timeout=options.timeout,
                                    stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        except subprocess.TimeoutExpired as error:
            raise SystemExit(f'Capture timed out; inspect {log}') from error
        text = log.read_text(errors='replace') if log.exists() else ''
        if result.returncode or 'GROVE_CAPTURE_COMPLETE ' not in text or 'GROVE_CAPTURE_FAILED ' in text:
            raise SystemExit(f'Capture failed with player exit {result.returncode}; inspect {log}')
        for name in names:
            path = output / name
            if not path.exists() or path.stat().st_size < 1000 or path.read_bytes()[:8] != b'\x89PNG\r\n\x1a\n':
                raise SystemExit(f'Missing or invalid capture: {path}')
    print(f'Captured {options.quality} grove views: {output}')


if __name__ == '__main__':
    main()
