"""Copy a bounded selection from the verified CC0 Standard ZIP; never overwrite."""
import hashlib,json,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Docs/Assets/Sources/SalvagePrototype/Quaternius/Modular SciFi MegaKit[Standard].zip'
DEST=ROOT/'Assets/EchoStation/SalvageNext/ThirdParty/Quaternius'
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()=='6fae60cf5189e44dff0bd91097f094a765acc6d57d64a85a0cc0dd56e03035e3'
selected={
 'Platforms':['Platform_Simple','Platform_CenterPlate','Platform_Metal2','Door_Frame_Square','Door_Frame_SquareTall'],
 'Walls':['WallAstra_Straight','WallBand_Straight','TopSimple_Straight','BottomSimple_Straight','ShortWall_Simple1_Straight','ShortWall_MetalPlates_Straight','TopCables_Straight'],
 'Columns':['Column_Simple','Column_MetalSupport'],
 'Props':['Prop_Computer','Prop_AccessPoint','Prop_Light_Small','Prop_Light_Wide','Prop_Fan_Small','Prop_Cable_1','Prop_Chest','Prop_Crate3','Prop_Crate4','Prop_Barrel_Large','Prop_ItemHolder','Prop_Light_Floor']}
prefix='Modular SciFi MegaKit[Standard]/'
with zipfile.ZipFile(SOURCE) as archive:
    paths=['FBX/'+folder+'/'+name+'.fbx' for folder,names in selected.items() for name in names]
    paths += [p[len(prefix):] for p in archive.namelist() if p.startswith(prefix+'Textures/T_Trim_') and p.endswith('.png')]
    paths += ['License_Standard.txt']
    for relative in paths:
        if (DEST/relative).exists():raise FileExistsError('Preserve existing adapted source: '+relative)
        archive.getinfo(prefix+relative)
    records=[]
    for relative in paths:
        raw=archive.read(prefix+relative);path=DEST/relative;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(raw)
        records.append(dict(path=path.relative_to(ROOT).as_posix(),bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest()))
manifest=ROOT/'Docs/Tests/SalvagePrototype/Evidence/R4-A01b/Extraction.json';manifest.parent.mkdir(parents=True,exist_ok=True)
assert not manifest.exists()
manifest.write_text(json.dumps(dict(sourceSha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),models=sum(map(len,selected.values())),selection=selected,files=records,scope='copied raw FBX/PNG/CC0 license only, no third-party code'),ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(models=sum(map(len,selected.values())),files=len(records),bytes=sum(r['bytes'] for r in records))))
