# S3 模块验收索引

2026-10-26。用户已授权24模块连续实施，逐模块独立测试/归档后再进入依赖模块。当前11/24已归档通过，G01、A01–A04、I01–I04及C01/C02完成。J01原版语义3/6，已批准Prompt v2的固定18条结构18/18、语义13/18；隔离Qwen3候选随后结构18/18、语义15/18，低于预设17/18，候选No-Go（F130）。正在原授权预算内核查1.5B路线，不计模块或阶段验收通过。

| 模块 | 状态 | 证据/计划 |
| --- | --- | --- |
| S3-G01 | 兼容工作流12项、守卫Unity测试5/5通过 | [实际报告](Reports/S3-G01_Compatibility_TestReport.md)、Evidence/S3-G01 |
| S3-A01 | 20/20及受影响S2 JSON12/12通过 | [实际报告](Reports/S3-A01_ModelPackage_TestReport.md) |
| S3-A02 | a先行8/8，b/回归16/16与400完整IDs一致 | [a报告](Reports/S3-A02a_PreTokenizer_TestReport.md)、[整体报告](Reports/S3-A02_Tokenizer_TestReport.md) |
| S3-A03 | a先行8/8；正式后端/回归10/10、20次真实Run、5轮释放 | [a报告](Reports/S3-A03a_TensorContract_TestReport.md)、[整体报告](Reports/S3-A03_OnnxContract_TestReport.md) |
| S3-A04 | 协议9/9；真实/回归10/10。raw格式0/3单列待修 | [a报告](Reports/S3-A04a_GenerationProtocol_TestReport.md)、[整体报告](Reports/S3-A04_Generation_TestReport.md) |
| S3-I01 | a协议8/8；真实服务/回归9/9，实际原生运行中取消释放 | [a报告](Reports/S3-I01a_OwnerProtocol_TestReport.md)、[整体报告](Reports/S3-I01_Owner_TestReport.md) |
| S3-I02 | 协议5/5；真实模型/回归6/6，实际间隔393.1731ms | [整体报告](Reports/S3-I02_Scheduling_TestReport.md) |
| S3-I03 | a17/17、b1为25/25；真实ORT/回归9/9，原生运行中取消/超时后安全复用 | [整体报告](Reports/S3-I03_DeadlineCancellation_TestReport.md) |
| S3-I04 | a11/11、b1为26/26；真实B8/8、C1/1，迟到结果/单实例/一次领取通过，首失败F121保留 | [整体报告](Reports/S3-I04_SessionLifecycle_TestReport.md) |
| S3-C01 | a14/14；真实token预算/回归23/23 | [整体报告](Reports/S3-C01_ContextBudget_TestReport.md) |
| S3-C02 | a复测16/16；实际后台准备/回归19/19，原始格式仍失败F120 | [整体报告](Reports/S3-C02_BackgroundPrompt_TestReport.md) |
| S3-J01 | 未验收：v2模板17/17、结构回归95/95；0.5B扩展语义13/18；Qwen3候选结构18/18、语义15/18，预设门槛测试0/1失败，No-Go | [v2诊断](Reports/S3-J01c_PromptV2_Model_Diagnostic.md)、[Qwen3实际No-Go](Reports/S3-J01_Qwen3_Candidate_NoGo_TestReport.md)、[候选审查](../../Stage3/J01_ModelCandidateReview.md) |
| S3-J02至S3-V03 | 未开始 | [模块拆分](../../Stage3/ModuleBreakdown.md) |

G01当前选择路线为ORT CPU，前置Barracuda失败见[BarracudaNoGo](../../Stage3/BarracudaNoGo.md)。INT4/动态INT8首候选及内存/Profiler失败均保留，不把脚本NPC或有意失败测试当LLM质量通过。正式JSON/意图质量、完整请求时延、三路线/会话压力与独立交付将在相应模块验证。

硬件基准沿用Windows10/11 x64、D3D11、1080p、最低i5-8400/GTX1060 6GB/16GB中档60FPS，推荐i5-12400/RTX3060 6GB/16GB高档60FPS。当前i7-12650H/RTX4060Laptop/16GB只提供开发机证据；目标实机验收、真实断网条件不能以静态检查替代。
