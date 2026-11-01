"""Read official package/model metadata, preserving source evidence before downloads."""
import json,urllib.request
from pathlib import Path
root=Path(__file__).resolve().parents[2];folder=root/'Docs/Tests/Stage3/Evidence/S3-G01';folder.mkdir(parents=True,exist_ok=True)
sources={
 'BarracudaRegistry':'https://packages.unity.com/com.unity.barracuda',
 'QwenOriginal':'https://huggingface.co/api/models/Qwen/Qwen2.5-0.5B-Instruct',
 'QwenOnnx':'https://huggingface.co/api/models/onnx-community/Qwen2.5-0.5B-Instruct',
 'OrtNuget':'https://api.nuget.org/v3-flatcontainer/microsoft.ml.onnxruntime/index.json',
}
for name,url in sources.items():
 target=folder/(name+'.json')
 if target.exists():data=json.loads(target.read_text(encoding='utf-8'))
 else:
  try:
   req=urllib.request.Request(url,headers={'User-Agent':'EchoStation-S3-Compatibility-Research/1.0'})
   with urllib.request.urlopen(req,timeout=35) as response:raw=response.read()
   data=json.loads(raw);target.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
  except Exception as e:
   (folder/(name+'-Error.json')).write_text(json.dumps({'url':url,'error':str(e)},indent=2),encoding='utf-8');print(name,type(e).__name__,str(e));continue
 if name=='BarracudaRegistry':print(name,json.dumps({'latest':data.get('dist-tags'), '3.0.2':data.get('versions',{}).get('3.0.2',{})},ensure_ascii=False)[:3500])
 elif 'Qwen' in name:print(name,json.dumps({'sha':data.get('sha'),'library':data.get('library_name'),'card':data.get('cardData',{}),'files':[x['rfilename'] for x in data.get('siblings',[])]},ensure_ascii=False)[:6000])
 else:print(name,data.get('versions',[])[-12:])
