"""Finalize S2 delivery records only after all native delivery probes pass."""
import hashlib
import json
import re
import shutil
from pathlib import Path

root = Path(__file__).resolve().parents[2]
evidence = root / 'Docs/Tests/Stage2/Evidence/S2-V03'
results = {}
for label in ('Delivery_A01', 'Delivery_B01', 'Delivery_Normal01'):
    results[label] = json.loads((evidence / label / 'Result.json').read_text(encoding='utf-8-sig'))
    assert results[label]['passed'], label
for label in ('Delivery_A01', 'Delivery_B01'):
    process = json.loads((evidence / label / 'Process.json').read_text(encoding='utf-8-sig'))
    assert process['exitCode'] == 0 and not process['timedOut']
perf = json.loads((evidence / 'Windows_Performance03/Result.json').read_text(encoding='utf-8'))
assert perf['passed'] and len(perf['samples']) == 1500 and all(s['gcBytes'] == 0 for s in perf['samples'])
assert re.search(r'^  enableFrameTimingStats: 0\r?$', (root/'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8'), re.M)

reports = root / 'Docs/Tests/Stage2/Reports'
ids = [f'S2-{group}{i:02}' for group, count in [('M',7),('D',6),('W',6),('P',4),('F',4),('V',3)] for i in range(1,count+1)]
rows = []
for module in ids:
    matches = list(reports.glob(module + '_*TestReport.md'))
    if module == 'S2-V03':
        matches = [reports/'S2-V03_Delivery_TestReport.md']
    assert len(matches) == 1, (module, matches)
    rows.append(f'| {module} | 工程与可用环境测试通过 | [{matches[0].stem}](Reports/{matches[0].name}) |')
index = '# S2 模块验收索引\n\n2026-10-26。30/30工程模块已逐项测试归档；源码/玩法/Windows交付完成，目标硬件与人类首次体验的外部验收另列。子模块首失败/复测与原始证据均在各报告及Evidence中保留。\n\n| 模块 | 状态 | 独立报告 |\n| --- | --- | --- |\n' + '\n'.join(rows) + '\n\nV01三条空局路线3/3、数据207/207、场景/导航55/55；范围有重叠，不相加冒充唯一用例数。V03受影响区域用例2/2；交付版完整归档/跨进程/正常启动均exit0。性能03五区1500帧全有效/0重复/全部0B GC，原始Profiler保留。\n\n外部验收未完成：最低/推荐目标机器1080p中/高档60FPS、人类首次25–40分钟、系统IME候选窗人工体验与长时间最终画质/流送压力。当前为S2单场景灰盒，无LLM。S3模型/推理与S4正式场景/流送/画质不在本次完成范围。\n'
(root/'Docs/Tests/Stage2/Index.md').write_text(index, encoding='utf-8')
status = '当前状态（2026-10-26）：30/30工程模块已开发、独立测试与归档，Windows五区/六谜题/三结局可玩。最终归档实走、跨进程恢复、无测试参数启动均exit0；性能1500有效帧全部0B GC。目标硬件60FPS、首次25–40分钟与人工IME体验仍待外部验收。S2执行结束，未进入S3/S4。'
path = root/'Docs/Stage2/ModuleBreakdown.md'
path.write_text(re.sub(r'^当前状态.*$', status, path.read_text(encoding='utf-8'), flags=re.M), encoding='utf-8')
path = root/'Docs/DevelopmentPlan.md'
text = path.read_text(encoding='utf-8')
text = re.sub(r'^状态（2026-10-26）：.*$', '状态（2026-10-26）：S1保留31/31；S2工程模块30/30开发/独立测试/归档，五区六谜题三结局、脚本对话/摘要记忆与完整原子单槽已交付。三路线3/3、数据207/207、场景/导航55/55；最终Windows归档/跨进程/正常启动均exit0，开发机1500有效帧全0B GC。目标硬件60FPS与首次25–40分钟/人工IME仍待外部验收；S3/S4未开始。', text, flags=re.M)
path.write_text(text, encoding='utf-8')
path = root/'Docs/Stage2/GameplayProposal.md'
text = path.read_text(encoding='utf-8').replace('本方案已确认；当前生产进度为 M01 通过，正进入 M02，其余模块需依次开发并独立测试。', '本方案已按S2的30个工程模块逐项实现并测试归档，见Docs/Tests/Stage2/Index.md。正式目标硬件/首次体验时长仍待外部验收；保留v0.1作为批准的设计依据。')
path.write_text(text, encoding='utf-8')
readme = '''# EchoStation · S2 五区探索原型

Unity **6000.6.7f1** / 普通URP Forward / 内部HDR转SDR / D3D11 / Windows10或11 x64。S2工程模块30/30已逐项开发、测试、归档；Echo是本地脚本机器人，**UseLLM=false**。五区灰盒跑通六个环境谜题和三个玩家选择结局，无战斗。

运行 **`Builds/S2/EchoStation.exe`**，保留整个Builds/S2文件夹。编辑器打开`Assets/EchoStation/Station/Scenes/ES_Station_Greybox.unity`。此为S2非Development可玩构建，Steam发布准备属于S4。

WASD/鼠标/空格移动视角跳跃，E交互、F手电、J日志、T文字、Tab指令/显示，1跟随/2待命/3调查眼前目标，F5保存/F9读取。Esc释放鼠标，单击恢复。文字可用“调查储藏室可疑点”“调查校准架”“给我提示”；脚本识别预设中文，已知密码须明确问。Echo只提供信息/移动/扫描/警告，终端/门/回路由玩家操作。

小屋扫描后蓝色箭头指向地下A区校准架，继续下到A区调查；它只是跨区线索，完整流程延续到供电、通道终端和核心选择。路线、操作和后续提示见[无剧透玩法说明](Docs/Stage2/HowToPlay.md)，需要答案时看[完整攻略](Docs/Stage2/Walkthrough.md)。

S2独立手动单槽`Application.persistentDataPath/Saves/S2/slot1.json`，不自动保存/读取、不在结局或新局覆盖旧手动档。建议最终选择前F5保存；新局有确认，读档后单击恢复鼠标。S1程序仍在Builds/S1，与S2存档分离；[S1历史指南](Docs/Stage1/S1_UserGuide.md)保留。

- [开发总文档](Docs/DevelopmentPlan.md) / [30模块报告索引](Docs/Tests/Stage2/Index.md) / [连续授权](Docs/Stage2/ContinuousExecutionAuthorization.md)
- [源码架构](Docs/SourceArchitecture.md) / [S2渲染参数](Docs/Rendering/S2_RenderConfiguration.md) / [实际性能](Docs/Performance/S2_Performance.md)
- [预制体与命名](Docs/Stage2/PrefabAndAssetConventions.md) / [资源许可](Docs/Assets/ThirdPartyLicenses.md) / [故障经验](Docs/Troubleshooting.md) / [DevLog](Docs/DevLog.md)
- [Unity MCP工具](Tools/MCP)：使用module_tests.py安全门禁，不直接调用会保存用户场景的原始测试接口。

新局三结局路线3/3、数据207/207、场景/导航55/55；最终Windows完整归档/新进程读档/无测试参数启动均通过。开发机五区1500有效帧全部0B GC，PVS受控Draw Calls43→38且图差0，原始证据保留。

目标最低i5-8400/GTX1060 6GB/16GB中画质1080p60、推荐i5-12400/RTX3060 6GB/16GB高画质1080p60尚待实机；人类首次25–40分钟、系统IME候选窗体验及长期压力也未验收。当前开发机数据不能替代这些结论。S3本地Barracuda模型/推理、S4四区正式美术/Addressables流送/三档画质尚未开始；本次停在S2。
'''
(root/'README.md').write_text(readme, encoding='utf-8')
build = root/'Builds/S2'
(build/'Docs').mkdir(exist_ok=True)
for source in ('Docs/Stage2/HowToPlay.md','Docs/Stage2/Walkthrough.md','Docs/Assets/ThirdPartyLicenses.md'):
    shutil.copyfile(root/source, build/'Docs'/Path(source).name)
(build/'README.md').write_text('# EchoStation S2\n\n运行EchoStation.exe，保留整个文件夹。WASD/鼠标/空格移动，E交互、F手电、J日志、T文字、Tab指令，F5保存/F9读取。\n\n[操作与流程（无答案）](Docs/HowToPlay.md) / [完整攻略（剧透）](Docs/Walkthrough.md) / [资源许可](Docs/ThirdPartyLicenses.md)。\n\n当前S2五区灰盒、六谜题、三个结局，UseLLM=false。目标机器60FPS与人类首次25–40分钟仍待外部验收。字体许可保存在ThirdPartyNotices/Noto。\n', encoding='utf-8')
manifest = build/'DeliveryManifest.json'
files = [{'path':p.relative_to(build).as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(build.rglob('*')) if p.is_file() and p != manifest]
manifest.write_text(json.dumps({'stage':'S2','unity':'6000.6.7f1','api':'D3D11','developmentBuild':False,'useLLM':False,'files':files}, ensure_ascii=False, indent=2), encoding='utf-8')
route = results['Delivery_A01']['route']
with (root/'Docs/DevLog.md').open('a', encoding='utf-8') as stream:
    stream.write(f'\n2026-10-26：S2连续执行完成30个工程模块。摘要记忆/预算检索、预设中文对话、五区灰盒六谜题/三结局、全状态原子单槽与事务回滚逐项归档；三条新局路线3/3，数据207/207、场景/导航55/55。非Development交付Builds/S2，完整归档实际{route["wallSeconds"]:.3f}秒（已知答案自动操作者）、跨进程读档/正常启动均exit0。Profiler定位区域闭包，功能2/2后实际1500帧0B GC，PVS43→38/图差0；FrameTiming诊断设置内存/磁盘均恢复false。S1的475保护文件未变。目标硬件60FPS、首次25–40分钟与人工IME尚待外部验收；未开始S3/S4。\n')
print(json.dumps({'moduleReports':len(rows),'deliveryFiles':len(files),'bytes':sum(f['bytes'] for f in files),'manifest':str(manifest)}, ensure_ascii=False))
