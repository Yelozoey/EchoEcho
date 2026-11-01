"""8MiB probe using the existing urllib HTTPS proxy only in the curl child.

No proxy address/credential is printed or persisted, no machine setting changes.
"""
from pathlib import Path
import hashlib,json,os,subprocess,urllib.request
root=Path(__file__).resolve().parents[2]
output=root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen15/CurlTransport_B.json'
target=root/'Library/EchoStation/S3Cache/models/qwen15-candidate/transport_8MiB_B.part'
assert not output.exists() and not target.exists()
proxy=urllib.request.getproxies().get('https')
assert proxy,'No existing HTTPS proxy; do not invent a route'
environment=dict(os.environ);environment['HTTPS_PROXY']=proxy
command=['curl.exe','--location','--fail','--silent','--show-error','--range','67108864-75497471',
         '--max-filesize','8388608','--connect-timeout','20','--max-time','60','--output',str(target),
         '--write-out','{"httpCode":"%{http_code}","downloadedBytes":%{size_download},"seconds":%{time_total},"bytesPerSecond":%{speed_download}}',
         'https://huggingface.co/onnx-community/Qwen2.5-1.5B-Instruct/resolve/d54957e3af1be04546436e874a60fe386041b244/onnx/model.onnx_data']
result=subprocess.run(command,env=environment,capture_output=True,text=True,timeout=65)
record={'scope':'bounded transport probe only, not model download acceptance','exitCode':result.returncode,
        'existingProxyAppliedToChildOnly':True,'machineSettingsChanged':False,'statistics':json.loads(result.stdout),
        'stderr':result.stderr.replace(proxy,'<existing configured HTTPS proxy>')}
if result.returncode==0:
    assert target.stat().st_size==8388608
    with (root/'Library/EchoStation/S3Cache/models/qwen15-candidate/onnx/onnx/model.onnx_data.part').open('rb') as source:
        source.seek(67108864);expected=source.read(8388608)
    actual=target.read_bytes()
    assert actual==expected,'Range differs from previously completed verified local chunk'
    record['matchesCompletedChunkPrefix']=True
    record['sha256']=hashlib.sha256(actual).hexdigest()
output.write_bytes(json.dumps(record,indent=2).encode('utf-8'))
print(json.dumps({key:value for key,value in record.items() if key!='stderr'}))
