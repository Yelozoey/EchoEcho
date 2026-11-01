"""Read Windows process memory with OS counters; never modifies the target process."""
import argparse,ctypes,json,time
from ctypes import wintypes
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--pid',type=int,required=True);p.add_argument('--output',required=True);p.add_argument('--done',required=True);p.add_argument('--seconds',type=int,default=90);a=p.parse_args()
path=Path(a.output)
if path.exists():raise SystemExit('Fresh memory evidence required')
class Counters(ctypes.Structure):
 _fields_=[('cb',wintypes.DWORD),('pageFaultCount',wintypes.DWORD)]+[(k,ctypes.c_size_t) for k in ['peakWorkingSet','workingSet','quotaPeakPagedPool','quotaPagedPool','quotaPeakNonPagedPool','quotaNonPagedPool','pagefile','peakPagefile','private']]
kernel=ctypes.WinDLL('kernel32',use_last_error=True);psapi=ctypes.WinDLL('psapi',use_last_error=True)
kernel.OpenProcess.restype=wintypes.HANDLE;kernel.OpenProcess.argtypes=[wintypes.DWORD,wintypes.BOOL,wintypes.DWORD];kernel.CloseHandle.argtypes=[wintypes.HANDLE]
psapi.GetProcessMemoryInfo.argtypes=[wintypes.HANDLE,ctypes.POINTER(Counters),wintypes.DWORD]
handle=kernel.OpenProcess(0x1010,False,a.pid)
if not handle:raise ctypes.WinError(ctypes.get_last_error())
samples=[];start=time.monotonic();finished=None
try:
 while time.monotonic()-start<a.seconds:
  c=Counters();c.cb=ctypes.sizeof(c)
  if not psapi.GetProcessMemoryInfo(handle,ctypes.byref(c),c.cb):raise ctypes.WinError(ctypes.get_last_error())
  samples.append({'seconds':time.monotonic()-start,'workingSet':c.workingSet,'private':c.private,'peakWorkingSet':c.peakWorkingSet})
  if Path(a.done).exists():
   if finished is None:finished=time.monotonic()
   if time.monotonic()-finished>2:break
  time.sleep(.1)
finally:kernel.CloseHandle(handle)
record={'pid':a.pid,'source':'Windows GetProcessMemoryInfo PROCESS_MEMORY_COUNTERS_EX','doneObserved':finished is not None,'samples':samples,'maxPrivate':max(s['private'] for s in samples),'maxWorkingSet':max(s['workingSet'] for s in samples),'privateDeltaPeak':max(s['private'] for s in samples)-samples[0]['private']}
path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(record,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in record.items() if k!='samples'},indent=2))
