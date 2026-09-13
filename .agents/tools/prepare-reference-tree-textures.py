"""Pack original 2K source albedo + needle alpha and copy OpenGL normals for Unity."""
import pathlib,uuid,re
from PIL import Image
root=pathlib.Path(__file__).resolve().parents[2];out=root/'Assets/Resources/ReferenceTrees'
for name,id in [('Pine','pine_tree_01'),('Fir','fir_tree_01')]:
 source=root/'ArtSource/Reference/raw'/id/'textures'
 # Trunk atlases are baked from the source's scanned-base/triplanar material by the importer.
 for part in ['bark','twig']:
  # Pine PNG contains invalid white RGB under embedded transparency; the official
  # opaque JPEG preserves the correct needle RGB when packing the standalone opacity map.
  extension='jpg' if name=='Pine' and part=='twig' else 'png'
  color=Image.open(source/(id+'_'+part+'_diff_2k.'+extension)).convert('RGBA')
  if part=='twig':color.putalpha(Image.open(source/(id+'_twig_alpha_2k.png')).convert('L'))
  color.save(out/(name+'_'+part+'_BaseColor.png'))
  Image.open(source/(id+'_'+part+'_nor_gl_2k.png')).convert('RGB').save(out/(name+'_'+part+'_Normal.png'))
normal=(root/'Assets/Resources/HenDetail_Normal.png.meta').read_text()
model=next((root/'Assets/Resources').glob('**/*.fbx.meta')).read_text()
for p in out.iterdir():
 if p.suffix not in ['.fbx','.png']:continue
 meta=p.with_name(p.name+'.meta')
 if meta.exists():continue
 s=model if p.suffix=='.fbx' else normal
 if p.suffix=='.png' and 'BaseColor' in p.name:s=s.replace('sRGBTexture: 0','sRGBTexture: 1').replace('textureType: 1','textureType: 0')
 s=re.sub(r'guid: \w+', 'guid: '+uuid.uuid4().hex,s,count=1)
 meta.write_text(s)
