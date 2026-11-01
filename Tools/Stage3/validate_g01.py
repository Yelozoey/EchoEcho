"""Verify independent G01 evidence. A fixture's passed flag is never sufficient alone."""
import hashlib,json,math,sys
from pathlib import Path
root=Path(__file__).resolve().parents[2];ev=root/'Docs/Tests/Stage3/Evidence/S3-G01'
sys.path.insert(0,str(root/'Tools/MCP'))
from preflight import payload
def read(name):return json.loads((ev/name).read_text(encoding='utf-8-sig'))
checks=[]
def check(id,value,detail):
 checks.append({'id':id,'passed':bool(value),'detail':detail})
 if not value:print('FAILED',id,detail)
before=read('PackagesBefore.json')['dependencies'];current=read('PackagesAfterG01.json')['dependencies']
builds=[json.loads(p.read_text()) for p in sorted(ev.glob('Build_*.json'))];build=builds[-1]
check('G01-01',all(current.get(k)==v for k,v in before.items()) and current.get('com.unity.barracuda')=='3.0.2' and build['result']=='Succeeded' and build['errors']==0,'Pinned Barracuda3.0.2 and ORT1.31; no prior package version changed; actual Windows build')
transform=read('LastHeadProbeTransformation.json');model=root/'Library/EchoStation/S3Cache/models/qwen-int4/onnx/model_weight8_last.onnx'
with model.open('rb') as stream:digest=hashlib.file_digest(stream,'sha256').hexdigest()
audit=read('Weight8GraphAudit.json');quant=audit['quantNodes']
check('G01-02',digest==transform['sha256'] and len(quant)==168 and all(n['attributes']['bits']==8 for n in quant) and transform['weightsUnchanged'] and model.stat().st_size<1024**3,'Actual168 packed INT8 MatMulNBits; hash/size checked; Apache2 original license retained')
w=read('Windows_B01/WindowsResult.json');short=w['short'];steps=[s for r in short['rounds'] for s in r['steps']]
check('G01-03',short['tinyAdd']==[3,5] and short['ownerThread']!=short['mainThread'],'Real native Add output3,5 on Owner')
orig=read('WEIGHT8_ACC4_A01/OriginalReference.json')
check('G01-04',short['passed'] and orig['allWithinTolerance'] and all(s['referenceMaxAbsoluteError']<=.002 for s in steps),'Original FP32 cosine/RMS/top10 limits and same-INT8 native reference<=0.002')
expected=read('WEIGHT8_ACC4_A01/PythonOrtReference.json')['generated']
check('G01-05',all([s['token'] for s in r['steps']]==expected for r in short['rounds']),'Three self-generated tokens match pinned Python reference in each round')
check('G01-06',all(s['ownerThread']==short['ownerThread']!=w['mainThread'] for s in steps) and short['releasedOnThread']==short['ownerThread'],'Creation/Run/Dispose Owner differs from Unity main; no main inference')
long=read('UnityOrt_Last_Long_A01.json');memory=read('UnityOrt_Last_Long_A01.Memory.json');lsteps=long['rounds'][0]['steps'];longref=read('WEIGHT8_LAST_LONG_A01/PythonOrtReference.json')
check('G01-07',long['passed'] and long['inputTokens']==1536 and [s['token'] for s in lsteps]==longref['generated'] and memory['doneObserved'] and 0<memory['privateDeltaPeak']<=3*1024**3 and lsteps[0]['logitsShape']==[1,1,151936] and lsteps[0]['kvShape']==[1,2,1536,64],'1536input, actual KV, finite logits, external OS memory delta<=3GiB; failed unoptimized evidence retained')
fixture=read('PrefillFixture.json');longtext=longref['decoded']
check('G01-08',fixture['inputTokens']==len(fixture['inputIds']) and fixture['eos']==151645 and longtext=='你是Echo，','Official tokenizer IDs matched upstream/converted; actual Chinese token decode matches reference. Production tokenizer remains A02')
failures=read('FailureReleaseSummary.json')
check('G01-09',len(short['rounds'])==5 and len(failures)==5 and all(f['released'] and f['expectedFailure'] for f in failures),'5 actual success create/Run/Dispose cycles and5 sequential expected missing-weight releases')
process=read('Windows_B01/Process.json')
check('G01-10',process['exitCode']==0 and not process['timedOut'] and w['unity']=='6000.6.7f1' and w['api']=='Direct3D11' and w['width']==1920 and w['height']==1080,'Actual Win64 visible1920x1080/D3D11 runtime with native DLLs, model read from local probe cache')
frames=[f for f in w['frames'] if f['cpuMs']>0 and f['gpuMs']>0];raw=ev/'Windows_B01/G01Profiler.raw'
check('G01-11',w['passed'] and raw.exists() and raw.stat().st_size>1024**2 and len(frames)>0 and len({f['timestamp'] for f in frames})==len(frames) and all(s['ownerThread']!=w['mainThread'] for s in steps),'Real greybox renders during actual inference; valid unique CPU/GPU frames plus raw Profiler. Full request/game acceptance remains V02')
protected=json.loads((root/'Docs/Tests/Stage3/Evidence/ProtectedS1S2-Verified.json').read_text())
check('G01-12',protected['unchanged'] and protected['checked']==767 and not read('BarracudaActualImport.json')['imported'] and 'Trilu' in read('BarracudaActualImport.json')['message'],'Actual Barracuda No-Go retained; conditional ORT route; S1/S2 protection767 unchanged')
suite=payload(read('G01NUnit_A.json'))['data']['result']['summary']
check('GuardedUnitySuite',suite['total']==5 and suite['failed']==0 and suite['resultState']=='Passed','Fresh scene/Prefab/Console/RenderSafety gate plus5 real Unity tests')
result={'passed':all(c['passed'] for c in checks),'checks':checks,'metrics':{k+'P95':sorted(f[k] for f in frames)[math.ceil(.95*len(frames))-1] for k in ['cpuMs','gpuMs','mainMs','renderMs']},'validUniqueFrames':len(frames),'modelBytes':model.stat().st_size,'privateDeltaPeakLong':memory['privateDeltaPeak'],'note':'Compatibility spike only. Not final JSON quality/latency/offline-disconnect/completeS3 acceptance.'}
(ev/'CompatibilityDecision.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in result.items() if k!='checks'},ensure_ascii=False,indent=2));raise SystemExit(0 if result['passed'] else 1)
