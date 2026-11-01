"""Install only approved official CPU runtime files; never copy unrelated architectures."""
import hashlib,json,zipfile
from pathlib import Path
root=Path(__file__).resolve().parents[2];cache=root/'Library/EchoStation/S3Cache/nuget';ev=root/'Docs/Tests/Stage3/Evidence/S3-G01';target=root/'Assets/EchoStation/AI/ThirdParty/OnnxRuntime'
if not (root/'Docs/Stage3/BarracudaNoGo.md').exists():raise SystemExit('Barracuda failure evidence required before backend switch')
items=[]
for name,files in [
 ('microsoft.ml.onnxruntime.managed',{'lib/netstandard2.0/Microsoft.ML.OnnxRuntime.dll':'Managed/Microsoft.ML.OnnxRuntime.dll','LICENSE.txt':'Licenses/Managed-LICENSE.txt','ThirdPartyNotices.txt':'Licenses/Managed-ThirdPartyNotices.txt'}),
 ('microsoft.ml.onnxruntime',{'runtimes/win-x64/native/onnxruntime.dll':'Plugins/x86_64/onnxruntime.dll','runtimes/win-x64/native/onnxruntime_providers_shared.dll':'Plugins/x86_64/onnxruntime_providers_shared.dll','LICENSE':'Licenses/Native-LICENSE.txt','ThirdPartyNotices.txt':'Licenses/Native-ThirdPartyNotices.txt'})]:
 with zipfile.ZipFile(cache/f'{name}.1.31.0.nupkg') as z:
  for source,dest in files.items():
   if source not in z.namelist():raise RuntimeError('Missing official artifact '+source)
   data=z.read(source);path=target/dest;path.parent.mkdir(parents=True,exist_ok=True)
   if path.exists() and path.read_bytes()!=data:raise RuntimeError('Preserve mismatching existing '+str(path))
   path.write_bytes(data);items.append({'package':name,'version':'1.31.0','source':source,'target':path.relative_to(root).as_posix(),'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()})
(ev/'OrtInstalledFiles.json').write_text(json.dumps(items,indent=2),encoding='utf-8');print(json.dumps(items,indent=2))
