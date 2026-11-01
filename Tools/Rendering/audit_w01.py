"""Audit actual cabin evidence; no target-hardware performance claim."""
import hashlib,json,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
E=ROOT/'Docs/Tests/Stage1/Evidence/S1-W01'
def read(name):return json.loads((E/name).read_text(encoding='utf-8-sig'))
tests=read('EditMode-Final.json')['structuredContent']['result']['data']['result']['summary']
player=read('Player/PlayerProbe.json');visibility=read('VisibilityPlayer/PlayerProbe.json')
protected=[{'path':x['path'],'unchanged':hashlib.sha256((ROOT/x['path']).read_bytes()).hexdigest()==x['sha256']} for x in read('ProtectedFiles.json')['templates']]
pngs=[{'path':str(p.relative_to(ROOT)),'size':struct.unpack('>II',p.read_bytes()[16:24])} for p in (E/'Player').glob('*.png')]
options=json.loads((ROOT/'Docs/Tests/Stage1/Evidence/S1-R05/EditorOptions-Final.json').read_text(encoding='utf-8-sig'))
checks={'testsPassed':tests['total']==73 and tests['passed']==73 and tests['failed']==tests['skipped']==0,
 'assetsProtected':all(x['unchanged'] for x in protected),'buildPassed':read('PlayerBuild.json')['result']=='Succeeded' and read('PlayerBuild.json')['errors']==0,
 'playerPassed':player['passed'] and read('Player/Exit.json')['exitCode']==0 and not read('Player/Exit.json')['timedOut'],
 'all23Collisions':len(player['collision']['checks'])==23 and all(x['passed'] for x in player['collision']['checks']),
 'actualLightmapAndProbes':player['lighting']['passed'] and player['lighting']['atlases']==2 and player['lighting']['probes']==141,
 'actualDrawReduction':visibility['passed'] and visibility['samples'][0]['median']>visibility['samples'][1]['median']>0,
 'actual1080pImages':len(pngs)==6 and all(x['size']==(1920,1080) for x in pngs),
 'editorOptionsRestored':options['enabled'] and options['options']==1}
result={'passed':all(checks.values()),'assertions':checks,'tests':tests,'protected':protected,'pngs':pngs,'targetHardwarePerformanceVerified':False}
(E/'FinalAudit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'passed':result['passed'],'assertions':checks},ensure_ascii=False,indent=2))
raise SystemExit(0 if result['passed'] else 1)
