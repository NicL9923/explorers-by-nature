"""Restore recorded downloads and pack Unity color/normal/metallic-smoothness maps."""
import hashlib,json,pathlib,urllib.request
from PIL import Image,ImageOps
ROOT=pathlib.Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Resources/ReferenceGround'
manifest=json.loads((ROOT/'docs/reference-ground-assets.json').read_text())
for entry in manifest['files']:
 p=ROOT/entry['path']
 if not p.exists():
  p.parent.mkdir(parents=True,exist_ok=True);urllib.request.urlretrieve(entry['url'],p)
 assert hashlib.sha256(p.read_bytes()).hexdigest()==entry['sha256'],p
for source,name in [('fern_02','Fern'),('tree_stump_01','Stump'),('rock_moss_set_01','Rock'),('forest_leaves_04','ForestFloor'),('forest_ground_04','TrailGround')]:
 d=ROOT/'ArtSource/ReferenceGround'/source
 im=Image.open(d/(source+'_diff_2k.png')).convert('RGBA')
 rough=Image.open(d/(source+'_rough_2k.png')).convert('L')
 if source=='fern_02':im.putalpha(Image.open(d/(source+'_alpha_2k.png')).convert('L'))
 # Terrain Lit consumes diffuse alpha as smoothness, even when an opaque source
 # imports into an alpha-capable compression format. Encode its intended value.
 if name in ('ForestFloor','TrailGround'):im.putalpha(ImageOps.invert(rough))
 im.save(OUT/(name+'_BaseColor.png'))
 Image.open(d/(source+'_nor_gl_2k.png')).convert('RGB').save(OUT/(name+'_Normal.png'))
 rough.save(OUT/(name+'_Roughness.png'))
 packed=Image.new('RGBA',rough.size,(0,0,0,255));packed.putalpha(ImageOps.invert(rough));packed.save(OUT/(name+'_MetallicSmoothness.png'))
