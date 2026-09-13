#!/usr/bin/env python3
"""Rebake and export both trees with a valid temporary color configuration on Fedora Blender."""
import os
import pathlib
import re
import subprocess
import tempfile

root = pathlib.Path(__file__).resolve().parents[2]
env = os.environ.copy()
with tempfile.TemporaryDirectory(prefix='reference-tree-bake-') as temporary:
    if 'OCIO' not in env:
        configurations = sorted(pathlib.Path('/usr/share/blender').glob('*/datafiles/colormanagement/config.ocio'))
        if not configurations:
            raise SystemExit('Set OCIO to the Blender color configuration before baking.')
        original = configurations[-1]
        text = original.read_text()
        # Fedora packages Blender's 2.5 config with OCIO 2.4. The new interoperability
        # metadata is unnecessary for these sRGB/tangent-map bakes; transforms stay intact.
        text = text.replace('ocio_profile_version: 2.5', 'ocio_profile_version: 2.4')
        text = re.sub(r'^\s*(interop_id|interchange):.*\n', '', text, flags=re.MULTILINE)
        text = text.replace('search_path: "icc:luts:filmic"',
                            'search_path: "' + ':'.join(str(original.parent / p) for p in ['icc', 'luts', 'filmic']) + '"')
        config = pathlib.Path(temporary) / 'config.ocio'
        config.write_text(text)
        env['OCIO'] = str(config)
    for name in ['pine', 'fir']:
        asset = name + '_tree_01'
        source = root / 'ArtSource/Reference/raw' / asset / (asset + '_2k.blend')
        subprocess.run(['blender', '-b', str(source), '-P', str(root / '.agents/tools/import-reference-trees.py')],
                       check=True, cwd=root, env=env)
