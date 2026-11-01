"""Run explicitly authorized visible local Unity validation windows; preserve each result."""
import argparse
import json
import re
import shutil
import subprocess
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--label', required=True)
parser.add_argument('--modes', nargs='+', choices=['startup', 'write', 'read', 'route', 'alternate'], default=['startup', 'write', 'read', 'route', 'alternate'])
args = parser.parse_args()
assert re.fullmatch(r'[A-Za-z0-9_-]{1,40}', args.label), 'Safe unique session label required'
exe = ROOT / 'Builds/Salvage/R2/EchoStationSalvage.exe'
evidence = ROOT / 'Docs/Tests/SalvagePrototype/Evidence/R2-V01b/Windows' / args.label
runtime = exe.parent / 'ValidationR2' / args.label
assert exe.is_file(), 'Build must succeed first'
assert not evidence.exists() and not runtime.exists(), 'Use a fresh label; no existing evidence is overwritten'
evidence.mkdir(parents=True)
records = []
for mode in args.modes:
    log = evidence / (mode + '_Player.log')
    command = [str(exe), '-force-d3d11', '-screen-width', '1920', '-screen-height', '1080',
               '-screen-fullscreen', '0', '-logFile', str(log), '--salvage-r2-validation', mode,
               '--salvage-r2-validation-session', args.label]
    started = time.monotonic()
    process = subprocess.Popen(command, cwd=ROOT)
    print(json.dumps({'mode': mode, 'pid': process.pid, 'event': 'started'}, ensure_ascii=False), flush=True)
    timed_out = False
    while process.poll() is None:
        if time.monotonic() - started > 260:
            timed_out = True
            process.terminate()  # Only this task-owned validation process, never the Editor.
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=10)
            break
        time.sleep(1)
    source = runtime / (mode + '_Result.json')
    data = json.loads(source.read_text(encoding='utf-8-sig')) if source.is_file() else None
    if source.is_file():
        shutil.copyfile(source, evidence / source.name)
    for png in runtime.glob('*.png') if runtime.exists() else []:
        target = evidence / png.name
        if not target.exists():
            shutil.copyfile(png, target)
    record = {'mode': mode, 'pid': process.pid, 'exitCode': process.returncode,
              'wallSeconds': time.monotonic() - started, 'timedOut': timed_out, 'result': data}
    records.append(record)
    (evidence / 'Processes.json').write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'mode': mode, 'exitCode': process.returncode, 'passed': data and data.get('passed'),
                      'wallSeconds': record['wallSeconds'], 'error': data and data.get('error')}, ensure_ascii=False), flush=True)
    assert not timed_out and process.returncode == 0 and data and data.get('passed'), 'Actual Windows run failed; evidence preserved'
    assert data['api'] == 'Direct3D11' and data['width'] == 1920 and data['height'] == 1080, 'Actual API/resolution mismatch'
    assert data['inputRestored'] and not data['useLLM'], 'Input cleanup / script baseline mismatch'
    assert data['hdr'] and not data['displayHdr'] and data['pipeline'] == 'ES_S2_ForwardHDR', 'Actual render binding mismatch'
print('All selected actual Windows processes passed', flush=True)
