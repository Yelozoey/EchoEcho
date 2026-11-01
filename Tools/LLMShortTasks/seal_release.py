"""Seal the new IME patch only; prior deliveries remain read-only."""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];folder=ROOT/'Builds/LLMShortTasks/IME01/Game_B';evidence=ROOT/'Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/IME01';target=evidence/'DeliveryManifest_Game_B.json';info=folder/'BuildInfo.json'
if target.exists() or info.exists():raise SystemExit('Preserve sealed release')
build=json.loads((evidence/'Build_Game_B.json').read_text(encoding='utf-8-sig'));assert build['passed'] and build['errors']==0 and folder.is_relative_to(ROOT/'Builds/LLMShortTasks')
assert not [p for p in folder.rglob('*') if p.is_file() and p.suffix.lower() in ('.onnx','.onnx_data')] and not (folder/'Validation').exists()
info.write_text(json.dumps({'release':'IME01_Game_B','unity':'6000.6.7f1','api':'Direct3D11','base':'SalvageNext ArtV2','UseLLM':False,'change':'Windows native IME initialization with InputSystem text/composition events','automatedTests':17},ensure_ascii=False,indent=2),encoding='utf-8')
files={p.relative_to(folder).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(folder.rglob('*')) if p.is_file()}
r={'release':'IME01_Game_B','files':files,'fileCount':len(files),'bytes':sum(f['bytes'] for f in files.values()),'noModelPayload':True,'noDiagnosticsPayload':True,'sourceSha256':hashlib.sha256((ROOT/'Assets/EchoStation/Player/Runtime/GameTextInput.cs').read_bytes()).hexdigest()};target.write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in r.items() if k!='files'},ensure_ascii=False))
