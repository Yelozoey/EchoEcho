import argparse,hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[2];folder=root/'Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/L02';folder.mkdir(parents=True,exist_ok=True)
p=argparse.ArgumentParser();p.add_argument('action',choices=['capture','verify']);a=p.parse_args();dest=folder/('Protection_Before.json' if a.action=='capture' else 'Protection_After.json');assert not dest.exists()
def collect():
    source={};binary={}
    for base in ['Assets','Packages','ProjectSettings','Builds']:
        for f in (root/base).rglob('*'):
            if not f.is_file():continue
            name=f.relative_to(root).as_posix()
            if base=='Builds' or f.suffix.lower() in ['.onnx','.onnx_data']:
                s=f.stat();binary[name]=[s.st_size,s.st_mtime_ns]
            elif f.suffix.lower() in ['.cs','.meta','.asset','.unity','.prefab','.asmdef','.shader','.hlsl','.json']:
                source[name]=hashlib.sha256(f.read_bytes()).hexdigest()
    return {'source':source,'binaryMetadata':binary}
r=collect()
if a.action=='verify':
    before=json.loads((folder/'Protection_Before.json').read_text(encoding='utf-8'));r['changedSource']=[n for n,h in before['source'].items() if r['source'].get(n)!=h];r['changedBinary']=[n for n,h in before['binaryMetadata'].items() if r['binaryMetadata'].get(n)!=h];r['allowed']=['Assets/EchoStation/LLMShortTasks/L01/Runtime/ExpansionDialoguePanel.cs','Assets/EchoStation/Player/Runtime/GameTextInput.cs','Assets/EchoStation/LLMShortTasks/Tests/ImeLifecycleTests.cs'];r['unexpected']=[n for n in r['changedSource'] if n not in r['allowed']];r['passed']=not r['unexpected'] and not r['changedBinary']
dest.write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in r.items() if k not in ['source','binaryMetadata']},ensure_ascii=False));assert r.get('passed',True)
