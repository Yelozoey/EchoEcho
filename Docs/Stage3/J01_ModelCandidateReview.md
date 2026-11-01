# J01 Prompt v2后候选模型复核

2026-10-26。v2模板17/17、解析/语法/真实结构95/95；原六类语义5/6，三种官方包装仍各5/6。预先固定扩展18条真实JSON18/18、意图/目标13/18（72.2%），V2_Expanded_A，job `d028975afaa44d609b865ca2bbfd275a`，85.835秒。错误包括代操作、将查询记录与未知设备误当调查现有目标，不能接入游戏后把守卫纠正算作模型正确。J01整体和S3未验收，不进入J02。

按现有连续授权，免费且允许分发的约0.5B–1.5B量化中文模型选型已委托，无须再次索取普通候选选择许可。先隔离评估Qwen3-0.6B，保留现有生产Qwen2.5-0.5B和全部证据，不直接替换。正文保持已批准v2，使用候选的官方非思考ChatTemplate（enable_thinking=false），不把推理链展示给玩家，依然只接收六意图JSON；保持离线ORT CPU、实际后台、2048/1536/256、20秒/300ms、1GiB随包/3GiB AI RAM/20GiB新增资源预算与≥90%/≥95%/零违规门槛。

依据[官方模型卡](https://huggingface.co/Qwen/Qwen3-0.6B)：Apache-2.0，约0.6B、28层、16查询头/8 KV头。[ONNX转换发布者文件列表](https://huggingface.co/onnx-community/Qwen3-0.6B-ONNX/tree/main/onnx)列出约919MB的model_q4.onnx；转换版不是Qwen官方ONNX发布，需独立来源/SHA/实际节点/数值/语义/Unity与Windows验证。它只说明候选文件可能符合大小预算，不代表内存、兼容性或质量合格。另审查Qwen2.5-1.5B，其现成INT4 ONNX约1.79GB，超1GiB随包目标，未下载或擅自放宽预算。[发布列表](https://huggingface.co/onnx-community/Qwen2.5-1.5B-Instruct/tree/main/onnx)

下载仅固定revision的元数据、tokenizer/配置/许可及必要权重，缓存Library/EchoStation/S3Cache/models/qwen3-candidate，证据CandidateQwen3；无trust_remote_code、无全机依赖或网络设置改动。每批权重前计算现有S3资源和计划新增大小，超20GiB中止；HTTP最多三次正确Range续传，大小与LFS SHA验证后才完成重命名。模型失败/不达门槛如实留证；超出已批准正文或硬约束的改变继续询问。

后续先做候选小规模真实输出和参考数值诊断，通过才逐叶适配tokenizer/张量并重跑受影响已验收模块；不能因新模型能加载就跳过模块门禁，不能把teacher-forced首token当完整语义验收。

## 候选叶与预先固定的检查

1. 来源/输入叶：固定官方与转换版revision；HTTP大小/LFS SHA匹配；官方非思考ChatTemplate；基础词表比较；18组输入均≤1536，原六组任务正文与Unity实际准备结果逐字相同。已生成CandidateCases.json，不运行模型即不声称语义通过。候选基础词表相同，新增标记26对22、NFC一致；不能据此跳过新分词器400组golden回归。
2. 数值优化叶：仅在lm_head前插入最后位置Gather，全部原权重dtype/shape/SHA不变；64token prefill与一步KV续算比较，logits最大绝对差预设≤1e-4、KV最大差0。失败保留，不能临时扩大容差后称等价；失败时回到原图评估或明确另提有证据的优化。数值叶未通过不运行该变体的生成。
3. 输出诊断叶：同一固定18输入、真实CPU2线程后台、单请求20秒/最多256tokens/300ms；记录原始greedy，不剥围栏/猜补JSON。原始格式和意图分开，teacher-forced首token仅为诊断。之后再用正式同款JSON约束与严格解析做真实生成，至少17/18意图/ID正确才作为继续适配的初步信号；这不是替代最终100条验收。任何设备代操作承诺、未知目标选现有设备的错误必须单列，不能接游戏后隐瞒。
4. 后续适配与回归叶：确认候选后，A01/A02/A03/A04及受影响Owner/上下文/JSON模块各自独立回归；真实Unity/Win64/并行性能与≤3GiB内存通过后才能替换正式路径。现有S1/S2资产保护与模型0.5B完整记录保留；不以纯Python诊断替代Unity/Player证明。

首次小文件下载一次TLS握手超时，复用F114有限重试/校验后成功，无TLS设置改动。大文件改用最多4个受限Range，复用已有完整前缀，逐段核对Content-Range与长度、合并后整文件SHA；截断保留有效字节重试，错误存RangeError_*.json。下载缓存与分段均计入20GiB峰值预算。

## Qwen3候选实际结论：No-Go

2026-10-26，守卫启动的Unity EditMode诊断Qwen3_Json_A，job `84807a6e9b3c4639b813c41408546ef8`，总208.068844秒，0/1失败。18条均严格JSON并正常结束，但意图/目标仅15/18（83.3%），低于事先固定的17/18。第14条代为解谜误为answer，第15条资料查询与第16条不存在设备误派investigate/power_panel_01；第15条还编造危险记录。所有原始输出留存，未接入游戏执行或展示，不用后续守卫补救混算正确率。[独立实际报告](../Tests/Stage3/Reports/S3-J01_Qwen3_Candidate_NoGo_TestReport.md)。

实际Owner8903、Unity主线程1、主线程native调用0；321个输出集合/56个空KV均由Owner成对释放、最终缓存0、cleanupComplete=true。冷准备19.9226581秒；18条生成5.073–15.532秒，开发机短回复时延目标未通过。外采Editor进程6335样本，private峰增量2116595712字节约1.971GiB；基线private已达16268304384字节且包含整个Editor，不能用这一增量宣称隔离AI分配、长输入内存或正式Windows预算通过。

此叶只消费官方Qwen3非思考输入IDs，输出临时复用相同的Qwen2.5基础字节词表并禁用新增4个ordinary标记；尚未完成新tokenizer适配、Windows、100条质量/安全、长上下文或游戏整合。测试保留原17/18断言并标为Explicit候选诊断，默认回归跳过不算该候选通过。生产0.5B包、v2正文及原门槛不变；J01仍未通过、S3仍11/24。

下一步仅在原0.5B–1.5B、INT4/INT8、≤1GiB随包/≤3GiB AI RAM/≤20GiB新增资源预算内核查Qwen2.5-1.5B路线。现成约1.79GB Q4不能直接采用；需先证明更完整权重量化、临时磁盘峰值与来源可行，再独立实测，不以尺寸估算代替兼容性或质量验收。
