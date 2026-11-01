"""Offline verification of the exact OFL font snapshot used by the UI."""
import hashlib,json,struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
manifest=json.loads((ROOT/'Docs/Assets/Sources/Noto/Manifest.json').read_text(encoding='utf-8-sig'))
checks={item['path']:((ROOT/item['path']).stat().st_size==item['bytes'] and hashlib.sha256((ROOT/item['path']).read_bytes()).hexdigest()==item['sha256']) for item in manifest['files']}
font=(ROOT/'Assets/EchoStation/ThirdParty/Noto/Resources/NotoSansCJKsc-Regular.otf').read_bytes()
count=struct.unpack('>H',font[4:6])[0]
tables=[font[12+i*16:16+i*16] for i in range(count)]
checks['staticCff1']=font[:4]==b'OTTO' and b'CFF ' in tables and b'CFF2' not in tables and b'fvar' not in tables
checks['licenseAndCopyrightPresent']='SIL OPEN FONT LICENSE Version 1.1' in (ROOT/'Assets/EchoStation/ThirdParty/Noto/LICENSE.txt').read_text(encoding='utf-8-sig') and 'Adobe' in (ROOT/'Assets/EchoStation/ThirdParty/Noto/COPYRIGHT.txt').read_text(encoding='utf-8-sig')
result={'passed':all(checks.values()),'checks':checks,'version':manifest['fontNames']['5']}
path=ROOT/'Docs/Tests/Stage1/Evidence/S1-P04/FontAudit.json'
path.write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2));raise SystemExit(0 if result['passed'] else 1)
