"""Select data-only meshes/textures into the isolated sample; preserve all old sources."""
import hashlib, json, zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/'Assets/EchoStation/InfiltrationArt/ThirdParty'
EVIDENCE=ROOT/'Docs/Tests/InfiltrationPrototype/Evidence/A00'
receipt=EVIDENCE/'SelectedSources_A.json'
if receipt.exists():raise SystemExit('Preserve intake receipt')
sources=ROOT/'Docs/Assets/Sources'
old_manifest=json.loads((sources/'SalvagePrototype/Manifest.json').read_text(encoding='utf-8'))
new_manifest=json.loads((sources/'InfiltrationPrototype/Manifest.json').read_text(encoding='utf-8'))
rows=[]
for group,selection in [
    ('modular-sci-fi-megakit',['Modular SciFi MegaKit[Standard]/FBX/Platforms/Door_Simple.fbx']),
    ('sci-fi-essentials-kit',['FBX (Unity)/Prop_Desk_Medium.fbx','FBX (Unity)/Prop_Chair.fbx']+
       ['Textures/T_Props_'+batch+'_'+suffix+'.png' for batch in ['Batch1','Batch2'] for suffix in ['BaseColor','Normal','ORM']]),
    ('lowpoly-robot',['FBX/Robot.fbx','License.txt'])]:
    entry=next(x for x in (old_manifest['assets']+new_manifest['assets']) if x['id']==group)
    assert entry['license']=='CC0-1.0' and entry['licenseEvidence']['cc0Observed']
    source=entry['files'][0];archive_path=ROOT/source['path']
    assert hashlib.sha256(archive_path.read_bytes()).hexdigest()==source['sha256'],'Source archive changed'
    with zipfile.ZipFile(archive_path) as archive:
        for member in selection:
            data=archive.read(member)
            target=DEST/group/Path(member).name
            target.parent.mkdir(parents=True,exist_ok=True)
            if target.exists():raise SystemExit('Preserve existing extracted asset')
            assert target.suffix.lower() in {'.fbx','.png','.txt'}
            target.write_bytes(data)
            rows.append({'path':target.relative_to(ROOT).as_posix(),'sourceArchive':source['path'],
                         'sourceMember':member,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),
                         'license':'CC0-1.0','page':entry['page']})
receipt.write_text(json.dumps({'files':rows,'executedDownloadedCode':False},indent=2),encoding='utf-8')
print(json.dumps({'selectedFiles':len(rows),'bytes':sum(r['bytes'] for r in rows),'dataOnly':True}))
