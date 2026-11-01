# Q3-R04c3 CPU4独立100条复核通过

2026-10-27，固定v5/同一独立B集/同一模型/官方非思考user/greedy，只将已验证ORT IntraOp CPU2改CPU4，inter1/无自旋/单BelowNormal Owner保持。依赖R04c1设置4项和c2真实18条完整token对照已通过归档。全程不复制模型/做大文件SHA校验，不调Prompt。

作业700a94e2b1684c4fb9e330fdfadde8a5实际1/1通过，450.641秒。结构100/100、原始意图及目标92/100、0超时/不完整；八个意图错误和已记录事实错误保持计错，不做脚本修改。与CPU2独立100条逐项比较输入和全部输出token（含EOS），100/100完全相同，Cases SHA同edf55f436da2ae1b25fb33ec2682fb347eb7508f616ed59e88a17ae6bee4b067。

冷准备10.603秒；nearest-rank生成P50/P95=4.254/5.260秒，当前开发机满足≤5/≤10目标。对照CPU2为6.927/8.412秒（该CPU2轮有并行文件校验，F141已说明）；额外18条干净对照也已通过。后台原生主线程调用0、Owner清理完整，30秒冷准备/20秒请求/300ms保持。

OS采样在测试前已启动：整个Editor峰值Private4,693,360,640B、WorkingSet3,398,217,728B、峰值增量2,154,737,664B，提供候选资源信号，不是隔离Windows AI≤3GiB或最低硬件证明。开发机i7-12650H/RTX4060 Laptop/16GB；i5-8400/GTX1060中档1080p60和i5-12400/RTX3060高档1080p60、推理并发真实游戏帧/GC仍在后续验证。

证据V5_HeldOut100_CPU4_A/的HeldOut_CPU4_A-Job/结果、Scene/RenderSafety、Summary、Memory、ThreadComparison、完整raw。原始UnityTuning_20261027T0109472758921Z_1e917887f5f247beae14050879630809.json，SHA68f3ef1bfad6d81acf677b07cfb3236d9a456bd857c20a849c5fb1046b8d414e。比较记录绑定CPU2原始SHA809023e9f79298a551389a820a8da325c5f92162e518c7071b08277348d0c5a2。

结论：R01–R04隔离候选调优门禁通过，选择v5+CPU4继续正式适配，资源账本R04_Complete为10.631GiB/20GiB。仍不是正式C#Qwen3分词/游戏整合或最终T01/T02验收；后续按Qwen3ProductionAdaptationPlan/Q3-A01_PackagePlan逐叶实施，事实台词必须经过可信守卫。S3总计仍11/24、J01尚未总通过，不进入S4。
