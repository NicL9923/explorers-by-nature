import json,pathlib,urllib.request,hashlib,concurrent.futures
root=pathlib.Path(__file__).resolve().parents[2]
entries=[]; jobs=[]
for short in ['pine','fir']:
 id=short+'_tree_01';d=json.load(urllib.request.urlopen('https://api.polyhaven.com/files/'+id));folder=root/'ArtSource/Reference/raw'/id;folder.mkdir(exist_ok=True)
 (folder/'files.json').write_text(json.dumps(d,indent=2))
 item=d['blend']['2k']['blend']; selected={id+'_2k.blend':item}
 selected.update({p:v for p,v in item['include'].items() if any(s in p for s in ['_diff_','_nor_gl_','_alpha_'])})
 if short=='pine': selected['textures/pine_tree_01_twig_diff_2k.jpg']=d['twig_diff']['2k']['jpg']
 entry={'id':id,'source':'https://polyhaven.com/a/'+id,'license':'CC0-1.0','license_url':'https://polyhaven.com/license','files':[]};entries.append(entry)
 for p,v in selected.items():jobs.append((folder/p,v,entry))
def get(job):
 p,v,e=job;p.parent.mkdir(exist_ok=True)
 if not p.exists():urllib.request.urlretrieve(v['url'],p)
 data=p.read_bytes();assert hashlib.md5(data).hexdigest()==v['md5'],p
 e['files'].append({'path':str(p.relative_to(root)),'url':v['url'],'md5':v['md5'],'sha256':hashlib.sha256(data).hexdigest()});print(p.name,flush=True)
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:list(pool.map(get,jobs))
(root/'docs/reference-trees.json').write_text(json.dumps({'assets':entries},indent=2)+'\n')
