"""Validate saved R05 evidence, retaining the distinction between draws and SRP submissions."""
import hashlib
import json
import re
import struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
E=ROOT/'Docs/Tests/Stage1/Evidence/S1-R05'
def read(name):return json.loads((E/name).read_text(encoding='utf-8-sig'))
def fields(text):
    found=list(re.finditer(r'^  (\w+):',text,re.M))
    return {m.group(1):text[m.end():found[i+1].start() if i+1<len(found) else len(text)].strip() for i,m in enumerate(found)}
pipeline='Assets/EchoStation/Rendering/Settings/ES_PC_ForwardHDR.asset'
before=fields((E/'Pipeline.before.txt').read_text(encoding='utf-8-sig'))
after=fields((ROOT/pipeline).read_text(encoding='utf-8-sig'))
changed=sorted(k for k in before.keys()|after.keys() if before.get(k)!=after.get(k))
derived=[k for k in changed if k.startswith('m_Prefilter') or k in {'m_Light2DPrefilteringMode','m_Light2DKeptVariantCombos'}]
effective=[k for k in changed if k not in derived]
protected=[]
for item in read('ProtectedFiles.json')['templates']:
    unchanged=hashlib.sha256((ROOT/item['path']).read_bytes()).hexdigest()==item['sha256']
    protected.append({'path':item['path'],'rawShaUnchanged':unchanged,'approvedCheckPassed':unchanged or (item['path']==pipeline and not effective)})
tests=read('EditMode-Final.json')['structuredContent']['result']['data']['result']['summary']
player=read('Player/PlayerProbe.json');exit_data=read('Player/Exit.json');build=read('PlayerBuild.json')
occlusion=read('Editor/OcclusionReport.json');dynamic=read('Dynamic/BatcherReport.json')
editor_options=read('EditorOptions-Final.json')
pngs=[]
for path in list((E/'Player').glob('*.png'))+list((E/'Editor').glob('*.png'))+list((E/'Dynamic').glob('*.png')):
    width,height=struct.unpack('>II',path.read_bytes()[16:24]);pngs.append({'file':str(path.relative_to(ROOT)),'width':width,'height':height})
result={'protected':protected,'engineDerivedPipelineChanges':derived,'effectivePipelineChanges':effective,'tests':tests,
        'build':build,'playerStates':player.get('states'),'pngs':pngs,'targetHardwarePerformanceVerified':False,
        'assertions':{'priorAssetsProtected':all(p['approvedCheckPassed'] for p in protected),'noPipelineInputChanges':not effective,
                      'testsPassed':tests['total']>=63 and tests['passed']==tests['total'] and tests['failed']==tests['skipped']==0,
                      'playerPassed':player['passed'] and exit_data['exitCode']==0 and not exit_data['timedOut'],
                      'buildPassed':build['result']=='Succeeded' and build['errors']==0,
                      'actualEditorOcclusionContrast':occlusion['passed'] and occlusion['opaqueBatchesOn']<occlusion['opaqueBatchesOff'],
                      'actualDynamicSrpPath':dynamic['passed'] and dynamic['presentSrpBatches']>dynamic['absentSrpBatches'],
                      'editorOptionsRestored':editor_options['enabled'] and editor_options['options']==1,
                      'actual1080pImages':len(pngs)>=10 and all(p['width']==1920 and p['height']==1080 for p in pngs)}}
result['passed']=all(result['assertions'].values());(E/'FinalAudit.json').write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({'passed':result['passed'],'assertions':result['assertions'],'effectivePipelineChanges':effective},indent=2))
raise SystemExit(0 if result['passed'] else 1)
