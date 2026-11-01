# S3-C01 摘要快照与真实上下文预算

2026-10-26，通过，S3第10/24模块。a先行14/14归档；b编译首错误A/B均保留（F123），修正后C01b_C真实tokenizer与回归23/23，2.575秒，job `0b22913edb134fbf943d4132936dca16`。证据Evidence/S3-C01。

环境Unity6000.6.7f1/Windows x64/D3D11/1080p普通Forward内部HDR-SDR，i7-12650H/RTX4060Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中档1080p60、推荐i5-12400/RTX3060 6GB/16GB高档1080p60未在数据模块验收。干净场景/Console/实际RenderSafety通过。

独立入口module_tests.py，assembly EchoStation.AI.Editor.Tests，groups `^EchoStation.Tests.AI.ContextBudgetTests`、`^EchoStation.Tests.AI.ContextSnapshotTests`。9项新增预算/脱敏、14项a回归全部通过。真实tokenizer为已A02对照上游400golden验证的Qwen byte-BPE，非字节或字数估算。

| 用例 | 期望及实际 |
| --- | --- |
| 无相关记忆 | 空/不相关记录不灌历史，0 entries/0 bytes/0 tokens，通过 |
| 数字密集8条 | 实际token超512时从末尾删完整低优先级条目；保留1–7条，token≤512、bytes≤1200；逐条末尾保留完整117数字，通过 |
| 中文密集 | 多条120字符中文摘要，最多8、实际UTF8≤1200/token≤512，保留摘要完整，通过 |
| 密码脱敏 | 普通提问/明确密码提问两组，标题/摘要/事件/状态/目标名均无7316；原记忆密码与摘要仍完整，通过 |
| 目标渲染 | 严格生成JSON，只有id/name/actions，换行/引号转义，role字串按普通文本编码不产生151644，通过 |
| 优先级衰减 | 旧低优先级事件排在新普通和关键证据之后，通过 |
| 新快照 | 后发现只进入新请求，旧上下文保持空，通过 |
| 坏Unicode | 进入Prompt/native前拒绝，通过 |

候选与跨区权限细节见a报告、S3-C01_ContextSnapshot.md。源记忆没有日志FullText字段；只有相关摘要进入MemoryText。Password值不送模型；最终明确询问的可信答案由后续J02/E01接S2规则，而非损坏或删除存储。目标名单最多8项且单独计真实token，完整输入1536上限由C02包装后核算，不能用此512记忆预算代替整段Prompt验收。

限制：可见性主线程采样、权限应用前复核尚待E01；固定Prompt与实际后台组装待C02；不是模型质量或游戏路线通过。原始格式F120仍待J01/T01，未进入S4。
