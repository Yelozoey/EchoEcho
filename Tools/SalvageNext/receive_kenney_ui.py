"""Author-provided free download endpoint; validate archives before extracting selected art."""
import hashlib,json,re,urllib.request,zipfile,io
from pathlib import Path
root=Path(__file__).resolve().parents[2]
out=root/'Docs/Assets/Sources/SalvageNext/KenneyUI'
out.mkdir(parents=True,exist_ok=True)
headers={'User-Agent':'Mozilla/5.0','Accept-Encoding':'identity'}
def read(url,referer=None):
    with urllib.request.urlopen(urllib.request.Request(url,headers={**headers,**({'Referer':referer} if referer else {})}),timeout=30) as r:return r.read(32*1024*1024)
for slug in ['ui-pack-sci-fi','input-prompts','crosshair-pack']:
    receipt=out/(slug+'_receipt.json')
    if receipt.exists(): print(slug,'already received');continue
    page='https://kenney.nl/assets/'+slug
    html=read(page).decode('utf-8')
    assert 'creativecommons.org/publicdomain/zero/1.0/' in html
    token=re.search("data-download-id='([a-f0-9]+)'",html).group(1)
    url=json.loads(read('https://kenney.nl/download-link/'+token,page))['url']
    assert url.startswith('https://kenney.nl/download/')
    data=read(url,page)
    assert data[:4]==b'PK\x03\x04','Not a ZIP; no asset imported'
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        names=z.namelist();assert all(not n.startswith('/') and '..' not in Path(n).parts for n in names)
        license_name=next(n for n in names if 'license' in n.lower() and n.lower().endswith('.txt'))
        license_text=z.read(license_name).decode('utf-8-sig')
        assert 'CC0' in license_text or 'Creative Commons Zero' in license_text
        (out/(slug+'_LICENSE.txt')).write_text(license_text,'utf-8')
    (out/(slug+'.zip')).write_bytes(data)
    receipt.write_text(json.dumps({'source':page,'license':'CC0-1.0','bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'licenseFile':license_name,'files':names,'signedDownloadViaOfficialEndpoint':True},indent=2),'utf-8')
    print(json.dumps({'pack':slug,'bytes':len(data),'files':len(names),'licenseVerified':True}))
