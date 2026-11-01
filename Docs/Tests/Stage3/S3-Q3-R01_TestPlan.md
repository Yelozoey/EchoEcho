# Q3-R01：v2输出复现与开销分解

2026-10-27，依赖用户新授权及Qwen3TuningPlan.md。仅隔离EditMode诊断，不改正式Prompt/生产模型、S1/S2场景或玩家档；不下载/转换Qwen2.5。

固定原模型SHA3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303，v2 SHA5b47baccb0048d5ed970c74dfd5f69a62443fd155112788e3eb2b5ca89074bb3，18条Cases SHA186bec0153bad9f113bc925f034725e86a2c1cfc823d66b5469214343539bcff。prepare_qwen3_tuning.py已用官方HF本地非思考模板重新生成逐项核验IDs/正文，并把原文件字节复制为Baseline_V2_B，输入564–599 tokens，未改1536/256/2048预算。

独立Unity夹具Qwen3TuningTests：先按module_tests.py检查干净场景/Prefab、Console、普通Forward/HDR/SDR/D3D11，然后新标签运行。CPU2/inter1、不自旋、真实Owner创建/推理/释放；300ms间隔、请求20秒、冷30秒保持。

R01仅将每个float的有限值检查从小函数调用改为直接条件分支，完整dtype/shape/长度/逐值检查保持。分别记录native Run与检查时间，其他生成器/语法/解析不变。期望18条每个输出token（含EOS）与旧A完全相同、结构18/18、旧15/18语义稳定；资源输出集合/56空KV成对释放，主线程native0。该基线复现通过不表示模型达到17/18；旧失败夹具与报告保留，后续smoke18仍必须≥17，独立100仍必须≥90，不允许配置文件任意降低。

对照旧A的P50=10.338376/P95=15.531938秒；新结果用相同nearest-rank统计，记录差异，不将一次Editor结果当Profiler/Windows/目标硬件验收。性能目标仍P50≤5/P95≤10，最低i5-8400/GTX1060 6GB/16GB、中档1080p60及推荐i5-12400/RTX3060 6GB/16GB、高档1080p60均须实机，本机仅i7-12650H/RTX4060 Laptop/16GB开发诊断。
