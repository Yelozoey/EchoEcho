"""Archive and verify this run's player-only diagnostic copies, then clean the new release."""
import hashlib,json,re,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];build=(ROOT/'Builds/SalvageExpansion').resolve();source=(build/'Validation').resolve();dest=(ROOT/'Docs/Tests/SalvagePrototype/Evidence/Expansion/V01/AllPlayerEvidence_A').resolve();receipt=dest.parent/'PlayerEvidenceArchive_A.json'
if not source.is_relative_to(build) or source!=ROOT/'Builds/SalvageExpansion/Validation' or source.is_symlink() or not source.is_dir() or dest.exists() or receipt.exists():raise SystemExit('Only fresh, exact owned player diagnostics may be archived')
for child in source.iterdir():
 if not child.is_dir() or not re.fullmatch(r'(Startup|Fan|Valve|Cross|Failure|Display|Performance)_[A-D][0-9]?',child.name):raise SystemExit('Unexpected content; preserve source')
for file in source.rglob('*'):
 if file.is_symlink() or file.is_file() and file.suffix not in ('.json','.png'):raise SystemExit('Unexpected player evidence file; preserve source')
def manifest(root):return {p.relative_to(root).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in root.rglob('*') if p.is_file()}
before=manifest(source);shutil.copytree(source,dest);after=manifest(dest)
if before!=after:raise SystemExit('Archive bytes differ; do not remove source')
# Absolute targets are checked again immediately before the only recursive deletion.
if source.resolve()!=build/'Validation' or not source.resolve().is_relative_to(build):raise SystemExit('Deletion escaped explicitly named new release')
shutil.rmtree(source)
record={'source':str(source),'archive':str(dest),'files':before,'archiveExact':True,'sourceRemoved':not source.exists(),'oldDeliveriesUntouched':True};receipt.write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({'files':len(before),'bytes':sum(v['bytes'] for v in before.values()),'archiveExact':True,'sourceRemoved':not source.exists()}))
