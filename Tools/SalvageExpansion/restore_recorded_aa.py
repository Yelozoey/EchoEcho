"""One narrowly checked recovery for the recorded Run01 URP MSAA synchronization."""
import base64,hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];folder=ROOT/'Docs/Tests/SalvagePrototype/Evidence/Expansion/Run01';target=folder/'AaDiskRepair_A.json'
if target.exists():raise SystemExit('Preserve existing recovery receipt')
baseline=json.loads((folder/'NativeBefore_A.json').read_text(encoding='utf-8'))
row=baseline['ProjectSettings/QualitySettings.asset'];native=json.loads(row['json'])['QualitySettings']['m_QualitySettings'][1]
if native['name']!='PC' or native['antiAliasing']!=4:raise SystemExit('Unexpected original quality state')
original=base64.b64decode(row['disk']);old_guid=native['customRenderPipeline']['guid'].encode('ascii');meta=(ROOT/'Assets/EchoStation/Rendering/Settings/ES_Gameplay_ForwardHDR.asset.meta').read_text();temporary=re.search(r'^guid: ([a-f0-9]{32})$',meta,re.M).group(1).encode('ascii')
if original.count(old_guid)!=1:raise SystemExit('Expected one original PC binding')
permitted=original.replace(old_guid,temporary);match=re.search(rb'(?m)^    name: PC\r?$',permitted)
if not match:raise SystemExit('Exact PC YAML entry missing')
end=permitted.find(b'\n  - serializedVersion:',match.end());end=len(permitted) if end<0 else end
block=permitted[match.start():end]
if block.count(b'    antiAliasing: 4')!=1:raise SystemExit('Expected one recorded PC AA field')
allowed=permitted[:match.start()]+block.replace(b'    antiAliasing: 4',b'    antiAliasing: 0',1)+permitted[end:]
path=ROOT/'ProjectSettings/QualitySettings.asset';current=path.read_bytes()
if current!=allowed:raise SystemExit('Other disk changes; preserving user state')
path.write_bytes(original)
record={'exactOnlyRecordedAaAndTemporaryPipelineChanges':True,'beforeSha256':hashlib.sha256(current).hexdigest(),'restoredSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'originalSha256':hashlib.sha256(original).hexdigest(),'exact':path.read_bytes()==original}
target.write_text(json.dumps(record,indent=2),encoding='utf-8');print(json.dumps(record))
