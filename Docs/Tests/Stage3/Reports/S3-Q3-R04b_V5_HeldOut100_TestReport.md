# Q3-R04b v5 独立100条意图门禁通过

2026-10-27，固定v5正文SHAf3524a85ad232bd4077da4237280e778d5b6cc3bc0fcc95d8c069480973bcbe1，官方非思考user包装、INT4 Qwen3-0.6B、ORT1.31 CPU2/inter1/无自旋、单后台Owner。Unity6000.6.7f1、D3D11、普通Forward/内部HDR/SDR；开发机i7-12650H/RTX4060 Laptop/16GB，非最低或推荐硬件验收。

R04a1输入8项、a2真实完整目标范围6项、a3失败延迟4项已独立通过留档后整合。B集100条完整新句子/五个新夹具目标、标签不入模型或语法，原始SHA48ce057de9fecbe441d18b56cba8513ef4754e331043938dd226eacac209a530；实际官方输入760–767tokens、Cases SHAedf55f436da2ae1b25fb33ec2682fb347eb7508f616ed59e88a17ae6bee4b067。没有依据输出调v5，100条均完整生成且0超时/不完整。

新鲜安全/Console门禁通过；作业e35c5f0f20284807bd799c73f39e48ac，NUnit实际1/1通过，714.717秒。原始严格JSON100/100、意图及目标92/100，达到≥95%格式及≥90%意图门槛；当前runner格式要求100%，没有降低。分项：Follow15/15、Wait14/15、Investigate19/20、Warn7/10、Answer24/25、Refuse13/15。

八项误判完整保留：停留误answer、一个明确ID调查误answer、三项危险提醒误answer、继电器资料提问误investigate、两项代解谜误answer。未以脚本修正混算。另人工查看台词：#70混合设备记录，#72位置臆断，#73把裸露电缆风险说成高温，#80/81遗漏已知危险，#84臆测事故关联；这些不是已通过的事实质量，必须由后续J02可信事实/密码守卫处理、T02独立验证。任何模型台词未经守卫都不派发/展示，当前没有接入游戏执行。

冷准备11.069秒；nearest-rank生成P50/P95=6.927/8.412秒，全部100条计入、无失败惩罚样本。期间有一次S1/S2大文件SHA并行校验（F141），因此延迟作为诊断，不能把单次差异单独归因模型；P50仍超≤5目标。下一叶固定Prompt做独立CPU2/4性能对照，不据B预测继续调v5。

Owner97创建/执行/释放，disposeThread97、mainThreadNativeCalls0；56空KV、1789输出集合均成对释放，全部请求清KV，最终completed/ownerCleanupComplete为true。OS整个Editor峰值Private4,646,707,200B、WorkingSet3,333,812,224B、采样峰值增量2,199,384,064B，不等同孤立AI增量或Windows最终3GiB证明。20秒请求/30秒冷准备/300ms冷却保持。

证据 `Evidence/S3-J01/CandidateQwen3/Tuning/V5_HeldOut100_A/`：Run/Prompt/Cases、HeldOut_A-Job/HeldOut_A.json、Scene/RenderSafety、Memory、Summary及原始UnityTuning_20261027T0049190787214Z_2794bf98a93b4c429d759acd08354ab4.json，原始SHA809023e9f79298a551389a820a8da325c5f92162e518c7071b08277348d0c5a2。

结论为隔离候选的独立意图/格式通过；仍使用官方HF输入IDs、共享旧普通输出词表且禁四个Qwen3新标记，正式C# Qwen3 tokenizer/后端、J01总复核、权限、游戏与Windows尚未完成。S3仍11/24，不把本叶冒充最终T01或S3交付。目标硬件1080p60FPS与完整场景推理并发性能均待后续实际验收。
