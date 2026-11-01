"""Pinned original weights and official NuGet archives for compatibility research only."""
import hashlib,json,time,urllib.request,zipfile
from pathlib import Path
root=Path(__file__).resolve().parents[2];cache=root/'Library/EchoStation/S3Cache';ev=root/'Docs/Tests/Stage3/Evidence/S3-G01'
def fetch(url,path):
 path.parent.mkdir(parents=True,exist_ok=True)
 if not path.exists():
  part=path.with_suffix(path.suffix+'.part');size=0;last=time.monotonic()
  with urllib.request.urlopen(url,timeout=45) as src,part.open('wb') as dst:
   while block:=src.read(1048576):
    dst.write(block);size+=len(block)
    if time.monotonic()-last>20:print(path.name,round(size/1048576,1),'MiB',flush=True);last=time.monotonic()
  part.replace(path)
 with path.open('rb') as f:digest=hashlib.file_digest(f,'sha256').hexdigest()
 return {'url':url,'path':path.relative_to(root).as_posix(),'bytes':path.stat().st_size,'sha256':digest}
items=[]
for name in ['microsoft.ml.onnxruntime','microsoft.ml.onnxruntime.managed']:
 version='1.31.0';path=cache/'nuget'/f'{name}.{version}.nupkg';items.append(fetch(f'https://api.nuget.org/v3-flatcontainer/{name}/{version}/{name}.{version}.nupkg',path))
 with zipfile.ZipFile(path) as z:
  files=z.namelist();(ev/(name+'-Files.json')).write_text(json.dumps(files,indent=2),encoding='utf-8')
  for f in files:
   if f.endswith('.nuspec'):(ev/(name+'.nuspec')).write_bytes(z.read(f))
 print('NuGet ready',name,flush=True)
(ev/'NugetDownloads.json').write_text(json.dumps(items,indent=2),encoding='utf-8')
revision=json.loads((ev/'QwenOriginal.json').read_text(encoding='utf-8'))['sha']
record=fetch(f'https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct/resolve/{revision}/model.safetensors',cache/'models/original/model.safetensors')
(ev/'OriginalWeightDownload.json').write_text(json.dumps(record,indent=2),encoding='utf-8');print('Original reference ready',record['bytes'],flush=True)
