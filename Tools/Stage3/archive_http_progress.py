"""Pack and reverify only credited HTTP prefixes before switching transport.

This never deletes the preallocated source file and never claims the model is
complete. The receipt permits exact restoration of all recorded offsets.
"""
from pathlib import Path
import hashlib,json
from datetime import datetime,timezone
from download_qwen15_candidate import validated_resume,validated_partial_resume,sha256_file,safe_child,resource_bytes,check_budget

root=Path(__file__).resolve().parents[2]
cache=safe_child(root,'Library/EchoStation/S3Cache/models/qwen15-candidate')
evidence=safe_child(root,'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen15')
source=safe_child(cache,'onnx/onnx/model.onnx_data.part')
manifest_path=safe_child(cache,'onnx/onnx/model.onnx_data.ranges.json')
archive=safe_child(cache,'http_progress_B.bin')
output=evidence/'HTTPProgressArchive_B.json'
assert not archive.exists() and not output.exists(),'Fresh archive required'
failure=json.loads((evidence/'Downloads_Weights_B.json').read_text(encoding='utf-8'))
assert failure['completed'] is False and failure['verified'] is False,'Do not archive a running/successful job'
manifest=json.loads(manifest_path.read_text(encoding='utf-8'))
binding=manifest['binding'];chunk=manifest['chunkBytes']
assert binding['bytes']==6208411648 and binding['sha256']=='7111acf0e546fe192035357890402832315b4c243e56b01b47ce9358285fc82e'
completed=validated_resume(source,manifest,binding,chunk)
partial=validated_partial_resume(source,manifest,binding,chunk)
assert completed==manifest['completed'] and partial==manifest.get('partial',{}),'All recorded data must still verify'
segments=[]
for key,digest in completed.items():
    index=int(key);segments.append({'sourceOffset':index*chunk,'bytes':min(chunk,binding['bytes']-index*chunk),'sha256':digest})
for key,record in partial.items():
    segments.append({'sourceOffset':int(key)*chunk,**record})
segments.sort(key=lambda row:row['sourceOffset'])
total=sum(row['bytes'] for row in segments)
check_budget(resource_bytes(root)['totalBytes'],total,64*1024**2)
with source.open('rb') as original,archive.open('xb') as packed:
    for segment in segments:
        segment['archiveOffset']=packed.tell();original.seek(segment['sourceOffset']);remaining=segment['bytes'];digest=hashlib.sha256()
        while remaining:
            block=original.read(min(1024**2,remaining));assert block
            digest.update(block);packed.write(block);remaining-=len(block)
        assert digest.hexdigest()==segment['sha256']
assert archive.stat().st_size==total
for segment in segments:
    assert sha256_file(archive,segment['archiveOffset'],segment['bytes'])==segment['sha256']
record={'scope':'All recorded HTTP complete chunks/prefixes preserved, not a complete verified model',
        'verified':True,'createdUtc':datetime.now(timezone.utc).isoformat(),'source':str(source),'sourceBytes':source.stat().st_size,
        'manifest':str(manifest_path),'manifestSha256':sha256_file(manifest_path),'binding':binding,'segments':segments,
        'archive':str(archive),'archiveBytes':total,'archiveSha256':sha256_file(archive),'sourceDeleted':False}
output.write_bytes(json.dumps(record,indent=2).encode('utf-8'))
print(json.dumps({'verified':True,'segments':len(segments),'archiveBytes':total,'archiveSha256':record['archiveSha256']}))
