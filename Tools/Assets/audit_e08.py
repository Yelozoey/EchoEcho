"""Audit saved scan evidence. Decode UTF-8 explicitly on Windows."""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
E=ROOT/'Docs/Tests/Stage1/Evidence/S1-E08'
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
run=read(E/'Regression.json')['structuredContent']['result']['data'];tests=run['result']
protected=read(ROOT/'Docs/Tests/Stage1/Evidence/S1-W01/ProtectedFiles.json')['templates'];options=read(E/'EditorOptions-Final.json');scans=read(E/'ActualScans.json');automatic=read(E/'AutonomousScan.json')
checks={'tests81Passed':len(tests['results'])==81 and all(t['state']=='Passed' for t in tests['results']),
 'protected13Unchanged':len(protected)==13 and all(hashlib.sha256((ROOT/item['path']).read_bytes()).hexdigest()==item['sha256'] for item in protected),
 'editorOptionsRestored':options['enabled'] and options['options']==1,
 'fiveTargetScansAndDuplicate':scans['passed'] and scans['discoveries']==5 and len(scans['rows'])==6,
 'autonomousScanWithoutDrift':automatic['passed'] and automatic['positionDrift']<.01,
 'worldLightingRegression':read(E/'WorldRegressionImages/LightingReport.json')['passed']}
result={'passed':all(checks.values()),'assertions':checks,'wallSeconds':(run['finished_unix_ms']-run['started_unix_ms'])/1000,
 'frameworkSummary':tests['summary'],'targetHardwarePerformanceVerified':False}
(E/'FinalAudit.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2));raise SystemExit(0 if result['passed'] else 1)
