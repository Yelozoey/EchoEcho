"""Repair the diagnosed disk-only scalar; preserve every unrelated byte."""
import hashlib
import json
import os
import re
from pathlib import Path
root = Path(__file__).resolve().parents[2]
path = root / 'ProjectSettings/ProjectSettings.asset'
old = path.read_bytes()
pattern = rb'(?m)^  enableFrameTimingStats: ([01])\r?$'
hits = list(re.finditer(pattern, old))
assert len(hits) == 1
item = hits[0]
start, end = item.span(1)
new = old[:start] + b'0' + old[end:]
if new != old:
    temp = path.with_name(path.name + '.echostation.tmp')
    with temp.open('xb') as stream:
        stream.write(new)
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temp, path)
assert path.read_bytes() == new
evidence = root / 'Docs/Tests/Stage2/Evidence/S2-V03/FrameTimingDiskRepair.json'
assert not evidence.exists()
evidence.write_text(json.dumps({'before': item.group(1).decode(), 'after': '0',
  'onlyExpectedScalarChanged': old[:start]+old[end:] == new[:start]+new[end:],
  'sha256Before': hashlib.sha256(old).hexdigest(), 'sha256After': hashlib.sha256(new).hexdigest()}, indent=2), encoding='utf-8')
print(evidence.read_text(encoding='utf-8'))
