# S2-V03-A/B 最终性能验收

2026-10-26，Performance03 exit0/56.250秒，1500/1500有效唯一CPU/GPU帧、0无效/0重复，五区每帧0B GC；原始Profiler与Result/Summary保存。前两轮实际24B不删，原Profiler父节点定位StationSession捕获查询；去闭包后功能2/2，常态36000B→0B，F109补齐证据。

开发机主线程P95最高4.223ms、渲染1.003ms、GPU1.758ms；最长帧19.738ms，未删尖峰。五区Unity总已用最高367.651MiB，Draw Calls中位76/101/54/40/27。实际PVS两轮43→38，受控图像差0，临时Renderer/光/相机状态恢复=true。完整统计/方法/报警线及局限见Docs/Performance/S2_Performance.md，不宣称目标硬件60FPS或全程时长已通过。

性能构建最终Succeeded/32.159秒/0错误/2既有警告。F108修复后finally恢复内存与磁盘FrameTimingStats=false，SettingsAudit03再查实际0、Editor空闲、Profiler空帧；只写任务自有标量，无SaveAssets。F107/F108首失败与getter-only记录保留。A/B通过，进入C交付构建与最终实走。
