"""Editor-only source/config audit; explicitly do not rehash old distribution payloads."""
import argparse,hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];folder=ROOT/'Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/L01';parser=argparse.ArgumentParser();parser.add_argument('--label',default='EditorProtection_After');args=parser.parse_args();assert args.label.replace('_','').isalnum();target=folder/(args.label+'.json');baseline_path=folder/'Protection_Before.json'
if target.exists():raise SystemExit('Fresh evidence only')
before=json.loads(baseline_path.read_text(encoding='utf-8'))['files'];cutoff=baseline_path.stat().st_mtime;changed=[];metadata=[];audited=0
for name,expected in before.items():
 p=ROOT/name
 if not p.is_file():changed.append(name);continue
 if name.startswith('Builds/') or p.suffix.lower() in ['.onnx','.onnx_data']:
  metadata.append({'path':name,'bytes':p.stat().st_size,'lastWriteBeforeFrozenBaseline':p.stat().st_mtime<=cutoff})
  if p.stat().st_mtime>cutoff:changed.append(name)
  continue
 h=hashlib.sha256()
 with p.open('rb') as stream:
  while b:=stream.read(4*1024*1024):h.update(b)
 audited+=1
 if h.hexdigest()!=expected:changed.append(name)
allowed=['Assets/EchoStation/SalvageExpansion/Runtime/ExpansionEcho.cs','Assets/EchoStation/Player/Runtime/GameTextInput.cs'];unexpected=[p for p in changed if p not in allowed]
r={'passed':not unexpected,'shaAuditedSourceConfigFiles':audited,'changed':changed,'allowed':allowed,'unexpected':unexpected,'oldBinaryMetadata':metadata,'binaryAuditLimit':'No package build in this editor-only task; old build/model files checked for existence and writes since frozen hash baseline, not rehashed. Do not present this as a fresh binary SHA audit.'};target.write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in r.items() if k not in ['oldBinaryMetadata','binaryAuditLimit']},ensure_ascii=False));assert r['passed']
