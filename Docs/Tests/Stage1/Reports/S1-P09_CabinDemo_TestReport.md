# S1-P09 小屋探索Demo最终验收

2026-10-25完成。A接线/原生指令/模态与专用导航、B独立烘焙/实际照明/PVS、C Windows完整探索/跨进程读档/显示/Profiler、D真实GC定位与修复按序记录，各独立报告保留。最终工程全量244/244通过、0失败/0跳过；作业墙钟246.642秒。框架根duration4.750秒受跨模式汇总影响，不能当完整耗时。证据`Evidence/S1-P09/FullRegression.json`。

后置审计发现T09基线幂等性测试会切回PC基线（F081），已按资产路径finally还原；相关R01与Demo配置14/14再通过，空测试工作区恢复原场景、播放选项并核对Gameplay绑定。未重写交付Windows包；该修复只涉及Editor测试清理。13份模板/基线保护SHA与原小屋/导航/烘焙14份SHA保持，786份历史证据保留；本轮新截图/数据在FullRegressionArtifacts与BindingCleanupRegressionArtifacts中，未覆盖历史。

Windows最终完整流程与同版本独立进程恢复均ExitCode0，内部HDR→SDR/普通Forward/D3D11/1080p/UseLLM=false。真实扫描/三色/波纹/日志/手电/保存恢复及显示选项通过，900个有效采样帧全部0B GC、GPU P95最高2.101ms、内存峰值335.206MiB；这是开发机三个稳定视点诊断，不是目标最低/推荐硬件或全游戏峰值性能验收。

产物：`Builds/S1/EchoStation.exe`及完整依赖目录；`Assets/EchoStation/World/Scenes/ES_Cabin_Demo.unity`；根README、SourceArchitecture、渲染配置、性能文档与DevLog。保留原模板/灰盒，Demo有专用Lightmap/PVS/NavMesh数据。源码按Runtime/Editor/测试程序集组织，无LLM实例，Echo没有开门/修改供电能力。

阶段边界：只验收小屋基础探索原型；没有声称已完成五区、25–40分钟、固定全部谜题或三个结局。高/中/低正式预设与目标硬件验收仍后续阶段。S1全部31模块通过后停止，不进入S2。
