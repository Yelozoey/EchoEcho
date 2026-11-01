"""Pinned candidate research only; no production changes or executable remote code."""
from pathlib import Path
import argparse,hashlib,json,time,urllib.request
root=Path(__file__).resolve().parents[2]
ev=root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3';ev.mkdir(parents=True,exist_ok=True)
cache=root/'Library/EchoStation/S3Cache/models/qwen3-candidate'
parser=argparse.ArgumentParser();parser.add_argument('--weights',action='store_true');parser.add_argument('--quant-only',action='store_true');args=parser.parse_args();assert not args.quant_only or args.weights
records=[]
def get_json(url):
 with urllib.request.urlopen(url,timeout=45) as response:return json.load(response)
def fetch(repo,revision,name,sub,metadata):
 path=cache/sub/name;path.parent.mkdir(parents=True,exist_ok=True)
 item=next(x for x in metadata['siblings'] if x['rfilename']==name)
 expected=item.get('size') or (item.get('lfs') or {}).get('size');expected_sha=(item.get('lfs') or {}).get('sha256')
 url=f'https://huggingface.co/{repo}/resolve/{revision}/{name}'
 if not path.exists():
  part=path.with_name(path.name+'.part')
  for attempt in range(3):
   size=part.stat().st_size if part.exists() else 0;last=time.monotonic()
   try:
    req=urllib.request.Request(url,headers={'Range':f'bytes={size}-'} if size else {})
    with urllib.request.urlopen(req,timeout=45) as response:
     if size and response.status==206:
      assert response.headers.get('Content-Range','').startswith(f'bytes {size}-');mode='ab'
     else:size=0;mode='wb'
     length=int(response.headers['Content-Length'])+size if response.headers.get('Content-Length') else expected
     with part.open(mode) as target:
      while block:=response.read(1048576):
       target.write(block);size+=len(block)
       if time.monotonic()-last>20:print(name,round(size/1048576,1),'MiB',flush=True);last=time.monotonic()
     assert length is None or size==length,'Truncated HTTP response'
    assert expected is None or size==expected,'File size differs from pinned metadata'
    with part.open('rb') as stream:digest=hashlib.file_digest(stream,'sha256').hexdigest()
    assert not expected_sha or digest==expected_sha,'Pinned LFS SHA mismatch'
    part.replace(path);break
   except Exception as error:
    (ev/f'DownloadError_{time.time_ns()}.json').write_text(json.dumps({'url':url,'bytes':size,'attempt':attempt,'error':str(error)},indent=2),encoding='utf-8')
    if attempt==2:raise
    print(name,'resume',size,type(error).__name__,flush=True)
 with path.open('rb') as stream:digest=hashlib.file_digest(stream,'sha256').hexdigest()
 assert expected is None or path.stat().st_size==expected
 assert not expected_sha or digest==expected_sha
 record={'repo':repo,'revision':revision,'url':url,'path':path.relative_to(root).as_posix(),'bytes':path.stat().st_size,'sha256':digest};records.append(record)
 (ev/f'Downloads_{"weights" if args.weights else "small"}.json').write_text(json.dumps(records,indent=2),encoding='utf-8');print('ready',sub,name,record['bytes'],flush=True)
sources=[('Qwen/Qwen3-0.6B','original',['LICENSE','README.md','config.json','tokenizer.json','tokenizer_config.json','generation_config.json']),('onnx-community/Qwen3-0.6B-ONNX','onnx',['README.md','config.json'])]
for repo,sub,files in sources:
 if args.quant_only and sub=='original':continue
 meta_path=ev/f'{sub}_metadata.json'
 if not meta_path.exists():
  metadata=get_json(f'https://huggingface.co/api/models/{repo}?blobs=true');assert not metadata.get('gated') and not metadata['private'];meta_path.write_text(json.dumps(metadata,indent=2),encoding='utf-8')
 metadata=json.loads(meta_path.read_text(encoding='utf-8'));revision=metadata['sha'];assert len(revision)==40
 selected=['model.safetensors'] if args.weights and sub=='original' else ['onnx/model_q4.onnx'] if args.weights else files
 if args.weights:
  budget=json.loads((root/'Docs/Tests/Stage3/Evidence/S3-I03/ResourceBudget.json').read_text(encoding='utf-8'));parts=[root/x['path'] for x in budget['parts']];current=sum(p.stat().st_size for base in parts if base.exists() for p in base.rglob('*') if p.is_file());needed=sum(next(x for x in metadata['siblings'] if x['rfilename']==f)['size'] for f in selected if not (cache/sub/f).exists());assert current+needed<20*1024**3,'Candidate download would exceed authorized resource budget'
 for name in selected:fetch(repo,revision,name,sub,metadata)
