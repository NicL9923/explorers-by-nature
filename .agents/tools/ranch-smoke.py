#!/usr/bin/env python3
"""Exercise two rendered Unity clients against the published .NET authority."""
import argparse, json, os, pathlib, socket, subprocess, tempfile, time
parser=argparse.ArgumentParser();parser.add_argument("--art-tour",action="store_true");options=parser.parse_args()
root=pathlib.Path(__file__).resolve().parents[2]
out=root/'Logs'/'ranch-smoke';out.mkdir(parents=True,exist_ok=True)
env=os.environ.copy();env.pop('LD_LIBRARY_PATH',None)
with socket.socket() as s:s.bind(('127.0.0.1',0));port=s.getsockname()[1]
with tempfile.TemporaryDirectory(prefix='explorers-server-smoke-') as data:
 server=subprocess.Popen([str(root/'Builds/Server/linux-x64/Explorers.Server'),'--port',str(port),'--code','integration-check','--data',data],stdout=(out/'server.log').open('w'),stderr=subprocess.STDOUT,env=env)
 players=[]
 try:
  for _ in range(50):
   try:
    with socket.create_connection(('127.0.0.1',port),timeout=.2):break
   except OSError:time.sleep(.1)
  for role in ['builder','observer']:
   args=[str(root/'Builds/Linux/ExplorersByNature'),'--ranch-smoke','--ranch-host','127.0.0.1','--ranch-port',str(port),'--ranch-code','integration-check','--smoke-output',str(out),'--quality-low','-screen-width','1280','-screen-height','720','-logFile',str(out/f'{role}.log')]
   if role=='observer':args+=['--smoke-observer']
   elif options.art_tour:
    args.remove("--quality-low");args += ["--art-tour","--quality-high"]
   players.append(subprocess.Popen(args,env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL))
  for player in players:
   if player.wait(timeout=75)!=0:raise RuntimeError('Unity client failed; see Logs/ranch-smoke')
  builder=json.loads((out/'builder.json').read_text());observer=json.loads((out/'observer.json').read_text())
  assert builder==observer,'Clients diverged'
  saved=json.loads((pathlib.Path(data)/'ranch.json').read_text());assert saved==builder,'Disk and clients diverged'
  assert builder['expeditionStage']==3 and len(builder['pieces'])==20,'Expedition and furnishing loop incomplete'
  print(f'PASS: two rendered Unity clients and dedicated save agree at revision {builder["revision"]}, {len(builder["pieces"])} pieces, milk={builder["milk"]}, eggs={builder["eggs"]}')
 finally:
  for player in players:
   if player.poll() is None:player.terminate();player.wait(timeout=10)
  server.terminate();server.wait(timeout=10)
