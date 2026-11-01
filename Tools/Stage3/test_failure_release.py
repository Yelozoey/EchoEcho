"""Five sequential expected backend failures, each fully returned before the next starts."""
import json,subprocess,sys,time
from pathlib import Path
root=Path(__file__).resolve().parents[2];ev=root/'Docs/Tests/Stage3/Evidence/S3-G01';records=[]
for index in range(5):
 tag=f'MissingModel_B{index+1:02}';output=ev/(tag+'.json')
 subprocess.run([sys.executable,'-X','utf8',str(root/'Tools/Stage3/run_unity_probe.py'),'--tag',tag,'--model','__g01_missing_model__.onnx','--reference','WEIGHT8_ACC4_A01','--repeats','1'],cwd=root,check=True,timeout=15)
 end=time.monotonic()+10
 while not output.exists():
  if time.monotonic()>end:raise TimeoutError('Failure owner did not return')
  time.sleep(.1)
 result=json.loads(output.read_text(encoding='utf-8'));assert not result['passed'] and result['releasedOnThread']==result['ownerThread']!=result['mainThread'] and '[ErrorCode:NoSuchFile]' in result['error'],result
 records.append({'tag':tag,'owner':result['ownerThread'],'released':True,'expectedFailure':True});print('expected release',index+1,flush=True)
(ev/'FailureReleaseSummary.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
