from pathlib import Path
from zipfile import ZipFile
import hashlib,json
root=Path(__file__).resolve().parents[2]
source=root/'Docs/Assets/Sources/SalvageNext/KenneyUI'
dest=root/'Assets/EchoStation/SalvageNext/ArtV2/UI/Kenney'
dest.mkdir(parents=True,exist_ok=True)
selection={'ui-pack-sci-fi':{'Panel.png':'PNG/Grey/Default/button_square_header_small_rectangle.png','Button.png':'PNG/Grey/Default/bar_square_large.png'},'crosshair-pack':{'Crosshair.png':'PNG/Light/crosshair-000.png'},'input-prompts':{k.upper()+'.png':'Keyboard & Mouse/Default/keyboard_'+k+'.png' for k in ['e','g','r','tab','f','t']}}
rows=[]
for pack,files in selection.items():
 receipt=json.loads((source/(pack+'_receipt.json')).read_text('utf-8')); blob=(source/(pack+'.zip')).read_bytes()
 assert hashlib.sha256(blob).hexdigest()==receipt['sha256']
 with ZipFile(source/(pack+'.zip')) as z:
  for output,original in files.items():
   data=z.read(original); path=dest/output
   if path.exists(): assert path.read_bytes()==data
   else: path.write_bytes(data)
   rows.append({'output':output,'sourceFile':original,'source':receipt['source'],'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data)})
 (dest/(pack+'_LICENSE.txt')).write_bytes((source/(pack+'_LICENSE.txt')).read_bytes())
(dest/'SOURCES.json').write_text(json.dumps({'license':'CC0-1.0','files':rows},ensure_ascii=False,indent=2),'utf-8')
print(json.dumps({'imported':len(rows),'bytes':sum(r['bytes'] for r in rows)}))
