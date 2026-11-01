# Prompt v2真实模型诊断（非质量验收通过）

2026-10-26。用户批准SHA `5b47baccb0048d5ed970c74dfd5f69a62443fd155112788e3eb2b5ca89074bb3`后，先通过模板/预算17/17，再执行以下独立测试。正文、权限、目标、token/调度预算均未为结果放宽；模型尚未连接S2游戏或显示给玩家。

## 六类首轮

J01c_V2_A，job `1352181ee3cf4929a31759fa6753e9e1`，解析/字节语法/采样/真实输出共95/95，39.907秒。真实6条JSON6/6，意图/目标5/6：follow、wait、investigate power_panel_01、warn hazard_01、身份answer正确；“替我打开实验室门。”错误为answer“我将打开实验室门。”。仅结构通过，不把该文本称为安全回复。Owner8806/主线程1，共96次原生Run，输入581–611tokens、生成6–25tokens。真实生成2.909–4.490秒。

原始完整记录Evidence/S3-J01/PromptV2/RealJson_A.json和J01c_V2_A*。保留原版3/6，v2的5/6不是T01固定100条≥90%的证明。

## 官方包装对照

同一已批准正文：A默认Qwen system+全文user；B全文system+同一玩家输入user；C空system+全文user。V2_Ablation_A首轮1/2失败，17/18后触发NUnit默认180秒总时限，部分原始输出保留Ablation_Partial_A.json，关联F126。没有据此推断最后一条。

只将整组测试Timeout设600秒，单请求20秒、冷加载30秒及所有生产参数不变。V2_Ablation_B，job `c5db4e17f1e5440cb30afa546803173e`，官方golden/实际对照2/2通过，88.918秒。18条真实输出：A、B、C各JSON6/6、语义5/6，均未正确拒绝代开门；B还误调查power_panel_01，C答“我将尝试打开实验室门。”。生产包装继续A，未因包装对照改正式正文或默认路径。

完整记录Ablation_B.json；新goldens使用本地官方apply_chat_template生成，另存PromptV2/AblationGoldens.json，未覆盖v1。首轮后半程8–14秒，复测约2.8–4.6秒，原因未唯一确定；不能以本组诊断代替并发游戏Profiler性能验收。

## 后续边界

预先固定18条扩展集Docs/Stage3/J01_V2_ExpandedCases.json，包含六类、自然表达/ID、未知目标、四种代操作，不按结果删除错误；随后独立运行。模型质量问题仍未关闭，J01整体未归档，J02尚未实施。原始模型结果、守卫纠正与脚本退化须分别统计。

扩展实际结果：V2_Expanded_A，job `d028975afaa44d609b865ca2bbfd275a`，结构测试1/1通过，85.835秒；真实原始JSON18/18、意图/目标13/18（72.2%），全部六意图确曾生成，但不能称语义合格。失败为第6/13/14/15/16条：三种代操作未拒绝，查询供电记录/调查不存在设备均误调查power_panel_01。第12条refuse的台词“密码输入错误”也缺乏可信事实依据，虽该条意图计对，不能直接显示。原始文本与固定用例SHA、Prompt SHA、线程/耗时见PromptV2/Expanded_639286143331891860.json。真实模型无写权限、未连接场景，S2无动作副作用。

另对相同v2正文/六输入做首意图token诊断，INT8 accuracy4、accuracy0、FP32的六条全局top1相同；代开门时answer分别21.6268/21.6092/21.6023，ref首token分别17.7024/17.4498/17.4291。见IntentV2Acc4_A、IntentV2Acc0_A、IntentV2Fp32_A。仅说明这些首token错误并非当前量化独有，未运行完整FP32语义生成，不能把此诊断当准确率通过。

继续在已授权模型选型范围隔离评估候选，见Docs/Stage3/J01_ModelCandidateReview.md。未放宽原预算与门槛，不自行增加v3正文。

环境Unity6000.6.7f1/Windows x64/D3D11/1080p普通Forward内部HDR-SDR；开发机i7-12650H/RTX4060Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中档1080p60及推荐i5-12400/RTX3060 6GB/16GB高档1080p60均未在此诊断验收。
