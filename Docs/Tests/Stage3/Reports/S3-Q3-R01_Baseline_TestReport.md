# Q3-R01 v2复现与实际开销

2026-10-27。Baseline_B job0a07a27990c14410bc217452058057ea，Unity6000.6.7f1/ORT1.31 CPU2、inter1、后台Owner8960/main1。module_tests.py新鲜干净场景/Prefab、Console、Forward内部HDR/SDR/D3D11门禁通过。开发机i7-12650H/RTX4060 Laptop/16GB，仅开发诊断；目标最低/推荐1080p60仍未验收。

独立测试1/1通过，205.7425566秒，**仅证明v2基线完整复现和资源门禁**。18条每个输出token（含EOS）与旧A完全相同；结构18/18、语义15/18。模型质量仍不合格，原≥17/18门槛不改，S3仍11/24，未进入J02。夹具明确baseline_v2模式，不把15/18的复现Passed当模型Passed。

| 项目 | 实际 |
| --- | --- |
| 冷准备 | 19.4324576秒 |
| 18条nearest-rank P50/P95 | 10.2553075 / 15.1695237秒 |
| session.Run累计 | 91.9569107秒 |
| 全部输出校验累计 | 87.2125259秒 |
| 主线程native | 0 |
| Owner清理 | ownerCleanupComplete=true |

旧A的P50/P95为10.338376/15.531938秒；仅直接有限值分支没有明显改善，仍未达P50≤5/P95≤10。校验与native计算几乎相当，不能把全部响应时间归因模型计算。dtype/shape/长度/所有float有限性仍逐值保留；不减少KV检查数量。

外采Memory.json在Owner清理确认后done：maxPrivate18427781120、maxWorkingSet3903709184、相对首样本private峰增量2112753664字节。整个Editor基线很大，不能据此宣称独立AI≤3GiB、无泄漏或16GB目标机达标。S1/S2保护767/767未变；实际S3资源10.61165GiB，未碰2.5下载/转换。

证据：CandidateQwen3/Tuning/Baseline_V2_B的Run.json、Prompt.txt、Cases.json、Baseline_B.json/Job/SceneSafety/RenderSafety、Memory.json，以及Tuning/UnityTuning_20261026T1610511872119Z_4d76a308af904fa7bd4bc46c6ab31fe8.json。输入官方HF本地重编码逐项相同，旧Cases SHA186bec0153bad9f113bc925f034725e86a2c1cfc823d66b5469214343539bcff、v2 SHA5b47baccb0048d5ed970c74dfd5f69a62443fd155112788e3eb2b5ca89074bb3。

下一子叶R01b：先用IEEE754类别/随机位模式/切片边界验证位模式有限性扫描与框架定义完全一致，再接入隔离runner复测真实18条；测试通过前不使用该优化，不减少安全检查。它不改变Prompt或语义策略。
