"""Copy the reviewed staged release only after clean native proof the old importer is unloaded."""
import datetime,hashlib,json,shutil,sys
from pathlib import Path
root=Path(__file__).resolve().parents[2];sys.path.insert(0,str(root/'Tools/MCP'))
from preflight import payload
ev=root/'Docs/Tests/Stage3/Evidence/S3-A01';preflight=payload(json.loads((ev/'InstallPreflight.json').read_text()))
assert preflight['success'];state=preflight['data']['result'];assert state['clean'] and state['idle'] and not state['barracudaLoaded']
stamp=datetime.datetime.fromisoformat(state['utc'].replace('Z','+00:00'));assert (datetime.datetime.now(datetime.timezone.utc)-stamp).total_seconds()<90
assert 'com.unity.barracuda' not in json.loads((root/'Packages/manifest.json').read_text())['dependencies']
source=root/'Library/EchoStation/S3Staging/Qwen25_05B_W8A8';target=root/'Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8';assert target.resolve().is_relative_to(root/'Assets/StreamingAssets/EchoLLM')
target.mkdir(parents=True,exist_ok=True);records=[]
for file in source.iterdir():
 if not file.is_file():raise RuntimeError('Unexpected staged directory')
 destination=target/file.name
 with file.open('rb') as stream:expected=hashlib.file_digest(stream,'sha256').hexdigest()
 if destination.exists():
  with destination.open('rb') as stream:assert hashlib.file_digest(stream,'sha256').hexdigest()==expected,'Existing different package preserved'
 else:shutil.copyfile(file,destination)
 records.append({'name':file.name,'bytes':destination.stat().st_size,'sha256':expected})
(ev/'InstalledRelease.json').write_text(json.dumps({'target':target.relative_to(root).as_posix(),'files':records,'totalBytes':sum(f['bytes'] for f in records)},indent=2),encoding='utf-8');print('Installed',len(records),'local files;',sum(f['bytes'] for f in records),'bytes')
