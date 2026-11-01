"""Launch only the independently built, explicitly authorized Windows validation player."""
import argparse,json,shutil,subprocess,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mode',choices=['startup','fan','valve','write','read','failure','display','performance'],required=True);p.add_argument('--label',required=True);a=p.parse_args()
if not a.label or len(a.label)>40 or any(not c.isascii() or not(c.isalnum() or c in '_-') for c in a.label):raise SystemExit('Safe label required')
output=ROOT/'Docs/Tests/SalvagePrototype/Evidence/Expansion/V01'/('Windows_'+a.label+'_'+a.mode)
if output.exists():raise SystemExit('Fresh validation evidence required')
output.mkdir(parents=True);exe=ROOT/'Builds/SalvageExpansion/EchoStationSalvageExpansion.exe';log=output/'Player.log'
command=[str(exe),'-force-d3d11','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','--expansion-check',a.mode,'--expansion-session',a.label,'-logFile',str(log)]
start=time.monotonic();process=subprocess.Popen(command,cwd=ROOT)
(output/'Started.json').write_text(json.dumps({'pid':process.pid,'command':command},indent=2),encoding='utf-8');print('Started Windows '+a.mode+' PID '+str(process.pid),flush=True)
try:code=process.wait(timeout=180)
except subprocess.TimeoutExpired:
 process.terminate();process.wait(timeout=15);source=exe.parent/'Validation'/a.label
 if source.exists():shutil.copytree(source,output/'PlayerEvidence')
 (output/'Timeout.json').write_text(json.dumps({'pid':process.pid,'seconds':time.monotonic()-start}),encoding='utf-8');raise SystemExit('Owned test process timed out; inspect window/modal and preserve evidence')
source=exe.parent/'Validation'/a.label
if source.exists():shutil.copytree(source,output/'PlayerEvidence')
result_path=source/(a.mode+'_Result.json');result=json.loads(result_path.read_text(encoding='utf-8-sig')) if result_path.exists() else None
record={'pid':process.pid,'exitCode':code,'seconds':time.monotonic()-start,'result':result};(output/'Process.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'exitCode':code,'passed':result.get('passed') if result else None,'error':result.get('error') if result else 'Missing result','seconds':record['seconds']},ensure_ascii=False),flush=True)
if code!=0 or not result or not result['passed']:raise SystemExit('Actual Windows validation failed; inspect retained log/result')
