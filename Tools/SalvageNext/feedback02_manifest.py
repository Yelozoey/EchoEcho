"""Final receipt only after real Windows runs, regression, restoration, and protection pass."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
E=ROOT/'Docs/Tests/SalvagePrototype/Evidence'
P=E/'Feedback02/P02'
def read(path):return json.loads(path.read_text('utf-8-sig'))
def sha(path):
    with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
target=P/'DeliveryManifest_A.json'
assert not target.exists()
reg=read(P/'FullNext_B.json');reg=json.loads(next(x['text'] for x in reg['content'] if x['type']=='text'))['data'];summary=reg['result']['summary']
assert reg['status']=='succeeded' and summary['total']==summary['passed']==208 and summary['failed']==summary['skipped']==0
build=read(E/'R4-P01b-2/Build_19e0fe12451e4c9696623a4f177e70e7.json')
assert build['passed'] and build['errors']==0 and build['scene'].endswith('/ArtV2/ES_ArtV2_Streamed.unity')
signature=read(E/'R4-P01c/Product_Feedback02_Final_A.json')
assert signature['passed'] and not signature['changed']
for p,h in signature['files'].items():assert sha(ROOT/p)==h,p
restored=read(P/'NativeRestored_A.json')
assert all(x['nativeExact'] and x['diskExact'] and x['dirtyExact'] for x in restored)
workspace=read(P/'FinalWorkspace_A.json')
assert workspace['scene'].endswith('/AI/Scenes/ES_S3_AI_Demo.unity') and not workspace['sceneDirty'] and not workspace['playing']
assert workspace['defaultPipeline'].endswith('/VC_ForwardHDR.asset') and workspace['qualityPipeline']==workspace['defaultPipeline']
assert workspace['quality']==1 and workspace['aa']==4 and workspace['api']=='Direct3D11'
assert workspace['nativeDiskDirtyExact'] and workspace['ordinaryForward'] and workspace['internalHDR'] and not workspace['displayHDR']
audit=read(E/'R3R4/Audit_Feedback02_Final_A.json')
assert audit['underLimit'] and audit['protectedFiles']==3610
assert set(audit['changedProtectedFiles'])=={'Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/EditorBuildSettings.asset'}
for label,modes in [('Feedback02_A',['startup','display','write','read','route','alternate']),('Feedback02_Perf_A',['performance'])]:
    for mode in modes:
        result=read(E/'R4-P01c/Windows'/label/(mode+'_Result.json'))
        assert result['passed'] and result['scene']=='ES_ArtV2_Streamed' and not result['useLLM'] and result['inputRestored']
perf=read(P/'PerformanceSummary_A.json');assert sum(x['valid'] for x in perf['rows'])==1620
release=ROOT/'Builds/SalvageNext'
assert not (release/'Validation').exists() and not (release/'EchoStationSalvageNext_Data/StreamingAssets/EchoLLM').exists()
assert (release/'README_zh-CN.md').read_bytes()==(ROOT/'Docs/SalvagePrototype/WindowsPlayGuide.md').read_bytes()
assert (release/'FirstPlayTestForm.md').read_bytes()==(ROOT/'Docs/SalvagePrototype/FirstPlayTestForm.md').read_bytes()
files={p.relative_to(release).as_posix():{'bytes':p.stat().st_size,'sha256':sha(p)} for p in sorted(release.rglob('*')) if p.is_file()}
total=sum(x['bytes'] for x in files.values())
result={'build':'19e0fe12451e4c9696623a4f177e70e7','scene':build['scene'],'entry':str(release/'EchoStationSalvageNext.exe'),'fileCount':len(files),'bytes':total,'MiB':total/1024**2,'files':files,'regression':summary,'regressionWallSeconds':(reg['finished_unix_ms']-reg['started_unix_ms'])/1000,'productSignatureFiles':len(signature['files']),'windowsModes':7,'validGpuSamples':1620,'restorationVerified':True,'protectedFiles':3610,'budgetGiB':audit['combinedGiB'],'useLLM':False,'limitations':['Target hardware and Windows10 not executed','No external first-player results','R3 model quality No-Go; development build']}
target.write_text(json.dumps(result,ensure_ascii=False,indent=2),'utf-8')
print(json.dumps({k:result[k] for k in ['fileCount','MiB','regressionWallSeconds','productSignatureFiles','budgetGiB']}))
