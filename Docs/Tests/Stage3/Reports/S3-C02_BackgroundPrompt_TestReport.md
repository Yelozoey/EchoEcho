# S3-C02 原样Prompt与后台准备实际报告

2026-10-26，通过，S3第11/24。a首轮15/16（交付LF问题F124保留），复测16/16归档后才接Owner；b真实模型/相关协议19/19，27.944秒，job `c97a99c48ff04a39b82029fe3ca4841d`，C02b_A。

环境Unity6000.6.7f1/Windows x64/D3D11/1080p普通Forward内部HDR-SDR、i7-12650H/RTX4060Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中档1080p60、推荐i5-12400/RTX3060 6GB/16GB高档1080p60未实机验收。干净场景/Console/实际RenderSafety通过。

入口module_tests.py，assembly EchoStation.AI.Editor.Tests，groups RealPreparationTests、PromptPreparationTests、SessionHostTests、OwnerCancellationTests。实际证据Evidence/S3-C02/RealPreparation_*.json及C02b_A回执。a的逐字/SHA/10组上游golden/1536预算见独立a报告。

真实请求：主线程1，Owner/准备线程均8639，准备0.003405秒，完整输入242 tokens，实际生成2.203秒；Body无密码7316、无快照之后新增摘要，相关摘要与1项有效目标存在。坏Unicode摘要返回GenerationFailed/Generation=null、native调用增量0、无法Claim；同Owner下一正常请求实际EOS成功。共38次Run，最终释放。

TokenRequest可携带不可变InferenceSnapshot，检索/脱敏/原样Prompt/tokenization在Owner内完成，主线程只捕获数据和投递。生成前扣除准备耗时，20秒逻辑预算覆盖准备与生成；错误上下文是可恢复GenerationFault.InvalidContext，不损坏/销毁健康原生Session。PreparedPrompt随结果返回只读实际正文、IDs及有效目标；现有ID测试路径仍可用，宿主与取消回归通过。

**本模块不算JSON或语义质量通过。** 两次真实raw均Markdown围栏加单content字段，没有合法intent，F120仍存在，且不能由文字“恢复成功”推断其跟随语义正确。这里只证明真实后台处理/可恢复输入拒绝/资源与预算；J01将实现严格解析和已授权的真实logits结构约束，T01再核对100条独立质量门槛。未接游戏执行、未进入S4。
