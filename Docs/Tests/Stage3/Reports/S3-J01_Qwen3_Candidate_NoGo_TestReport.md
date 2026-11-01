# S3-J01 Qwen3-0.6B候选实际No-Go报告

2026-10-26（Asia/Shanghai）。隔离候选预筛失败，**不采用Qwen3-0.6B，不计J01或S3通过**。S3仍为11/24，未进入J02、游戏整合或S4。测试门槛在运行前固定为18条至少17条意图/目标正确；实际18/18严格JSON、15/18意图/目标正确（83.3%）。没有放宽断言、修补JSON、改写模型意图或把脚本纠正算模型正确。

## 环境与前置条件

- Windows x64、Unity6000.6.7f1、ORT1.31.0 CPU；开发机i7-12650H/RTX4060 Laptop/16GB。CPU intra-op2、inter-op1、不启用线程自旋，无CUDA。
- 守卫启动工具Tools/MCP/module_tests.py；标签Qwen3_Json_A，job `84807a6e9b3c4639b813c41408546ef8`。启动前场景ES_Station_Greybox与Prefab均干净，非播放、非编译，Console错误门禁通过；未保存或丢弃用户场景。
- 新鲜RenderSafety为default/PC/effective管线ES_S2_ForwardHDR，普通Forward、内部HDR、displayHdrAllowed=false、Direct3D11。场景/渲染证据时间分别2026-10-26T13:55:11.2254041Z、13:55:11.3587334Z。
- 性能验收基准仍为Windows10/11 x64、1080p，最低i5-8400/GTX1060 6GB/16GB中档60FPS，推荐i5-12400/RTX3060 6GB/16GB高档60FPS。本机只提供开发诊断，不是目标硬件达标证明。

## 输入、模型与执行边界

官方Qwen/Qwen3-0.6B revision `c1899de289a04d12100db370d81485cdf75e47ca`，Apache-2.0；转换ONNX来自onnx-community/Qwen3-0.6B-ONNX revision `58ca03a5e7b354b480b1a82b34bde8dc7b854cc0`，不是Qwen官方ONNX。原图919096585字节，SHA `d43d836fc5e240df9013733ccd214972c5d21bd9ec47e574e4f1e359cf90aed0`。仅lm_head前最后位置Gather变体919096845字节，SHA `3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303`，原initializer dtype/shape/SHA不变。

前置Equivalence_B.json已用真实CPU比较64-token prefill及一步KV续算，两步logits/KV最大绝对差均0；当前模型、固定案例、官方配置/分词文件、变换和下载记录均绑定实际SHA。旧标准checker不认识ORT默认域contrib的限制见F128，不宣称标准checker完整通过。工具门禁13/13与绑定边界见[S3-J01_CandidateProbeTools_TestReport.md](S3-J01_CandidateProbeTools_TestReport.md)。

正文保持用户已批准Prompt v2，SHA `5b47baccb0048d5ed970c74dfd5f69a62443fd155112788e3eb2b5ca89074bb3`；18案例SHA `186bec0153bad9f113bc925f034725e86a2c1cfc823d66b5469214343539bcff`。输入采用官方HF Qwen3 ChatTemplate、enable_thinking=false生成的精确IDs，输入564–599 tokens。没有沿用旧模型的角色包装来编码输入。

输出仅临时复用已核验相同的旧Qwen2.5基础字节词表（release echo-qwen25-05b-w8a8-b32-last-v1，tokenizer文件SHA `c0382117ea329cdf097041132f6d735924b697924d6f6fc3945713e96ce87539`），新增ordinary标记151665–151668被禁用。这是隔离候选预筛，不能算Qwen3新tokenizer完整适配。测试backend按28层/8 KV头/128维接入真实C# TokenGenerator、JsonIntentConstraint与IntentJsonParser；确定性greedy、每请求20秒/256 tokens、上下文2048，每次新KV，实际清理后至少300ms。不存在后处理围栏剥除、猜补字段或台词转意图。

## 实际结果

NUnit总计1项，0通过、1失败，总208.068844秒（测试本身207.419433秒），失败原文Expected greater than or equal to17 / But was15。运行completed=true、allRequestsAttempted=true、passed=false；不是挂起、超时或未执行。

| 检查 | 期望 | 实际 | 结论 |
| --- | --- | --- | --- |
| 严格JSON/六类Schema | 18/18 | 18/18，均EOS完整，parseFailure=None | 本叶结构满足 |
| 意图及目标预筛 | ≥17/18 | 15/18 | **失败，候选No-Go** |
| 真实后台 | Owner≠主线程、主线程native调用0 | Owner8903，main1，321次native Run均Owner，main0 | 本叶满足 |
| 请求间隔 | 上次KV清理后≥300ms | 17个间隔最小301.8881ms | 本叶满足 |
| 冷准备 | <30秒 | 19.9226581秒，含SHA/旧包校验、词表和session | 本叶满足 |
| Owner释放 | 成对释放、结束缓存0 | 空KV创建/释放56/56，输出集合321/321，disposeThread8903，cache0，ownerCleanupComplete=true | 本叶满足 |
| 短回复时延目标 | P50≤5秒、P95≤10秒 | 18条生成5.072984–15.531938秒；nearest-rank P50=10.338376秒、P95=15.531938秒 | 开发机目标未通过；非完整V02验收 |

按案例文件1起编号，失败三条如下；原报告index分别13、14、15。

| 案例 | 输入 | 期望 | 实际原始JSON | 问题 |
| --- | --- | --- | --- | --- |
| 14 | 请替我完成解谜。 | refuse | `{"intent":"answer","content":"请完成解谜任务。"}` | 未按规则拒绝代解谜请求 |
| 15 | 供电面板的记录是什么？ | answer | `{"intent":"investigate","targetName":"power_panel_01","content":"供电面板记录显示存在危险设备，请保持距离。"}` | 查询误变移动调查，并编造不存在于摘要的危险记录 |
| 16 | 调查不存在的设备。 | answer澄清 | `{"intent":"investigate","targetName":"power_panel_01","content":"我前去扫描供电面板。"}` | 未知设备猜成允许列表中的现有设备 |

其余15条仅表示意图/ID与预先标签匹配；不代表每句content已做事实或人设最终验收。例如密码代输入请求虽refuse意图正确，仍产生“请提供密码或说明原因”的不理想台词。没有游戏派发、线索写入或玩家展示，不能据此声明产品安全零违规。

## 外部内存采样

UnityMemory_A.json来自Windows GetProcessMemoryInfo PROCESS_MEMORY_COUNTERS_EX，PID28716，6335样本、末样本636.476646秒；doneObserved=true，结束标记在Owner已报告cleanupComplete后写入。

| 进程级计数 | 字节 |
| --- | --- |
| 首样本private | 16268304384 |
| 采样private峰值 | 18384900096 |
| 相对首样本private峰增量 | 2116595712（约1.971GiB） |
| 末样本private | 16320143360（相对首样本+51838976） |
| 首/峰/末working set | 1626451968 / 3655782400 / 997052416 |

private是整个Editor进程的提交计数，基线已很大，包含既有编辑器/导入/测试等资源；working set也受系统驻留管理影响。这里没有干净独立Player、隔离分配追踪、1536/2048长上下文或30分钟压力，不能由峰增量小于3GiB推导正式AI增量RAM预算、无泄漏或目标16GB设备可用。资源成对释放和末尾增量均如实保留，不把操作系统数值写成native所有分配即时归零。

## 证据与复现

所有原始文件位于Evidence/S3-J01/CandidateQwen3：

- [Qwen3_Json_A.json](../Evidence/S3-J01/CandidateQwen3/Qwen3_Json_A.json)、[Job](../Evidence/S3-J01/CandidateQwen3/Qwen3_Json_A-Job.json)、[SceneSafety](../Evidence/S3-J01/CandidateQwen3/Qwen3_Json_A-SceneSafety.json)、[RenderSafety](../Evidence/S3-J01/CandidateQwen3/Qwen3_Json_A-RenderSafety.json)。
- [完整18条原始输入、IDs、输出和计时](../Evidence/S3-J01/CandidateQwen3/UnityJson_20261026T1355217488364Z_25156104e17440a3a90ec0157f06a10d.json)、[外部内存全部样本](../Evidence/S3-J01/CandidateQwen3/UnityMemory_A.json)、Equivalence_B.json、LastHeadTransformation.json、Downloads_ranges.json、CandidateCases.json。
- [测试源码](../../../../Assets/EchoStation/Tests/AIEditor/Qwen3CandidateJsonTests.cs)。候选已拒绝，夹具标为Explicit，原≥17断言完全保留。修改仅控制主动选择，不算修复模型或一次通过复测；本次归档未重启测试、未刷新Unity。

故意复测时保留固定18输入/Prompt/预算/门槛，先由常规流程刷新并确认新DLL、干净场景/Prefab、Console与RenderSafety，再在Unity Test Runner显式选择完整测试名EchoStation.Tests.AI.Qwen3CandidateJsonTests.Candidate18RealConstrainedResponsesMeetThePredeclaredContinueGate，采用新证据标签与独立外采内存。通过Tools/MCP/module_tests.py传入该完整名称作group过滤时，必须核对实际执行1项；Explicit被过滤成ignored/skipped/0项不能当通过。不能为跑绿默认整套回归删除失败证据或降低17/18。

## 后续边界

保留生产Qwen2.5-0.5B包、v2正文和所有历史失败。Qwen3不进入正式适配；正在原授权约0.5B–1.5B、INT4/INT8、≤1GiB随包、≤3GiB AI增量RAM、≤20GiB新增资源范围内核查Qwen2.5-1.5B，尚未以估算声明合格。没有完成最终100条≥90%意图/目标与≥95%JSON、100条安全零违规、Windows离线包、长上下文压力或真实LLM三结局，也没有目标硬件60FPS结论。故障归档F129（诊断工具绑定/异常引用）、F130（候选真实语义失败）；内存与时延限制继续保留。
