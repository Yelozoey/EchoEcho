"""Read-only current S3 resource accounting, including candidate intermediates."""
from pathlib import Path
import argparse,json
root=Path(__file__).resolve().parents[2];parser=argparse.ArgumentParser();parser.add_argument('--output',required=True);args=parser.parse_args();output=(root/args.output).resolve();assert output.is_relative_to(root/'Docs/Tests') and not output.exists(),'Fresh evidence output required'
baseline=json.loads((root/'Docs/Tests/Stage3/Evidence/S3-I03/ResourceBudget.json').read_text(encoding='utf-8'));paths=[root/x['path'] for x in baseline['parts']]+[root/'Builds/S3'];parts=[]
# Include every owned S3 build/tool-cache and AI test directory, not only the early G01 probe.
paths+=list((root/'Library/EchoStation').glob('S3*'))+list((root/'Builds/Tests').glob('S3*'))+list((root/'Assets/EchoStation/Tests').glob('AI*'))
selected=[]
for base in sorted(set(p.resolve() for p in paths),key=lambda p:(len(p.parts),str(p).casefold())):
 assert base.is_relative_to(root),'Resource accounting must remain within the project'
 if any(parent==base or parent in base.parents for parent in selected):continue
 selected.append(base)
for base in selected:
 size=base.stat().st_size if base.is_file() else sum(p.stat().st_size for p in base.rglob('*') if p.is_file()) if base.exists() else 0
 parts.append({'path':base.relative_to(root).as_posix(),'bytes':size})
total=sum(x['bytes'] for x in parts);record={'scope':'current S3 cache/environments/staging/production/all S3 builds/evidence/tools/all AI tests including range pieces and candidates; deduplicated read-only stat accounting','parts':parts,'totalBytes':total,'limitBytes':20*1024**3,'underLimit':total<=20*1024**3}
output.parent.mkdir(parents=True,exist_ok=True);output.write_bytes(json.dumps(record,indent=2).encode('utf-8'));print(json.dumps({'totalBytes':total,'totalGiB':total/1024**3,'remainingGiB':(20*1024**3-total)/1024**3,'underLimit':record['underLimit']}));assert record['underLimit']
