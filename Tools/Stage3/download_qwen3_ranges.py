"""Four bounded official HTTP ranges with prefix reuse and whole-file LFS validation."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import hashlib,json,re,time,urllib.request
root=Path(__file__).resolve().parents[2];ev=root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3';cache=root/'Library/EchoStation/S3Cache/models/qwen3-candidate/onnx/onnx'
meta=json.loads((ev/'onnx_metadata.json').read_text(encoding='utf-8'));item=next(x for x in meta['siblings'] if x['rfilename']=='onnx/model_q4.onnx');size=item['size'];digest=item['lfs']['sha256'];revision=meta['sha'];assert re.fullmatch('[a-f0-9]{40}',revision)
url=f'https://huggingface.co/onnx-community/Qwen3-0.6B-ONNX/resolve/{revision}/onnx/model_q4.onnx';target=cache/'model_q4.onnx';part=cache/'model_q4.onnx.part';pieces=cache/'model_q4.ranges';pieces.mkdir(parents=True,exist_ok=True);chunk=64*1024**2;count=(size+chunk-1)//chunk
assert not target.exists(),'Final file already present; do not redownload'
budget=json.loads((root/'Docs/Tests/Stage3/Evidence/S3-I03/ResourceBudget.json').read_text(encoding='utf-8'));current=sum(p.stat().st_size for x in budget['parts'] for p in (root/x['path']).rglob('*') if p.is_file());assert current+2*size<20*1024**3,'Range staging would exceed authorized budget'
if part.exists():
 with part.open('rb') as original:
  for i in range(count):
   prefix=original.read(min(chunk,size-i*chunk))
   if not prefix:break
   piece=pieces/f'{i:03}.part'
   if not piece.exists() or piece.stat().st_size<len(prefix):piece.write_bytes(prefix)
def fetch(index):
 piece=pieces/f'{index:03}.part';base=index*chunk;length=min(chunk,size-base);last=time.monotonic()
 for attempt in range(3):
  have=piece.stat().st_size if piece.exists() else 0;assert have<=length
  if have==length:return index
  start=base+have;end=base+length-1
  try:
   req=urllib.request.Request(url,headers={'Range':f'bytes={start}-{end}'})
   with urllib.request.urlopen(req,timeout=45) as response:
    assert response.status==206 and response.headers.get('Content-Range')==f'bytes {start}-{end}/{size}','Exact HTTP range not honored'
    assert int(response.headers['Content-Length'])==end-start+1,'Range content length differs'
    with piece.open('ab') as output:
     while block:=response.read(min(1048576,length-have)):
      output.write(block);have+=len(block);assert have<=length
      if time.monotonic()-last>20:print('range',index,round(have/1048576,1),'MiB',flush=True);last=time.monotonic()
      if have==length:break
   assert have==length,'Truncated range'
   print('complete range',index,flush=True);return index
  except Exception as error:
   (ev/f'RangeError_{time.time_ns()}.json').write_text(json.dumps({'index':index,'attempt':attempt,'start':start,'end':end,'error':str(error)},indent=2),encoding='utf-8')
   if attempt==2:raise
   print('retry range',index,type(error).__name__,flush=True)
with ThreadPoolExecutor(max_workers=4) as pool:done=list(pool.map(fetch,range(count)))
assert len(done)==count
with part.open('wb') as output:
 for i in range(count):
  piece=pieces/f'{i:03}.part';assert piece.stat().st_size==min(chunk,size-i*chunk)
  with piece.open('rb') as source:
   while block:=source.read(1048576):output.write(block)
with part.open('rb') as source:actual=hashlib.file_digest(source,'sha256').hexdigest()
assert part.stat().st_size==size and actual==digest,'Complete size or LFS SHA differs'
part.replace(target);record={'repo':meta['id'],'revision':revision,'url':url,'path':target.relative_to(root).as_posix(),'bytes':size,'sha256':actual,'ranges':count,'workers':4,'prefixReused':True,'budgetProjectedMax':current+2*size,'verified':True}
(ev/'Downloads_ranges.json').write_text(json.dumps(record,indent=2),encoding='utf-8');print(json.dumps(record),flush=True)
