# S3-A04a 生成协议叶模块测试报告

2026-10-26。前置A03归档。**9/9协议测试通过**，job `1e792addc62249798b9f32c5cf447d67`，3.060秒；可进入A04b真实完整生成，A04尚未完成。

Unity6000.6.7f1/Windows11 x64/D3D11/1080p/普通Forward内部HDR-SDR；开发机i7-12650H/RTX4060Laptop/16GB，目标最低i5-8400/GTX1060 6GB/16GB中档1080p60、推荐i5-12400/RTX3060 6GB/16GB高档1080p60不在本叶验证。

TokenGenerator接收纯数据ITokenModel与真实tokenizer，greedy平分取最小ID、仅定义词表ID、EOS151643/151645、其他特殊角色标记不自动输出；256token含EOS、4KiB/20秒上限、取消检查、所有实际生成出口finally清缓存。达到上限明确未完成，严格UTF8失败不以替换字符冒充合法台词。约束接口只作用真实logits选择，不修补字符串；JSON grammar留J01独立实现。

独立9项覆盖：EOS文本与两次reset；token上限未完成；平分greedy；预取消/原生阶段后取消无结果；恰20秒超时/reset；不完整UTF8失败；4KiB拒绝；无合法候选失败且不修补；非法/NaN预算不触模型。此组用明确标注的确定性SequenceModel验证控制协议，没有读取LLM权重、不计为真实推理或模型质量成功。场景/Console/RenderSafety门禁均通过，数据测试无Unity对象访问。下一叶真实后端/实际权重/tokenizer跑完整EOS和原始输出，对脚本与真实推理分别留证。
