# Q3-R03 v4 六类示例（不通过）

2026-10-27。环境/硬件/边界沿用R03 v3报告和R01计划；Unity6000.6.7f1、ORT1.31 CPU2/inter1、开发机i7-12650H/16GB，目标硬件与实际游戏性能验收未执行。正式Prompt和模型、S1/S2不变。

v4用六类完整示例加两个边界对照，user包装输入517–552tokens；固定同一18条预期、JSON语法与解析器、256输出/20秒/300ms/30秒冷准备。module_tests.py新鲜SceneSafety与RenderSafety通过，作业258734705e0a4f7c91a1316cf87ba29c实际0/1失败，293.758秒。

结构18/18，意图/目标14/18，低于17/18，不采用。两条代开门误follow、代解谜及不存在目标误investigate。没有游戏派发或脚本纠错。Owner创建/执行/释放，主线程native0，原始completed/ownerCleanupComplete为true。冷27.446秒，生成P50/P95=13.824/19.730秒，性能也未过；与前轮不同正文且Editor长期运行，不能把延迟变化单独归因Prompt。

证据 `Evidence/S3-J01/CandidateQwen3/Tuning/V4_Balanced_A/`：Run/Prompt/Cases、V4_A-Job、V4_A.json、Scene/RenderSafety、Summary、原始UnityTuning_20261027T0027086445065Z_73ca6cab1826440886651cbab22fa398.json（SHA972d5d5268befe5ee719fa885893982b1afa4d71775e886a1f40b4ed482a15f9）。汇总工具第一次在Owner仍工作时正确拒绝，之后排空才归档；未放宽门禁。

下一轮回到旧v2，只插入三个边界示例，保存为v5。先按既有授权正常重启干净Editor、保持D3D11；若要归因重启性能，还需相同输入版本对照，不能用v5单次结果证明重启带来某倍提速。故障F139另记。
