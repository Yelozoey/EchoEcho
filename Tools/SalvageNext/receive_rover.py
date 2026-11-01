"""Official author CC0 source intake only; no extraction, import or execution."""
import argparse,importlib.util,json,zipfile
from pathlib import Path
from datetime import datetime,timezone
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Docs/Assets/Sources/SalvageNext'
PAGE='https://opengameart.org/content/mars-rover'
URL='https://opengameart.org/sites/default/files/001_mars_rover.zip'
ARCHIVE=BASE/'OpenGameArt/001_mars_rover.zip'
RECEIPT=BASE/'RoverReceipt.json'
spec=importlib.util.spec_from_file_location('trusted_asset_intake',ROOT/'Tools/Assets/receive_salvage_sources.py')
intake=importlib.util.module_from_spec(spec);spec.loader.exec_module(intake);intake.BASE=BASE
def verify():
    receipt=json.loads(RECEIPT.read_text(encoding='utf-8'))
    assert receipt['status']=='received' and receipt['license']=='CC0-1.0' and receipt['licenseEvidence']['cc0Observed']
    file=receipt['file'];assert ARCHIVE.stat().st_size==file['bytes'] and intake.sha(ARCHIVE)==file['sha256']
    audit=intake.archive_audit(ARCHIVE)
    with zipfile.ZipFile(ARCHIVE) as archive:
        files=[p.filename for p in archive.infolist() if not p.is_dir()]
    formats=sorted({Path(p).suffix.lower() for p in files})
    assert any(ext in formats for ext in ['.blend','.obj','.fbx','.dae','.gltf','.glb'])
    return dict(bytes=file['bytes'],sha256=file['sha256'],formats=formats,files=files,archiveAudit=audit,imported=False,executed=False)
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--verify',action='store_true');args=parser.parse_args()
    if not args.verify:
        if RECEIPT.exists() or ARCHIVE.exists():raise FileExistsError('Preserve received asset; use --verify')
        evidence=intake.license_evidence(PAGE)
        record=intake.fetch_file(URL,ARCHIVE)
        receipt=dict(utc=datetime.now(timezone.utc).isoformat(),status='received',author='JamesWhite',page=PAGE,download=URL,license='CC0-1.0',licenseEvidence=evidence,file=record,scope='raw source only, no execution or Unity import')
        intake.json_save(RECEIPT,receipt)
    result=verify();intake.json_save(BASE/'RoverVerification.json',result);print(json.dumps(result,ensure_ascii=True))
if __name__=='__main__':main()
