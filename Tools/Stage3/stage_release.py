"""Stage fixed, licensed local model assets outside AssetDatabase before safe installation."""
import hashlib,json,shutil,urllib.request
from pathlib import Path
root=Path(__file__).resolve().parents[2];cache=root/'Library/EchoStation/S3Cache/models';target=root/'Library/EchoStation/S3Staging/Qwen25_05B_W8A8';target.mkdir(parents=True,exist_ok=True)
ev=root/'Docs/Tests/Stage3/Evidence/S3-A01';ev.mkdir(parents=True,exist_ok=True)
original_revision='7ae557604adf67be50417f59c2c2f167def9a775';converted_revision='22942cb7d7ba4cc81bb4673549ca4d4614469b5e';release_id='echo-qwen25-05b-w8a8-b32-last-v1'
card=cache/'original/README.md'
if not card.exists():
 with urllib.request.urlopen(f'https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct/resolve/{original_revision}/README.md',timeout=30) as response:card.write_bytes(response.read())
sources={'model.onnx':cache/'qwen-int4/onnx/model_weight8_last.onnx','tokenizer.json':cache/'original/tokenizer.json','tokenizer_config.json':cache/'original/tokenizer_config.json','config.json':cache/'original/config.json','generation_config.json':cache/'original/generation_config.json','LICENSE.txt':cache/'original/LICENSE','MODEL_CARD.md':card}
for name,source in sources.items():
 path=target/name
 if path.exists():
  with path.open('rb') as f:existing=hashlib.file_digest(f,'sha256').hexdigest()
  with source.open('rb') as f:expected=hashlib.file_digest(f,'sha256').hexdigest()
  if existing!=expected:raise RuntimeError('Preserve mismatching staged '+name)
 else:shutil.copyfile(source,path)
notice=f'''EchoStation local model derivative, 2026-10-26.
Original: Qwen/Qwen2.5-0.5B-Instruct, revision {original_revision}.
Original license: Apache-2.0; see LICENSE.txt and original MODEL_CARD.md.
ONNX conversion source: onnx-community/Qwen2.5-0.5B-Instruct, revision {converted_revision}.
The community conversion is not claimed to be a Qwen-produced ONNX release.
EchoStation modifications: ONNX Runtime1.31.0 DEFAULT symmetric weight-only INT8 block32 on168 constant MatMul nodes; accuracy_level4; Gather final hidden position before lm_head. Embedding/shared output head and other unquantized weights remain floating point. Original weights retained by these transformations except quantized main matrices.
No model training, cloud inference, remote code or runtime downloader is included.
The runtime uses a2048-token project context limit despite a larger original model limit.
ORT native/managed runtime MIT and third-party notices are distributed separately under EchoStation/AI/ThirdParty/OnnxRuntime/Licenses.
'''
(target/'NOTICE.md').write_text(notice,encoding='utf-8')
files=[]
for p in sorted(target.iterdir()):
 if not p.is_file() or p.name=='manifest.json':continue
 with p.open('rb') as f:digest=hashlib.file_digest(f,'sha256').hexdigest()
 files.append({'name':p.name,'bytes':p.stat().st_size,'sha256':digest})
assert next(f['sha256'] for f in files if f['name']=='model.onnx')=='6d04df6470b91cc7a977be0d8eae87e27f3400ef654940ed85bd80e315411055'
assert sum(f['bytes'] for f in files)<1024**3
manifest={'version':1,'releaseId':release_id,'files':files};(target/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(ev/'StagedRelease.json').write_text(json.dumps({'path':str(target.relative_to(root)),'manifest':manifest,'sumBytes':sum(f['bytes'] for f in files)},indent=2),encoding='utf-8')
lines=['// Generated from pinned local artifacts by Tools/Stage3/stage_release.py.','namespace EchoStation.AI.Models','{',' public static class ModelCatalog',' {',f'  public const string DirectoryName="Qwen25_05B_W8A8";','  public static ModelRelease Current {get;}=new ModelRelease("'+release_id+'",new ModelFileSpec[]{']
lines.extend('   new ModelFileSpec('+json.dumps(f['name'])+','+str(f['bytes'])+'L,'+json.dumps(f['sha256'])+'),' for f in files)
lines.extend(['  });',' }','}'])
dest=root/'Assets/EchoStation/AI/Models/ModelCatalog.generated.cs';dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(json.dumps({'stagedFiles':len(files),'sumBytes':sum(f['bytes'] for f in files),'releaseId':release_id},indent=2))
