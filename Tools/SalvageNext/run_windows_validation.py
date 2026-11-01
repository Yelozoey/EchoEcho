"""Visible task-owned Windows processes, isolated slots, fresh evidence only."""
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
parser.add_argument('--modes', nargs='+', choices=['startup','display','write','read','route','alternate','performance'], default=['startup','display','write','read','route','alternate','performance'])
args = parser.parse_args()
assert re.fullmatch(r'[A-Za-z0-9_-]{1,40}', args.label)
exe = ROOT/'Builds/SalvageNext/EchoStationSalvageNext.exe'
evidence = ROOT/'Docs/Tests/SalvagePrototype/Evidence/R4-P01c/Windows'/args.label
runtime = exe.parent/'Validation'/args.label
assert exe.is_file() and not evidence.exists() and not runtime.exists(), 'Fresh label and successful build required'
evidence.mkdir(parents=True)
records=[]
for mode in args.modes:
    command=[str(exe),'-force-d3d11','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',str(evidence/(mode+'_Player.log')),'--salvage-next-validation',mode,'--salvage-next-validation-session',args.label]
    start=time.monotonic()
    process=subprocess.Popen(command,cwd=ROOT)
    print(json.dumps({'mode':mode,'event':'started','pid':process.pid}),flush=True)
    timed_out=False
    while process.poll() is None:
        if time.monotonic()-start>300:
            timed_out=True
            process.terminate()
            try:process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill();process.wait(timeout=10)
            break
        time.sleep(1)
    source=runtime/(mode+'_Result.json')
    result=json.loads(source.read_text(encoding='utf-8-sig')) if source.is_file() else None
    if source.is_file():shutil.copyfile(source,evidence/source.name)
    for image in runtime.glob('*.png') if runtime.exists() else []:
        if not (evidence/image.name).exists():shutil.copyfile(image,evidence/image.name)
    record={'mode':mode,'pid':process.pid,'exitCode':process.returncode,'timedOut':timed_out,'wallSeconds':time.monotonic()-start,'result':result}
    records.append(record)
    (evidence/'Processes.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'mode':mode,'passed':bool(result and result.get('passed')),'exitCode':process.returncode,'seconds':record['wallSeconds'],'error':result and result.get('error')},ensure_ascii=False),flush=True)
    assert not timed_out and process.returncode==0 and result and result.get('passed'),'Real Windows run failed; preserve evidence'
    assert result['api']=='Direct3D11' and result['width']==1920 and result['height']==1080
    assert result['hdr'] and not result['displayHdr'] and not result['useLLM'] and result['inputRestored']
    assert result['pipeline']=='ES_Next_Medium_ForwardHDR','Final dedicated medium binding required'
    log=(evidence/(mode+'_Player.log')).read_text(encoding='utf-8-sig',errors='replace')
    unexpected=[p for p in ['NullReferenceException','MissingReferenceException','InvalidOperationException:','ArgumentException:','Shader error','Missing shader (','RenderGraph Execution error','Assertion failed','Invalid pass'] if p in log]
    assert not unexpected, 'Unexpected runtime error markers: '+str(unexpected)
print('All selected Windows validation processes passed',flush=True)
