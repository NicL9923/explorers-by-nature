#!/usr/bin/env python3
"""Capture all four actual in-game explorer choices with isolated preferences."""
import argparse,os,pathlib,subprocess,tempfile
root=pathlib.Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--quality',choices=['low','high'],default='high');p.add_argument('--width',type=int,default=1280);p.add_argument('--height',type=int,default=720);p.add_argument('--vulkan-device-index',type=int)
a=p.parse_args();out=root/'Logs/player-capture'/f'{a.quality}-{a.width}x{a.height}';out.mkdir(parents=True,exist_ok=True)
env=os.environ.copy();env.pop('LD_LIBRARY_PATH',None)
with tempfile.TemporaryDirectory(prefix='explorers-wardrobe-') as t:
 env.update(XDG_CONFIG_HOME=t+'/config',XDG_DATA_HOME=t+'/data',TMPDIR=t)
 cmd=[str(root/'Builds/Linux/ExplorersByNature'),'--player-model-capture','--capture-output',str(out),'--quality-'+a.quality,'--validation-fps','30','-screen-width',str(a.width),'-screen-height',str(a.height),'-screen-fullscreen','0','-logFile',str(out/'player.log')]
 if a.vulkan_device_index is not None:cmd+=['-force-vulkan','-force-device-index',str(a.vulkan_device_index)]
 subprocess.run(cmd,env=env,check=True,timeout=90,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
 log=(out/'player.log').read_text(errors='replace');assert 'PLAYER_CAPTURE_COMPLETE ' in log and 'PLAYER_CAPTURE_FAILED' not in log
 for name in ['ranch-hand','trail-scout','homesteader','frontiersman']:assert (out/(name+'.png')).read_bytes().startswith(b'\x89PNG\r\n\x1a\n')
 print(out)
