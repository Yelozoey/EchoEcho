"""Stat the established combined resource roots plus this isolated expansion."""
import argparse,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser();parser.add_argument('--output',required=True);args=parser.parse_args()
target=(ROOT/args.output).resolve()
if not target.is_relative_to(ROOT/'Docs/Tests') or target.exists():raise SystemExit('Fresh test evidence path required')
baseline=json.loads((ROOT/'Docs/Tests/SalvagePrototype/Evidence/R3R4/Audit_Feedback02_Final_A.json').read_text(encoding='utf-8-sig'))
paths=[ROOT/p['path'] for p in baseline['parts']]
paths += [ROOT/p for p in ['Assets/EchoStation/SalvageExpansion','Assets/EchoStation/Tests/SalvageExpansionEditor','Tools/SalvageExpansion','Library/EchoStation/SalvageExpansion','Builds/SalvageExpansion']]
selected=[]
for path in sorted(set(p.resolve() for p in paths),key=lambda p:(len(p.parts),str(p).lower())):
 if not path.is_relative_to(ROOT):raise SystemExit('Budget scope outside workspace')
 if any(p==path or p in path.parents for p in selected):continue
 selected.append(path)
def size(path):return path.stat().st_size if path.is_file() else sum(p.stat().st_size for p in path.rglob('*') if p.is_file()) if path.exists() else 0
parts=[{'path':p.relative_to(ROOT).as_posix(),'bytes':size(p)} for p in selected]
total=sum(p['bytes'] for p in parts);models=size(ROOT/'Assets/StreamingAssets/EchoLLM')
previous=json.loads((ROOT/'Docs/Tests/SalvagePrototype/Evidence/R4-P01b-2/Build_19e0fe12451e4c9696623a4f177e70e7.json').read_text(encoding='utf-8-sig'))
record={'parts':parts,'combinedBytes':total,'combinedGiB':total/1024**3,'limitBytes':20*1024**3,'underLimit':total<=20*1024**3,'projectModelBytes':models,'previousActuallyCopiedModelBytes':previous['newOutputModelBytesRemoved'],'estimatedNewBuildFloorBytes':total+models+265552257,'estimateNote':'Current retained resources + automatic StreamingAssets model copy + previous clean package size; excludes additional build/cache growth, not a guaranteed peak.'}
target.parent.mkdir(parents=True,exist_ok=True);target.write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:record[k] for k in ('combinedGiB','underLimit','projectModelBytes','previousActuallyCopiedModelBytes','estimatedNewBuildFloorBytes')},ensure_ascii=False))
if not record['underLimit']:raise SystemExit('Resource limit exceeded')
