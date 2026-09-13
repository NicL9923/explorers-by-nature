#!/usr/bin/env python3
"""Record unedited player frames of wind, spring-driven fur and river interaction."""
import argparse,json,os,pathlib,subprocess,tempfile
root=pathlib.Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--quality',choices=['low','high'],default='high')
p.add_argument('--vulkan-device-index',type=int)
p.add_argument('--output',type=pathlib.Path)
a=p.parse_args()
out=(a.output or root/'Logs/nature-motion'/a.quality).resolve();out.mkdir(parents=True,exist_ok=True)
for old in out.glob('frame-*.png'):old.unlink()
for name in ['proof.json','player.log','motion.mp4']:(out/name).unlink(missing_ok=True)
env=os.environ.copy();env.pop('LD_LIBRARY_PATH',None)
with tempfile.TemporaryDirectory(prefix='explorers-motion-') as temporary:
 env.update(XDG_CONFIG_HOME=temporary+'/config',XDG_DATA_HOME=temporary+'/data',TMPDIR=temporary)
 cmd=[str(root/'Builds/Linux/ExplorersByNature'),'--nature-motion-capture','--motion-output',str(out),'--motion-quality',a.quality,'--quality-'+a.quality,'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',str(out/'player.log')]
 if a.vulkan_device_index is not None:cmd+=['-force-vulkan','-force-device-index',str(a.vulkan_device_index)]
 result=subprocess.run(cmd,env=env,timeout=240,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
 log=(out/'player.log').read_text(errors='replace')
 if result.returncode or 'NATURE_MOTION_COMPLETE ' not in log or 'NATURE_MOTION_FAILED ' in log:raise SystemExit('Motion recording failed; inspect '+str(out/'player.log'))
 proof=json.loads((out/'proof.json').read_text())
 if len(list(out.glob('frame-*.png')))!=proof['frames']:raise SystemExit('Missing motion frames')
 subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-framerate',str(proof['fps']),'-i',str(out/'frame-%05d.png'),'-c:v','libx264','-preset','medium','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'motion.mp4')],env=env,check=True)
print(json.dumps(proof,indent=2));print(out/'motion.mp4')
