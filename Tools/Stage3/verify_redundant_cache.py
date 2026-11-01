"""Verify only reconstructible task-owned duplicates; never deletes files."""
from pathlib import Path
import hashlib, json

root=Path(__file__).resolve().parents[2]
ev=root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen15'
ev.mkdir(parents=True,exist_ok=True)
output=ev/'DuplicateCacheVerification_A.json'
assert not output.exists(), 'Fresh evidence required'
def digest(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
stage=(root/'Library/EchoStation/S3Staging/Qwen25_05B_W8A8').resolve(strict=True)
production=(root/'Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8').resolve(strict=True)
pieces=(root/'Library/EchoStation/S3Cache/models/qwen3-candidate/onnx/onnx/model_q4.ranges').resolve(strict=True)
source=pieces.parent/'model_q4.onnx'
assert stage.is_relative_to(root/'Library/EchoStation/S3Staging')
assert pieces.is_relative_to(root/'Library/EchoStation/S3Cache')
assert not stage.is_symlink() and not pieces.is_symlink()
records=[]
for path in sorted(stage.iterdir()):
    assert path.is_file() and not path.is_symlink(), 'Unexpected staging content'
    peer=production/path.name
    sha=digest(path)
    assert peer.is_file() and peer.stat().st_size==path.stat().st_size and digest(peer)==sha
    records.append({'path':str(path),'bytes':path.stat().st_size,'sha256':sha,'retainedIdenticalSource':str(peer)})
download=json.loads((root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3/Downloads_ranges.json').read_text())
assert digest(source)==download['sha256'] and source.stat().st_size==download['bytes']
files=sorted(pieces.iterdir())
assert len(files)==download['ranges'] and all(p.name==f'{i:03}.part' for i,p in enumerate(files))
with source.open('rb') as stream:
    for path in files:
        assert path.is_file() and not path.is_symlink()
        block=stream.read(path.stat().st_size)
        sha=digest(path)
        assert hashlib.sha256(block).hexdigest()==sha
        records.append({'path':str(path),'bytes':path.stat().st_size,'sha256':sha,'retainedIdenticalSource':str(source)})
    assert not stream.read(1)
record={'purpose':'Verified reconstructible task-owned duplicate files only; production, source models and all historical evidence retained',
        'verified':True,'roots':[str(stage),str(pieces)],'files':records,'reclaimBytes':sum(x['bytes'] for x in records)}
output.write_bytes(json.dumps(record,indent=2).encode('utf-8'))
print(json.dumps({'verified':True,'files':len(records),'reclaimBytes':record['reclaimBytes'],'evidence':str(output)}))
