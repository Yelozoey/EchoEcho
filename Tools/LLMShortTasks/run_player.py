"""Launch the owned diagnostic/recheck with isolated evidence, never an old save slot."""
import argparse,json,subprocess,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('--build',required=True);p.add_argument('--label',required=True);p.add_argument('--seconds',type=int,default=600);a=p.parse_args();evidence=ROOT/'Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/IME01'/a.label
if evidence.exists():raise SystemExit('Fresh evidence label required')
evidence.mkdir();exe=ROOT/'Builds/LLMShortTasks/IME01'/a.build/'EchoStationIME.exe'
args=[str(exe),'-force-d3d11','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','--echo-ime-report',str(evidence/'NativeEvents.json'),'-logFile',str(evidence/'Player.log')]
start=time.monotonic();process=subprocess.Popen(args,cwd=ROOT);(evidence/'Started.json').write_text(json.dumps({'pid':process.pid,'args':args},indent=2),encoding='utf-8');print('Owned IME diagnostic PID '+str(process.pid),flush=True)
try:code=process.wait(timeout=a.seconds)
except subprocess.TimeoutExpired:process.terminate();code=process.wait(timeout=15);(evidence/'Timeout.json').write_text(json.dumps({'pid':process.pid}),encoding='utf-8')
(evidence/'Process.json').write_text(json.dumps({'pid':process.pid,'exitCode':code,'seconds':time.monotonic()-start},indent=2),encoding='utf-8');print('Exit '+str(code),flush=True)
