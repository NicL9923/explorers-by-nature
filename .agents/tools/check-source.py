#!/usr/bin/env python3
"""Compile C# against installed Unity/template assemblies without running the editor.

This catches API/type errors only. It does not import assets, compile shaders, run
Unity tests, validate serialization, or replace a licensed Unity player build.
"""
from pathlib import Path
import os
import subprocess
import tempfile
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[2]
version = next(line.split(': ', 1)[1] for line in (root / 'ProjectSettings/ProjectVersion.txt').read_text().splitlines() if line.startswith('m_EditorVersion:'))
editor = Path(os.environ.get('UNITY_EDITOR', str(Path.home() / 'Unity/Hub/Editor' / version / 'Editor/Unity'))).parent
cache = editor / 'Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies'
if not cache.is_dir():
    raise SystemExit('Expected template assemblies are missing; run real Unity validation instead.')
project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
properties = ET.SubElement(project, 'PropertyGroup')
for name, value in {'TargetFramework': 'netstandard2.1', 'EnableDefaultCompileItems': 'false', 'GenerateAssemblyInfo': 'false', 'LangVersion': '9.0'}.items():
    ET.SubElement(properties, name).text = value
items = ET.SubElement(project, 'ItemGroup')
for source in sorted((root / 'Assets').rglob('*.cs')):
    ET.SubElement(items, 'Compile', Include=str(source))
references = list((editor / 'Data/Managed/UnityEngine').glob('*.dll'))
references += [path for path in (editor / 'Data/Managed').glob('UnityEditor*.dll') if path.name != 'UnityEditor.dll']
references += list(cache.glob('Unity.RenderPipelines.*.dll'))
references += [editor / 'Data/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net40/unity-custom/nunit.framework.dll']
for path in references:
    reference = ET.SubElement(items, 'Reference', Include=path.stem)
    ET.SubElement(reference, 'HintPath').text = str(path)
with tempfile.TemporaryDirectory(prefix='explorers-source-check-') as directory:
    path = Path(directory) / 'Check.csproj'
    ET.ElementTree(project).write(path)
    subprocess.run(['dotnet', 'build', str(path), '--nologo', '-v', 'quiet'], check=True)
