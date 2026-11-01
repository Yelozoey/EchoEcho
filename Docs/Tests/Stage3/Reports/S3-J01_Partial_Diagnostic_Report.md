# S3-J01 原版诊断报告（历史失败保留，整体未验收）

2026-10-26后续：用户已批准Prompt v2，现已归档原版并开始独立复测。下文为批准前的实际结果，不覆盖或改写为通过；新的结果使用PromptV2目录和新标签。

2026-10-26。S3完整归档11/24；J01结构叶已通过，模型语义仍不足，未进入J02及游戏接线。停止依赖性开发等待用户对Prompt正文变更的选择，不把本报告标为整体通过。

环境：Unity6000.6.7f1、Windows x64、D3D11、1080p普通Forward内部HDR-SDR，i7-12650H/RTX4060Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中档1080p60、推荐i5-12400/RTX3060 6GB/16GB高档1080p60仍待实机，诊断不代替目标性能验收。

## 已通过结构叶

- a严格解析：Unity64/64（61用例+3契约），Python61/61。报告S3-J01a_StrictJson_TestReport.md。
- b1字节语法/解析回归81/81；b2采样协议30/30。两份独立报告已归档，替身未算模型质量。
- c实际模型：J01c_A为69/69、26.091秒，job `e995ffe5caf849b8991cc4687ae3e544`，六个真实请求JSON6/6，均EOS，Owner8722/主线程1，总50次Run；生成1.19–2.08秒。格式合规不能代表意图正确。

## 实际语义问题与对照

六输入中跟随、待命、警告正确；调查供电面板、身份提问、代开门请求错误为follow，3/6。原始输出和实际目标/线程/时延保留RealJson_*.json。

三种官方包装A/B/C离线golden对照及18条真实推理：测试2/2、44.140秒，job `e35560acd0a542cea54fd8f148493a1a`，仅代表诊断顺利执行；每种仍JSON6/6、语义3/6。生产默认未变，原固定正文未改。见Ablation_*.json、AblationGoldens.json、J01c_Ablation_A*。

已下载模型accuracy4、accuracy0、未量化FP32各六条首意图token数值探针：在固定相同前缀上三者top1全相同，包含三种错误输入的follow偏向。完整向量/前20/对照cosine与RMS见IntentAcc4_A、IntentAcc0_A、IntentFp32_A、IntentReferenceSummary.json。它们只是teacher-forced首token对照，**没有声称FP32完整六条生成或100条语义验收**。结果说明量化误差不是错误的唯一来源，不能仅切精度后直接交付。

## 待确认项

原规范要求固定Prompt直接使用，ExecutionProposal明确改正文先询问。已准备Docs/Stage3/PromptChangeProposal.md及PromptV2_Proposed.txt：增加意图映射、禁止默认follow/代操作、未知或歧义目标澄清、权限/事实约束和六类JSON示例。没有应用到生产，也没有用草案运行模型，改善幅度未测。

DraftAudit仅文本token核算：原版空上下文follow为173，新草案525；仍1536输入/256生成/2048上下文。当前正式原文SHA `78a7640483b9619502dcb9b7bb9db7a84bab2abf6115d5e51a5057792792bd5d`，草案SHA `5b47baccb0048d5ed970c74dfd5f69a62443fd155112788e3eb2b5ca89074bb3`。模型/接口/预算和100条≥90%意图、≥95%格式、安全零违规门槛不放宽。

用户确认前不改正文、不继续依赖的游戏集成。可选批准草案后先重测，或保留正文继续评估其他适合模型。S1/S2保护767/767未变；无在途Owner、无模型接入现有S2场景，未进入S4。关联F120/F125，其他编译/夹具历史保留。
