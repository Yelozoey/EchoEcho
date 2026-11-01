"""Build final evidence indices from actual successful processes and immutable test results."""
import json,math,statistics,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/MCP'));from preflight import payload
E=ROOT/'Docs/Tests/SalvagePrototype/Evidence/Expansion/V01';P=ROOT/'Docs/Tests/SalvagePrototype/Reports';D=ROOT/'Docs/SalvagePrototype/Expansion'
mapping=[('startup','Startup_D'),('fan','Fan_D'),('valve','Valve_D'),('write','Cross_D'),('read','Cross_D'),('failure','Failure_D'),('display','Display_D2'),('performance','Performance_D')]
rows=[]
for mode,label in mapping:
 folder=E/('Windows_'+label+'_'+mode);process=json.loads((folder/'Process.json').read_text(encoding='utf-8'));result=process['result'];quit=json.loads((folder/'PlayerEvidence/Quit.json').read_text(encoding='utf-8'))
 if process['exitCode']!=0 or not result['passed'] or not result['inputRestored'] or not result['profilerRestored'] or not quit['backgroundRestored']:raise SystemExit('Not a fully passing actual process: '+mode)
 rows.append({'mode':mode,'label':label,'processEvidence':folder.relative_to(ROOT).as_posix()+'/Process.json','passed':True,'exitCode':0,'actions':result['actions']})
checks={'passed':True,'buildReceipt':'Build_f3ce690cf8ff428fa3fed169eb7701d5.json','processes':rows,'historicalSuiteDDisplayFailureRetained':True,'displayRecheck':'Display_D2 same package, actual foreground window','automatedInputNotHumanPlaytest':True}
target=E/'FinalWindowsChecks_A.json'
if target.exists():raise SystemExit('Preserve existing summary')
target.write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
perf=json.loads((E/'Windows_Performance_D_performance/PlayerEvidence/performance_Result.json').read_text(encoding='utf-8'));samples=[s for s in perf['samples'] if s['valid']]
def pct(values,p=.95):return sorted(values)[math.ceil(len(values)*p)-1]
metrics={'validFrames':len(samples),'mainP95ms':pct([s['mainMs'] for s in samples]),'gpuP95ms':pct([s['gpuMs'] for s in samples]),'gcMedianBytes':statistics.median(s['gcBytes'] for s in samples),'gcP95Bytes':pct([s['gcBytes'] for s in samples]),'memoryMaxBytes':max(s['usedBytes'] for s in samples),'drawCallsMax':max(s['drawCalls'] for s in samples)}
quality_rows=[]
for q in range(3):
 for z in range(3):
  group=[s for s in samples if s['quality']==q and s['zone']==z]
  if len(group)<80:raise SystemExit('Insufficient actual GPU group')
  quality_rows.append({'quality':q,'zone':z,'frames':len(group),'mainP95ms':pct([s['mainMs'] for s in group]),'gpuP95ms':pct([s['gpuMs'] for s in group]),'gcMedianBytes':statistics.median(s['gcBytes'] for s in group),'drawCallsMax':max(s['drawCalls'] for s in group)})
(E/'PerformanceSummary_A.json').write_text(json.dumps({'hardware':{'cpu':perf['cpu'],'gpu':perf['gpu']},'metrics':metrics,'groups':quality_rows,'actions':perf['actions'],'targetHardwareAcceptance':False},ensure_ascii=False,indent=2),encoding='utf-8')
lines=['# 环境回收切片开发机性能实测','','2026-10-31。最终构建f3ce690，Windows独立可见窗口、D3D11、1920×1080、普通Forward、内部HDR转SDR、UseLLM=false。显式验证模式临时60FPS/vSync0，退出恢复；真实Application.isFocused检查、非零CPU/GPU时间与唯一时间戳过滤，未用虚拟焦点代替前台。','',f'开发机：{perf["cpu"]} / {perf["gpu"]}。有效样本{len(samples)}/810。百分位按最近秩计算。','','| 画质 | 区域 | 有效帧 | 主线程P95 ms | GPU P95 ms | GC中位 B/帧 | Draw Calls峰值 |','| --- | --- | ---: | ---: | ---: | ---: | ---: |']
for row in quality_rows:lines.append(f'| {("低","中","高")[row["quality"]]} | {row["zone"]+1} | {row["frames"]} | {row["mainP95ms"]:.3f} | {row["gpuP95ms"]:.3f} | {row["gcMedianBytes"]:.0f} | {row["drawCallsMax"]} |')
lines+=['',f'总体主线程/GPU P95={metrics["mainP95ms"]:.3f}/{metrics["gpuP95ms"]:.3f}ms；GC中位{metrics["gcMedianBytes"]:.0f}B/帧、P95 {metrics["gcP95Bytes"]}B/帧；Profiler总使用内存峰值{metrics["memoryMaxBytes"]/1024**2:.2f}MiB（不是整机RAM/VRAM峰值）。HUD/状态/诊断采样仍有分配，不称全部帧零GC。','','前轮Performance_B同机同流程Main/GPU P95约4.530/2.379ms、GC中位1816B；移除Get/Held/Paused热点LINQ后本轮中位0B。两轮存在场景时序差异，不能把所有计时差值当单变量因果证明。','','实际验证远端房间0/2距离卸载、Renderer/Lightmap引用清理且流送无失败；动态物品carving与拾回恢复已在FinalRegression_B实际验证。使用已落地的静态烘焙、合并Mesh、原三区PVS、Instancing/三LOD/Impostor、分级阴影和SRP Batcher；新局部蒸汽雾为半分辨率16步并随供电抑制。','','此为短时开发机采样，不能替代目标硬件或长时稳定性。最低i5-8400/GTX1060 6GB/16GB、中画质1080p60FPS；推荐i5-12400/RTX3060 6GB/16GB、高画质1080p60FPS；Windows10另机、首次真人5–8分钟与趣味性均待外部验证。','','原始证据：Evidence/Expansion/V01/Windows_Performance_D_performance/PlayerEvidence/performance_Result.json；汇总PerformanceSummary_A.json。所有失败轮、警告与采样前条件保留。']
(D/'Performance.md').write_text('\n'.join(lines)+'\n',encoding='utf-8');print(json.dumps({'windowsProcesses':len(rows),'metrics':metrics},ensure_ascii=False))
