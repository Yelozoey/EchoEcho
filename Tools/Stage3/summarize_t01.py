"""Summarize a completed raw T01 report without altering its cases or evidence."""
import argparse,hashlib,json
from collections import Counter
from pathlib import Path
from qwen3_tuning_metrics import timing

p=argparse.ArgumentParser();p.add_argument('--raw',required=True);p.add_argument('--output',required=True);a=p.parse_args()
raw=Path(a.raw);out=Path(a.output)
if out.exists():raise SystemExit('Fresh summary path required')
data=raw.read_bytes();report=json.loads(data)
if not all(report.get(k) is True for k in ('finished','completed','ownerCleanupComplete')):
 raise SystemExit('Wait for complete suite and actual Owner cleanup; partial output is not acceptance')
rows=report['requests']
if len(rows)!=100 or len({r['id'] for r in rows})!=100:raise SystemExit('Exactly100 completed attempts required')
strict=sum(r.get('strictJson') is True for r in rows)
correct=sum(r.get('semanticCorrect') is True for r in rows)
confusion=Counter((r['expected'],r.get('actual','Error')) for r in rows)
summary=dict(raw=str(raw),rawSha256=hashlib.sha256(data).hexdigest(),scope=report['scope'],profileId=report.get('profileId'),promptSha256=report['promptSha256'],suiteSha256=report['suiteSha256'],attempts=100,strictJsonCorrect=strict,semanticCorrect=correct,qualityThresholdsPassed=strict>=95 and correct>=90,coldSeconds=report.get('coldSeconds'),timing=timing(rows),mainThread=report['mainThread'],ownerThread=report['ownerThread'],nativeRunThread=report.get('nativeRunThread'),disposeThread=report.get('disposeThread'),nativeCalls=report.get('nativeCalls'),cancelled=report.get('cancelled'),error=report.get('error'),confusion=[dict(expected=e,actual=v,count=n) for (e,v),n in sorted(confusion.items())],failures=[{k:r.get(k) for k in ('id','setup','input','expected','expectedTarget','actual','actualTarget','raw','error')} for r in rows if not r.get('semanticCorrect')])
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in summary.items() if k not in ('confusion','failures')},ensure_ascii=False))
