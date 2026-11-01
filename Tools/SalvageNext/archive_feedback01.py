"""Byte-verified archive before the next visual release; never removes originals."""
import hashlib, json, zipfile
from pathlib import Path
root=Path(__file__).resolve().parents[2]
out=root/'Docs/Tests/SalvagePrototype/Evidence/Feedback02'
out.mkdir(parents=True,exist_ok=True)
archive=out/'Feedback01_Windows.zip'
if archive.exists(): raise FileExistsError(archive)
build=root/'Builds/SalvageNext'
files=sorted(p for p in build.rglob('*') if p.is_file())
digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
expected={p.relative_to(build).as_posix():digest(p) for p in files}
prior=json.loads((root/'Docs/Tests/SalvagePrototype/Evidence/R4-Feedback01/DeliveryManifest_F.json').read_text('utf-8'))
with zipfile.ZipFile(archive,'x',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for p in files:z.write(p,p.relative_to(build).as_posix())
with zipfile.ZipFile(archive) as z:
    assert set(z.namelist())==set(expected)
    for name,sha in expected.items():assert hashlib.sha256(z.read(name)).hexdigest()==sha,name
owned={p.relative_to(root).as_posix():digest(p) for base in ['Assets/EchoStation/SalvageNext','Assets/EchoStation/Tests/SalvageNextEditor','Assets/EchoStation/Tests/SalvageNextFixtures','Assets/AddressableAssetsData'] for p in (root/base).rglob('*') if p.is_file()}
(out/'BeforeAssets.json').write_text(json.dumps(owned,indent=2),'utf-8')
(out/'Archive_A.json').write_text(json.dumps({'files':len(files),'sourceBytes':sum(p.stat().st_size for p in files),'archiveBytes':archive.stat().st_size,'verified':True,'sha256':digest(archive),'sourceManifest':expected},indent=2),'utf-8')
print(json.dumps({'files':len(files),'verified':True,'archiveBytes':archive.stat().st_size,'protectedCurrentAssets':len(owned)}))
