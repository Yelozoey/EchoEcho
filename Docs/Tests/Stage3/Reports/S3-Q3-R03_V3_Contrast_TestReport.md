# Q3-R03 v3 对照规则试验（不通过）

2026-10-27，Unity6000.6.7f1 / ORT1.31 CPU2，开发机 i7-12650H、RTX4060 Laptop、16GB；普通Forward、内部HDR/SDR、D3D11。Windows10/11 x64、1080p；最低 i5-8400/GTX1060 6GB/16GB 中画质60FPS、推荐 i5-12400/RTX3060 6GB/16GB 高画质60FPS 仍须目标硬件测试，本试验不能替代。

依赖输入准备报告 Q3-R02；同一18条输入、预期意图与目标保持不变。仅使用隔离 Q3_V3_Contrast.txt、官方非思考 user 包装，greedy、INT4 Qwen3-0.6B、20秒请求/30秒冷准备/300ms冷却、1536输入/256输出等门槛不变。

V3_Contrast_A 在旧夹具校验未使用旧权重时冷超时，0条生成，不评价Prompt。F137修复后实际 V3_Contrast_B 冷准备11.864秒；完整实际18条结构18/18、意图/目标11/18，低于17/18。原地待命误answer、三项代操作误answer、代解谜与不存在目标误investigate，ID扫描误answer。部分content含多余右花括号或与行动不符；格式通过不等于台词或权限通过。不得采用，也未改正式Prompt或派发游戏动作。

作业6f824cfb55af450691a9fa84fb71cef2为0/1失败：15.231秒时 Unity IAP 服务TokenExchange联网错误中断NUnit。后台Owner继续排空，最终原始报告completed=true、ownerCleanupComplete=true、mainThreadNativeCalls=0；因此18条可作为原始诊断数据，不能把NUnit记为通过。见F138。保存错误后清空Console，以新标签另测包装；没有忽略错误或改变网络设置。

真实输出 P50/P95=5.338/8.951秒，仅开发机该组生成延迟；P50仍不满足≤5秒。OS内存采样晚于启动，峰值Private18,444,394,496B、WorkingSet2,628,366,336B、采样增量516,689,920B，都是整个Editor口径，不能证明AI增量≤3GiB。已验证Owner清理后结束采样。

证据：`Evidence/S3-J01/CandidateQwen3/Tuning/V3_Contrast_B/` 内 Run/Prompt/Cases、V3_B-Job、V3_B.json、SceneSafety/RenderSafety、Memory、Summary及原始UnityTuning JSON。Summary绑定原始SHA2af27d8c9beb2b23f33c8eba8c4c7932a26549a6288f6ab4d1464f1b8e91341c。先前A与旧v2基线均保留。下一试验仅比较同一v3正文的system包装，不降低质量门槛。

system 包装复测 V3_System_A：作业91bfe39f836e41b099dd90a423537dd8，213.071秒，0/1因13/18低于17而失败。真实结构18/18，语义13/18；冷10.757秒，P50/P95=9.563/17.472秒。Owner清理完成、主线程native0。证据同级 V3_System_A/，原始SHA56eccffe4728f97f0d5b27ebccf1b618b4e7c6fd0111774a492c82e4d2e52998。没有IAP中断，证明仅调整包装不足以解决问题。下一版 v4 恢复六类结构示例、保留问题/未知目标对照，先使用原user包装；不采用v3。
