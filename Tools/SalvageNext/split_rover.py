"""Derive movable CC0 OBJ parts without modifying the original source archive."""
import hashlib,json,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Assets/EchoStation/SalvageNext/ThirdParty/MarsRover'
SOURCE=ROOT/'Docs/Assets/Sources/SalvageNext/OpenGameArt/001_mars_rover.zip'
EVIDENCE=ROOT/'Docs/Tests/SalvagePrototype/Evidence/R4-A01c-1'
SCALE=.25
def obj(raw):
    vertices=[];uvs=[];faces=[]
    for line in raw.decode('utf-8').splitlines():
        fields=line.split()
        if not fields:continue
        if fields[0]=='v':vertices.append(tuple(map(float,fields[1:4])))
        elif fields[0]=='vt':uvs.append(tuple(map(float,fields[1:3])))
        elif fields[0]=='f':faces.append([(int(t.split('/')[0])-1,int(t.split('/')[1])-1) for t in fields[1:]])
    return vertices,uvs,faces
def partition(vertices,faces):
    parts={'Body':[],'Head':[]}
    for side in ['Left','Right']:
        for place in ['Front','Middle','Back']:parts['Wheel_'+side+'_'+place]=[]
    for index,face in enumerate(faces):
        points=[vertices[i] for i,_ in face]
        if min(p[1] for p in points)>=1.93:name='Head'
        elif max(p[1] for p in points)<=.51 and min(abs(p[0]) for p in points)>=.64 and (all(p[0]>0 for p in points) or all(p[0]<0 for p in points)):
            side='Left' if sum(p[0] for p in points)>0 else 'Right' # Unity OBJ mirrors X, measured by ObjAxisProbe_A.
            z=sum(p[2] for p in points)/len(points);place=min([('Front',.8774295),('Middle',-.317809),('Back',-1.289329)],key=lambda pair:abs(pair[1]-z))[0]
            name='Wheel_'+side+'_'+place
        else:name='Body'
        parts[name].append(index)
    assert all(parts.values()),'Every independently movable part needs geometry'
    return parts
def export_part(vertices,uvs,faces,indices,pivot,name):
    vi={};ti={};polygons=[]
    for index in indices:
        face=[]
        for vertex,uv in faces[index]:
            if vertex not in vi:vi[vertex]=len(vi)+1
            if uv not in ti:ti[uv]=len(ti)+1
            face.append(str(vi[vertex])+'/'+str(ti[uv]))
        polygons.append('f '+' '.join(face))
    lines=['# CC0 JamesWhite Mars Rover; EchoStation derived movable part','o ES_Rover_'+name]
    lines += ['v '+' '.join(format((vertices[i][a]-pivot[a])*SCALE,'.9g') for a in range(3)) for i in vi]
    lines += ['vt '+' '.join(format(value,'.9g') for value in uvs[i]) for i in ti]
    lines += ['s off']+polygons
    return ('\n'.join(lines)+'\n').encode('utf-8')
def main():
    assert not BASE.exists(),'Never overwrite an existing adapted Rover source'
    receipt=json.loads((ROOT/'Docs/Assets/Sources/SalvageNext/RoverReceipt.json').read_text(encoding='utf-8'))
    assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==receipt['file']['sha256']
    axes=json.loads((EVIDENCE/'ObjAxisProbe_A.json').read_text(encoding='utf-8'));assert axes['unityCenter']==[-1.5,3,5]
    with zipfile.ZipFile(SOURCE) as archive:
        original=archive.read('rover_mesh.obj');texture=archive.read('rover_diffuse.png')
    vertices,uvs,faces=obj(original);parts=partition(vertices,faces)
    root=(0,min(p[1] for p in vertices),(min(p[2] for p in vertices)+max(p[2] for p in vertices))/2)
    outputs=[]
    for name,indices in parts.items():
        used={i for f in indices for i,_ in faces[f]};lo=[min(vertices[i][a] for i in used) for a in range(3)];hi=[max(vertices[i][a] for i in used) for a in range(3)]
        pivot=root if name=='Body' else tuple((lo[a]+hi[a])/2 for a in range(3))
        raw=export_part(vertices,uvs,faces,indices,pivot,name);path=BASE/'Derived'/(name+'.obj')
        position=[(pivot[a]-root[a])*SCALE*(-1 if a==0 else 1) for a in range(3)]
        outputs.append(dict(name=name,path=path.relative_to(ROOT).as_posix(),sourcePivot=pivot,unityPosition=position,sourceFaces=indices,faces=len(indices),sourceVertices=len(used),sha256=hashlib.sha256(raw).hexdigest(),radius=(hi[1]-lo[1])*SCALE/2 if name.startswith('Wheel_') else None,raw=raw))
    (BASE/'Original').mkdir(parents=True);(BASE/'Derived').mkdir()
    (BASE/'Original/rover_mesh.obj').write_bytes(original);(BASE/'Original/rover_diffuse.png').write_bytes(texture)
    for row in outputs:(ROOT/row['path']).write_bytes(row.pop('raw'))
    manifest=dict(sourceZipSha256=receipt['file']['sha256'],originalObjSha256=hashlib.sha256(original).hexdigest(),textureSha256=hashlib.sha256(texture).hexdigest(),license='CC0-1.0',author='JamesWhite',url=receipt['page'],scale=SCALE,sourceRootPivot=root,axisSigns=[-1,1,1],sourceVertices=len(vertices),sourceUvs=len(uvs),sourceFaces=len(faces),parts=outputs,scope='derived source geometry; no Unity rig/animation acceptance')
    (BASE/'RoverParts.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    (BASE/'SOURCE.md').write_text('# Mars Rover\n\nJamesWhite, CC0-1.0. '+receipt['page']+'\nOriginal archive SHA256: '+receipt['file']['sha256']+'\nOnly OBJ/PNG copied; .blend and Mac metadata not executed or imported. Derived files preserve every polygon and UV; scale/pivots in RoverParts.json.\n',encoding='utf-8')
    print(json.dumps(dict(parts=len(outputs),faces=len(faces),sourceVertices=len(vertices),partFaces={r['name']:r['faces'] for r in outputs})))
if __name__=='__main__':main()
