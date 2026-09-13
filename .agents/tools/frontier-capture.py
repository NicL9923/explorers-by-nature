#!/usr/bin/env python3
"""Exercise and record actual player gathering, riding, hunting and reload animation."""
import argparse,json,os,pathlib,subprocess,tempfile
root=pathlib.Path(__file__).resolve().parents[2];p=argparse.ArgumentParser(description=__doc__);p.add_argument('--quality',choices=['low','high'],default='high');p.add_argument('--vulkan-device-index',type=int);a=p.parse_args()
out=root/'Logs/frontier-capture'/a.quality;out.mkdir(parents=True,exist_ok=True)
for old in out.glob('frame-*.png'):old.unlink()
env=os.environ.copy();env.pop('LD_LIBRARY_PATH',None)
with tempfile.TemporaryDirectory(prefix='explorers-frontier-') as t:
 env.update(XDG_CONFIG_HOME=t+'/config',XDG_DATA_HOME=t+'/data',TMPDIR=t)
 cmd=[str(root/'Builds/Linux/ExplorersByNature'),'--frontier-capture','--capture-output',str(out),'--quality-'+a.quality,'--validation-fps','30','-screen-width','1280','-screen-height','720','-screen-fullscreen','0','-logFile',str(out/'player.log')]
 if a.vulkan_device_index is not None:cmd+=['-force-vulkan','-force-device-index',str(a.vulkan_device_index)]
 run=subprocess.run(cmd,env=env,timeout=180,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
 log=(out/'player.log').read_text(errors='replace')
 if run.returncode or 'FRONTIER_CAPTURE_COMPLETE ' not in log:raise SystemExit('Capture failed: '+str(out/'player.log'))
 proof=json.loads((out/'proof.json').read_text());assert len(list(out.glob('frame-*.png')))==proof['frames']==240
 subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-framerate','24','-i',str(out/'frame-%05d.png'),'-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'reload.mp4')],env=env,check=True)
 print(json.dumps(proof,indent=2))
