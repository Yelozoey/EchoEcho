"""Stage only pinned, already cached Qwen3 files; no downloader or old-model processing."""
import hashlib
import json
import shutil
from pathlib import Path

root=Path(__file__).resolve().parents[2]
evidence=root/'Docs/Tests/Stage3/Evidence/S3-Q3-A01b'
target=root/'Library/EchoStation/S3Staging/Qwen3_06B_Q4'
catalog=root/'Assets/EchoStation/AI/Models/Qwen3Catalog.generated.cs'
candidate=root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3'
cache=root/'Library/EchoStation/S3Cache/models/qwen3-candidate'
release='echo-qwen3-06b-q4-last-v1'
original_revision='c1899de289a04d12100db370d81485cdf75e47ca'
export_revision='58ca03a5e7b354b480b1a82b34bde8dc7b854cc0'
model_sha='3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303'
source_sha='d43d836fc5e240df9013733ccd214972c5d21bd9ec47e574e4f1e359cf90aed0'

def digest(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()

def encoded(value):return (json.dumps(value,ensure_ascii=False,indent=2)+'\n').encode('utf-8')

assert not target.exists() and not catalog.exists(),'Preserve existing package/catalog; fresh staging required'
assert not (evidence/'StagedRelease.json').exists(),'Fresh evidence required'
pins={
 'LICENSE':(11343,'832dd9e00a68dd83b3c3fb9f5588dad7dcf337a0db50f7d9483f310cd292e92e'),
 'README.md':(13965,'1ab64a26fcb3b461423b89a433a8c858f1bf8d4086f979cbb3ff878d47cf20e9'),
 'config.json':(726,'660db3b73d788119c04535e48cf9be5f55bc3100841a718637ae695b442f27dd'),
 'tokenizer.json':(11422654,'aeb13307a71acd8fe81861d94ad54ab689df773318809eed3cbe794b4492dae4'),
 'tokenizer_config.json':(9732,'d5d09f07b48c3086c508b30d1c9114bd1189145b74e982a265350c923acd8101'),
 'generation_config.json':(239,'2325da0f15bb848e018c5ae071b7943332e9f871d6b60e2ed22ca97d4cb993d2')}
downloads=json.loads((candidate/'Downloads_small.json').read_text(encoding='utf-8'))
for name,(size,sha) in pins.items():
    recorded=[item for item in downloads if item['repo']=='Qwen/Qwen3-0.6B' and Path(item['path']).name==name]
    assert len(recorded)==1 and recorded[0]['revision']==original_revision and recorded[0]['bytes']==size and recorded[0]['sha256']==sha
transform=json.loads((candidate/'LastHeadTransformation.json').read_text(encoding='utf-8'))
equivalence=json.loads((candidate/'Equivalence_B.json').read_text(encoding='utf-8'))
assert transform['sha256']==model_sha and transform['sourceSha256']==source_sha and transform['weightsUnchanged']
assert equivalence['passed'] and equivalence['completed'] and equivalence['bindingsStable']
assert equivalence['bindingsStart']==equivalence['bindingsEnd']
assert equivalence['bindingsStart']['optimizedModelSha256']==model_sha
assert len(equivalence['comparisons'])==2 and all(item['logitsMaxAbs']<=.0001 and item['kvMaxAbs']==0 for item in equivalence['comparisons'])
sources={'model.onnx':(cache/'onnx/onnx/model_q4_last.onnx',919096845,model_sha)}
for name,(size,sha) in pins.items():
    output={'LICENSE':'LICENSE.txt','README.md':'MODEL_CARD.md'}.get(name,name)
    sources[output]=(cache/'original'/name,size,sha)

# Stat-only accounting before adding any new large resource.
parts=json.loads((root/'Docs/Tests/Stage3/Evidence/S3-I03/ResourceBudget.json').read_text(encoding='utf-8'))['parts']
paths=[item['path'] for item in parts]+['Assets/EchoStation/Tests/AIEditor']
current=sum(path.stat().st_size for relative in paths for path in (root/relative).rglob('*') if path.is_file())
planned=sum(size for _,size,_ in sources.values())+1024*1024
assert current+planned<=20*1024**3,'Resource budget would be exceeded'
for path,size,sha in sources.values():
    assert path.is_file() and path.stat().st_size==size and digest(path)==sha,'Pinned source differs: '+str(path)

target.mkdir(parents=True)
for name,(path,size,sha) in sources.items():
    destination=target/name
    with path.open('rb') as source,destination.open('xb') as output:shutil.copyfileobj(source,output,1024*1024)
    assert destination.stat().st_size==size and digest(destination)==sha,'Staged copy differs'

notice=f'''EchoStation Qwen3 local model package, 2026-10-27.
Upstream model: Qwen/Qwen3-0.6B, revision {original_revision}.
Upstream license: Apache-2.0, preserved in LICENSE.txt; original model card in MODEL_CARD.md.
Upstream source: https://huggingface.co/Qwen/Qwen3-0.6B/tree/{original_revision}
Community INT4 ONNX export: onnx-community/Qwen3-0.6B-ONNX, revision {export_revision}.
Export source: https://huggingface.co/onnx-community/Qwen3-0.6B-ONNX/tree/{export_revision}
This community ONNX export is not claimed to be an official Qwen-produced ONNX file.
EchoStation change: Gather the final hidden position before lm_head; all initializers remain unchanged relative to the cached upstream INT4 graph. No training or additional weight quantization was performed.
Original INT4 graph SHA256: {source_sha}
Packaged graph SHA256: {model_sha}
Prefill and one cached continuation numerical equivalence were measured against the original INT4 graph; this is not an FP32-model quality claim. Details are in PROVENANCE.json and project test records.
Runtime policy: local offline CPU inference, non-thinking greedy, project context2048/input1536/generated256. No runtime downloader or cloud inference is included.
ONNX Runtime has separate MIT/third-party notices in the project runtime dependency; the final S3 build must retain those notices.
'''
(target/'NOTICE.md').write_bytes(notice.encode('utf-8'))
provenance=dict(upstreamModel='Qwen/Qwen3-0.6B',upstreamRevision=original_revision,exportRepo='onnx-community/Qwen3-0.6B-ONNX',exportRevision=export_revision,
    sourceModelSha256=source_sha,packagedModelSha256=model_sha,weightsUnchangedByLastPositionGather=True,
    numericalAudit=dict(record='Equivalence_B.json',sha256=digest(candidate/'Equivalence_B.json'),comparisons=equivalence['comparisons']),
    transformationRecordSha256=digest(candidate/'LastHeadTransformation.json'),sourceMetadataSha256=digest(candidate/'Downloads_small.json'),
    tokenizerProfile=dict(baseVocabulary=151643,merges=151387,addedTokens=26,definedTokens=151669,normalizer='NFC',mergeStorage='pair-array'),
    tensorProfile=dict(layers=28,queryHeads=16,kvHeads=8,headSize=128,vocabulary=151936,inputs=59,outputs=57),
    candidateOnly=True,productionTokenizerAndGameAcceptance='pending')
(target/'PROVENANCE.json').write_bytes(encoded(provenance))
files=[dict(name=path.name,bytes=path.stat().st_size,sha256=digest(path)) for path in sorted(target.iterdir())]
total=sum(item['bytes'] for item in files)
assert total<1024**3 and len(files)<=16
manifest=dict(version=1,releaseId=release,files=files)
(target/'manifest.json').write_bytes(encoded(manifest))
lines=['// Generated by Tools/Stage3/stage_qwen3_release.py; old catalog is unchanged.','namespace EchoStation.AI.Models','{',' public static class Qwen3Catalog',' {','  public const string DirectoryName="Qwen3_06B_Q4";','  public static ModelRelease Current {get;}=new ModelRelease("'+release+'",new ModelFileSpec[]{']
lines.extend('   new ModelFileSpec('+json.dumps(item['name'])+','+str(item['bytes'])+'L,'+json.dumps(item['sha256'])+'),' for item in files)
lines.extend(['  });',' }','}'])
with catalog.open('xb') as output:output.write(('\n'.join(lines)+'\n').encode('utf-8'))
evidence.mkdir(parents=True,exist_ok=True)
(evidence/'StagedRelease.json').write_bytes(encoded(dict(path=target.relative_to(root).as_posix(),manifest=manifest,sumBytes=total,budgetBefore=current,budgetProjected=current+planned,sourceFilesVerified=True)))
print(json.dumps(dict(releaseId=release,files=len(files),sumBytes=total,path=target.relative_to(root).as_posix(),resourceProjectedGiB=(current+planned)/1024**3)))
