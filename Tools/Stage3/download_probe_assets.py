"""Download pinned official-hosted evidence/model assets; never import executable remote code."""
import hashlib,json,tarfile,time,urllib.request
from pathlib import Path
root=Path(__file__).resolve().parents[2];evidence=root/'Docs/Tests/Stage3/Evidence/S3-G01';cache=root/'Library/EchoStation/S3Cache';cache.mkdir(parents=True,exist_ok=True)
def fetch(url,path):
 path.parent.mkdir(parents=True,exist_ok=True)
 if not path.exists():
  partial=path.with_name(path.name+'.part');total=0;started=time.monotonic();last=started
  try:
   req=urllib.request.Request(url,headers={'User-Agent':'EchoStation-S3-Compatibility/1.0'})
   with urllib.request.urlopen(req,timeout=45) as response,partial.open('wb') as target:
    while True:
     block=response.read(1048576)
     if not block:break
     target.write(block);total+=len(block)
     if time.monotonic()-last>20:print(path.name,round(total/1048576,1),'MiB',flush=True);last=time.monotonic()
   partial.replace(path)
  except Exception as error:
   (evidence/(path.name+'-DownloadError.json')).write_text(json.dumps({'url':url,'error':str(error),'bytes':total},indent=2),encoding='utf-8');raise
 h=hashlib.sha256()
 with path.open('rb') as stream:
  for block in iter(lambda:stream.read(1048576),b''):h.update(block)
 return {'url':url,'path':path.relative_to(root).as_posix(),'bytes':path.stat().st_size,'sha256':h.hexdigest()}
manifest=[]
registry=json.loads((evidence/'BarracudaRegistry.json').read_text(encoding='utf-8'));metadata=registry['versions']['3.0.2'];archive=cache/'barracuda-3.0.2.tgz';manifest.append(fetch(metadata['dist']['tarball'],archive));assert hashlib.sha1(archive.read_bytes()).hexdigest()==metadata['dist']['shasum']
with tarfile.open(archive,'r:gz') as tar:
 for member in tar.getmembers():
  if member.name.endswith(('ONNXModelConverter.cs','SupportedOperators.md','package.json')) and member.isfile():
   text=tar.extractfile(member).read();target=evidence/('Barracuda302-'+Path(member.name).name);target.write_bytes(text)
print('Barracuda verified source ready',flush=True)
original=json.loads((evidence/'QwenOriginal.json').read_text(encoding='utf-8'));converted=json.loads((evidence/'QwenOnnx.json').read_text(encoding='utf-8'))
for repo,revision,sub,files in [
 ('Qwen/Qwen2.5-0.5B-Instruct',original['sha'],'original',['LICENSE','config.json','tokenizer.json','tokenizer_config.json','generation_config.json']),
 ('onnx-community/Qwen2.5-0.5B-Instruct',converted['sha'],'qwen-int4',['README.md','config.json','quantize_config.json','tokenizer.json','tokenizer_config.json','onnx/model_q4.onnx']),
]:
 for name in files:
  record=fetch('https://huggingface.co/'+repo+'/resolve/'+revision+'/'+name,cache/'models'/sub/name);record.update(repo=repo,revision=revision);manifest.append(record);print('ready',sub,name,record['bytes'],flush=True)
(evidence/'DownloadedAssets.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8');print('Complete',len(manifest),'files',flush=True)
