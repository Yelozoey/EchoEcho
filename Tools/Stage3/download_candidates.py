"""G01 second quantization candidate plus unquantized conversion reference (never shipped)."""
import concurrent.futures,hashlib,json,time,urllib.request
from pathlib import Path
root=Path(__file__).resolve().parents[2];ev=root/'Docs/Tests/Stage3/Evidence/S3-G01';cache=root/'Library/EchoStation/S3Cache/models/qwen-int4/onnx';revision=json.loads((ev/'QwenOnnx.json').read_text(encoding='utf-8'))['sha']
def fetch(name):
 path=cache/name;url=f'https://huggingface.co/onnx-community/Qwen2.5-0.5B-Instruct/resolve/{revision}/onnx/{name}'
 if not path.exists():
  part=path.with_suffix('.onnx.part')
  for attempt in range(3):
   size=part.stat().st_size if part.exists() else 0;last=time.monotonic()
   try:
    req=urllib.request.Request(url,headers={'Range':f'bytes={size}-'} if size else {})
    with urllib.request.urlopen(req,timeout=45) as src:
     if size and src.status==206:
      if not src.headers.get('Content-Range','').startswith(f'bytes {size}-'):raise RuntimeError('Mismatched resume range')
      mode='ab'
     else:size=0;mode='wb'
     expected=int(src.headers['Content-Length'])+size if src.headers.get('Content-Length') else None
     with part.open(mode) as dst:
      while data:=src.read(1048576):
       dst.write(data);size+=len(data)
       if time.monotonic()-last>20:print(name,round(size/1048576,1),'MiB',flush=True);last=time.monotonic()
     if expected is not None and size!=expected:raise RuntimeError('Truncated response')
    part.replace(path);break
   except Exception as error:
    (ev/f'{name}-ResumeError-{attempt}-{time.time_ns()}.json').write_text(json.dumps({'error':str(error),'size':size,'attempt':attempt}),encoding='utf-8')
    if attempt==2:raise
    print(name,'retry at',size,type(error).__name__,flush=True)
 with path.open('rb') as f:digest=hashlib.file_digest(f,'sha256').hexdigest()
 return {'url':url,'file':str(path.relative_to(root)),'bytes':path.stat().st_size,'sha256':digest}
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
 records=list(pool.map(fetch,['model_int8.onnx','model.onnx']))
(ev/'SecondCandidateDownloads.json').write_text(json.dumps(records,indent=2),encoding='utf-8');print('Reference and INT8 ready',flush=True)
