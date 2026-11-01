"""Summarize real visible Player samples, never infer target-machine acceptance."""
import argparse
import json
import math
from pathlib import Path

parser=argparse.ArgumentParser()
parser.add_argument('result',type=Path)
parser.add_argument('--output',required=True,type=Path)
args=parser.parse_args()
assert not args.output.exists(),'Fresh evidence only'
data=json.loads(args.result.read_text(encoding='utf-8-sig'))
samples=data.get('samples') or []
def percentile(items,p):
    values=sorted(items)
    return values[max(0,math.ceil(len(values)*p)-1)] if values else None
rows=[]
for quality in range(3):
    for zone in range(3):
        group=[s for s in samples if s['quality']==quality and s['zone']==zone]
        valid=[s for s in group if s['valid']]
        row={'quality':['Low','Medium','High'][quality],'zone':zone,'total':len(group),'valid':len(valid)}
        for field in ['cpuMs','gpuMs','mainMs','renderMs','deltaMs','gcBytes','usedBytes','drawCalls']:
            values=[s[field] for s in valid]
            row[field]={'p50':percentile(values,.5),'p95':percentile(values,.95),'max':max(values) if values else None}
        rows.append(row)
report={'source':str(args.result.resolve()),'sourcePassed':data.get('passed'),'cpu':data.get('cpu'),'gpu':data.get('gpu'),'resolution':[data.get('width'),data.get('height')],'api':data.get('api'),'validNonzeroGpuRequired':True,'rows':rows,'targetHardwareAcceptance':'Not executed: development PC is not the minimum/recommended target hardware','scope':'Visible Development Player; includes instrumentation and product presentation. No claim of zero whole-frame allocation.'}
args.output.parent.mkdir(parents=True,exist_ok=True)
args.output.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'groups':len(rows),'valid':sum(r['valid'] for r in rows),'cpu':report['cpu'],'gpu':report['gpu']},ensure_ascii=False))
