"""Finalize only the same-package Native_C evidence after the human's three checks."""
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];evidence=ROOT/'Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/IME01';target=evidence/'Native_C_Acceptance.json'
if target.exists():raise SystemExit('Preserve acceptance evidence')
native=json.loads((evidence/'Native_C/NativeEvents.json').read_text(encoding='utf-8-sig'));process=json.loads((evidence/'Native_C/Process.json').read_text(encoding='utf-8-sig'));assert process['exitCode']==0 and not (evidence/'Native_C/Timeout.json').exists()
events=native['events'];states=[e['data'] for e in events if e['kind']=='state'];compositions=sum(e['kind']=='composition' for e in events);cjk=sum(e['kind']=='text' and 0x4e00<=e['data']['codepoint']<=0x9fff for e in events)
valid=sum(s['focused'] and s['nativeBridge'] and s['context']!=0 and s['nativeMode']=='On' for s in states);restored=sum(not s['focused'] and not s['nativeBridge'] and s['nativeMode']=='Auto' for s in states)
assert compositions>0 and cjk>0 and valid>0 and restored>0
manifest=json.loads((evidence/'DeliveryManifest_Game_B.json').read_text(encoding='utf-8'));release=ROOT/'Builds/LLMShortTasks/IME01/Game_B';changes=[]
for name,entry in manifest['files'].items():
 p=release/name
 if not p.is_file() or p.stat().st_size!=entry['bytes'] or hashlib.sha256(p.read_bytes()).hexdigest()!=entry['sha256']:changes.append(name)
assert not changes and len(list(p for p in release.rglob('*') if p.is_file()))==manifest['fileCount']
assert hashlib.sha256((ROOT/'Assets/EchoStation/Player/Runtime/GameTextInput.cs').read_bytes()).hexdigest()==manifest['sourceSha256']
r={'passed':True,'recordedUtc':datetime.now(timezone.utc).isoformat(),'release':'IME01_Game_B','sameSealedPackage':True,'releaseFiles':manifest['fileCount'],'process':process,'nativeCompositionEvents':compositions,'nativeCjkCommits':cjk,'submitEvents':sum(e['kind']=='submit' for e in events),'focusedValidNativeStates':valid,'restoredNativeAutoStates':restored,'humanAcceptance':{'question':'输入跟随我；确认候选Enter不误发送、再次Enter只发送一次；Esc关闭后重开T再输入中文','answer':'三项都正常','source':'Direct user async reply in this chat, call_b18043051dff424a8398dc1d119dc51e'},'limits':['Microsoft Pinyin on the current Windows 11 machine only','No claim for other IMEs, Windows 10, target-hardware performance or LLM readiness']}
target.write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in r.items() if k not in ['humanAcceptance','limits']},ensure_ascii=False))
