"""Receive only official free art for A00; no executables, account, payment or model downloads."""
import hashlib, importlib.util, json, sys, zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('art_receiver',ROOT/'Tools/Assets/receive_salvage_sources.py')
receiver=importlib.util.module_from_spec(spec);spec.loader.exec_module(receiver)
receiver.BASE=ROOT/'Docs/Assets/Sources/InfiltrationPrototype'
receiver.MAX_BYTES=48*1024**2
receiver.MAX_EXPANDED=96*1024**2
receiver.BASE.mkdir(parents=True,exist_ok=True)
manifest=receiver.BASE/'Manifest.json'
if manifest.exists():
    raise SystemExit('Preserve original intake receipt')
record=receiver.itch('lowpoly-robot','Animated Robot by Quaternius.zip',None)
record['edition']='Original free animated robot'
receiver.json_save(manifest,{'task':'A00','assets':[record],'scope':'Official free raw art only; no scripts executed or Unity import'})
archive=ROOT/record['files'][0]['path']
audit=json.loads(archive.with_suffix('.inventory.json').read_text(encoding='utf-8'))
print(json.dumps({'received':True,'zipBytes':archive.stat().st_size,'expandedBytes':audit['expandedBytes'],
                 'cc0':record['licenseEvidence']['cc0Observed'],'entries':[s['path'] for s in audit['entries']]},ensure_ascii=True))
