# 环境回收切片开发机性能实测

2026-10-31。最终构建f3ce690，Windows独立可见窗口、D3D11、1920×1080、普通Forward、内部HDR转SDR、UseLLM=false。显式验证模式临时60FPS/vSync0，退出恢复；真实Application.isFocused检查、非零CPU/GPU时间与唯一时间戳过滤，未用虚拟焦点代替前台。

开发机：12th Gen Intel(R) Core(TM) i7-12650H / NVIDIA GeForce RTX 4060 Laptop GPU。有效样本810/810。百分位按最近秩计算。

| 画质 | 区域 | 有效帧 | 主线程P95 ms | GPU P95 ms | GC中位 B/帧 | Draw Calls峰值 |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| 低 | 1 | 90 | 2.762 | 1.450 | 0 | 109 |
| 低 | 2 | 90 | 2.684 | 1.529 | 0 | 96 |
| 低 | 3 | 90 | 2.789 | 1.416 | 0 | 104 |
| 中 | 1 | 90 | 2.572 | 2.330 | 0 | 143 |
| 中 | 2 | 90 | 2.649 | 2.821 | 0 | 125 |
| 中 | 3 | 90 | 2.545 | 2.186 | 0 | 115 |
| 高 | 1 | 90 | 2.624 | 2.428 | 0 | 185 |
| 高 | 2 | 90 | 2.518 | 2.986 | 0 | 170 |
| 高 | 3 | 90 | 2.777 | 2.279 | 0 | 137 |

总体主线程/GPU P95=2.682/2.495ms；GC中位0B/帧、P95 3274B/帧；Profiler总使用内存峰值531.49MiB（不是整机RAM/VRAM峰值）。HUD/状态/诊断采样仍有分配，不称全部帧零GC。

前轮Performance_B同机同流程Main/GPU P95约4.530/2.379ms、GC中位1816B；移除Get/Held/Paused热点LINQ后本轮中位0B。两轮存在场景时序差异，不能把所有计时差值当单变量因果证明。

实际验证远端房间0/2距离卸载、Renderer/Lightmap引用清理且流送无失败；动态物品carving与拾回恢复已在FinalRegression_B实际验证。使用已落地的静态烘焙、合并Mesh、原三区PVS、Instancing/三LOD/Impostor、分级阴影和SRP Batcher；新局部蒸汽雾为半分辨率16步并随供电抑制。

此为短时开发机采样，不能替代目标硬件或长时稳定性。最低i5-8400/GTX1060 6GB/16GB、中画质1080p60FPS；推荐i5-12400/RTX3060 6GB/16GB、高画质1080p60FPS；Windows10另机、首次真人5–8分钟与趣味性均待外部验证。

原始证据：Evidence/Expansion/V01/Windows_Performance_D_performance/PlayerEvidence/performance_Result.json；汇总PerformanceSummary_A.json。所有失败轮、警告与采样前条件保留。
