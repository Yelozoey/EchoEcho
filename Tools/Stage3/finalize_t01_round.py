"""Bind actual default regressions, workspace restoration and current resource audit."""
import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[2];folder=root/'Docs/Tests/Stage3/Evidence/S3-T01r0'
def read(path):return json.loads(path.read_text(encoding='utf-8'))
def latest(directory,stem):
 files=list((root/directory).glob(stem+'_[0-9]*.json'))
 path=max(files,key=lambda p:int(p.stem.rsplit('_',1)[1]))
 assert path.stat().st_mtime>=(folder/'Default_A-Job.json').stat().st_mtime,'Old raw report is not this regression'
 return path,read(path)
six_path,six=latest('Docs/Tests/Stage3/Evidence/S3-E03c3','LiveSix')
save_path,save=latest('Docs/Tests/Stage3/Evidence/S3-S01d','NativeSave')
assert six['completed'] and six['ownerCleanupComplete'] and six['jsonCorrect']==six['semanticCorrect']==six['accepted']==6
assert save['completed'] and save['ownerCleanupComplete'] and save['delivered']==2
protection=root/'Docs/Tests/Stage3/Evidence/ProtectedS1S2-Verified.json';protected=read(protection)
assert protected==dict(checked=767,unchanged=True,changed=[])
copy=folder/'Protection_Final_A.json'
with copy.open('xb') as stream:stream.write(protection.read_bytes())
resource=read(folder/'Resource_Final_A.json');parts=resource['parts'];paths=[(root/p['path']).resolve() for p in parts]
assert len(paths)==len(set(paths)) and not any(a in b.parents for a in paths for b in paths if a!=b)
assert resource['underLimit'] and resource['totalBytes']==sum(p['bytes'] for p in parts)
assert any(p['path']=='Assets/EchoStation/Tests/AIExecutionEditor' and p['bytes']>0 for p in parts)
assert any(p['path']=='Builds/S3' for p in parts)
for actual in (root/'Builds/Tests').glob('S3*'):assert any(p==actual.resolve() or p in actual.resolve().parents for p in paths)
restored=read(folder/'Workspace_A-WorkspaceRestored.json')['structuredContent']['data']['result']
assert restored['scenes']==[dict(path='Assets/EchoStation/Station/Scenes/ES_Station_Greybox.unity',loaded=True,active=True)]
assert restored['defaultPipelinePath']==restored['qualityPipelinePath']=='Assets/EchoStation/Station/Generated/ES_S2_ForwardHDR.asset'
record=dict(scope='default v5/greedy real six intents and S3 save after experiments; not T01 quality pass',s3CompletedModules=17,t01Passed=False,defaultSixRaw=str(six_path.relative_to(root)),defaultSix=dict(count=six['count'],json=six['jsonCorrect'],semantic=six['semanticCorrect'],accepted=six['accepted'],cleanup=six['ownerCleanupComplete']),defaultSaveRaw=str(save_path.relative_to(root)),defaultSave=dict(delivered=save['delivered'],cleanup=save['ownerCleanupComplete']),protection=protected,workspaceRestored=restored,resourceBytes=resource['totalBytes'],resourceGiB=resource['totalBytes']/1024**3,resourceCoverageChecked=True,trainingStarted=False)
output=folder/'Final_Round_A.json'
with output.open('x',encoding='utf-8') as stream:stream.write(json.dumps(record,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(record,ensure_ascii=False))
