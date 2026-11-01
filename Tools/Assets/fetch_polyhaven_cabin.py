"""Development-only asset fetcher. Powered by Poly Haven (https://polyhaven.com)."""
import hashlib,json,urllib.request
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/'Docs/Assets/Sources/PolyHaven'
DEST.mkdir(parents=True,exist_ok=True)
HEADER={'User-Agent':'EchoStation-asset-preparation/1.0 (offline Unity project; Powered by Poly Haven)'}
def get(url):
    with urllib.request.urlopen(urllib.request.Request(url,headers=HEADER),timeout=45) as r:return r.read()
license_url='https://polyhaven.com/license'
license_html=get(license_url)
if b'CC0' not in license_html or b'commercial' not in license_html:raise RuntimeError('Official license verification failed; no assets imported')
tos_url='https://raw.githubusercontent.com/Poly-Haven/Public-API/master/ToS.md'
tos=get(tos_url);(DEST/'API-Terms-2026-10-25.txt').write_bytes(tos)
manifest={'retrievedUtc':datetime.now(timezone.utc).isoformat(),'provider':'Poly Haven','credit':'Powered by Poly Haven',
 'license':'CC0-1.0','licenseUrl':license_url,'licenseHtmlSha256':hashlib.sha256(license_html).hexdigest(),
 'apiTermsUrl':tos_url,'assets':[]}
for asset,author in [('concrete_floor_02','Rob Tuytel'),('painted_plaster_wall','Amal Kumar')]:
    info=json.loads(get('https://api.polyhaven.com/info/'+asset));files=json.loads(get('https://api.polyhaven.com/files/'+asset))
    (DEST/(asset+'-info.json')).write_text(json.dumps(info,indent=2),encoding='utf-8')
    (DEST/(asset+'-files.json')).write_text(json.dumps(files,indent=2),encoding='utf-8')
    entry={'id':asset,'author':author,'page':'https://polyhaven.com/a/'+asset,'license':'CC0-1.0','widthMeters':2,'files':[]}
    for kind,label in [('Diffuse','BaseColor'),('nor_gl','Normal'),('arm','ARM')]:
        row=files[kind]['1k']['png'];path=DEST/(asset+'_'+label+'.png')
        raw=path.read_bytes() if path.exists() else get(row['url'])
        if len(raw)!=row['size'] or hashlib.md5(raw).hexdigest()!=row['md5']:raise RuntimeError('Official integrity mismatch: '+str(path))
        if not path.exists():path.write_bytes(raw)
        entry['files'].append({'kind':label,'path':str(path.relative_to(ROOT)).replace('\\','/'),'url':row['url'],'bytes':len(raw),'md5':row['md5'],'sha256':hashlib.sha256(raw).hexdigest()})
        print(asset,label,len(raw),'verified',flush=True)
    manifest['assets'].append(entry)
(DEST/'Manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('PASS: 6 files verified against official size/MD5; CC0 source recorded. No runtime network dependency.')
