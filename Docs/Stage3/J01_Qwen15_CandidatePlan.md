# J01 Qwen2.5-1.5B候选隔离叶

2026-10-26。Qwen3实际JSON18/18、语义15/18未达预设17/18，已No-Go；见对应报告。S3仍11/24。按原授权的约0.5B–1.5B、量化ONNX、ORT CPU本地离线范围继续选型，不改已批准Prompt v2和最终质量/安全/时延预算，不进入J02或S4。

来源：官方Qwen/Qwen2.5-1.5B-Instruct revision989aa7980e4cf806f80c7fef2b1adb7bc71aa306，Apache-2.0；社区ONNX revision d54957e3af1be04546436e874a60fe386041b244。配置28层、12查询头、2KV头、head128、hidden1536。现成INT4约1.79GB超过1GiB目标，因此不采用该文件。

先隔离测试官方ORT1.31 DEFAULT/RTN对称INT4 QOperator，block64、accuracy_level4。源FP32图1,057,843字节、external6,208,411,648字节，均固定上游SHA。源图共享embedding与转置输出头；必须分别量化其Gather和MatMul布局，并剪掉不再引用的原float权重。目标197个MatMulNBits和1个GatherBlockQuantized，其他位置常量保留；lm_head前只取最后位置，KV保持完整。源opset14通过官方version_converter迁移21，不能只改版本号。最小toy前后原生对照通过后，才转换真实权重。

保守估算含32MiB源常量及10MiB分词/元数据约995.8MiB，只是预算估算；实际模型加10MiB必须≤1GiB，否则失败，不能裁词表或暗改模型来凑大小。需要改block128时另留变体/质量测试，不能沿用64测试结论。转换逐权重加载，避免把整个6.2GB源放入protobuf；转换工具峰值与正式AI推理RAM分别记录，不混算。

顺序门禁：

1. 下载工具独立14项测试、固定revision的小文件校验报告已通过；随后下载external单个预分配文件，最多4个Range/每块3次、最终LFS SHA，完整文件才重命名。
2. 输入叶：官方分词器/ChatTemplate，18条原固定用例正文不变、预算不变。与旧包六组分词规则解析全同且tokenizer.json字节SHA相同；仍不据此宣称新模型张量适配通过。
3. 转换叶：toy独立数值/形状/保留常量/外部文件/剪枝；再实际转换，记录来源、参数、节点和权重指纹、实际字节/SHA、工具峰值内存与20GiB磁盘预算。未通过不加载候选。
4. 实际Python参考叶：后台CPU2，64-token prefill+一步KV，完整logits/全部KV形状和有限值，绑定实际模型/receipt/官方文件；不是FP32等价或质量验收。首意图token分数只供诊断，不能代替完整JSON/目标。
5. Unity隔离叶：真实C#对Python参考logits≤1e-4，两步KV，再清KV跑原18条。正式同款生成器/JSON约束/严格解析，要求18/18结构和≥17/18意图/目标，冷30秒、请求20秒、300ms间隔、后台创建/执行/释放。全过程不派发游戏动作；保留失败，不以脚本修正混算。
6. 仅候选初筛通过，才逐叶更新A01–A04及受影响上下文/Owner回归、Windows及内存；最终100条质量、100条安全、压力/三结局/性能要求仍完整执行。

缓存清理仅移除23个经SHA/逐字节核验的任务自有重复分段和旧staging，回收1,891,229,429字节，保留生产包、源模型和全部历史证据。前后核验及PowerShell故障F131见CandidateQwen15证据。清理后当前S3约10.11GiB；下载前实际统计加6.2GB源及量化/安装预留约18.06GiB，必须继续实测，不以一次预测替代后续预算。

资料：[官方模型](https://huggingface.co/Qwen/Qwen2.5-1.5B-Instruct)、[ONNX转换发布](https://huggingface.co/onnx-community/Qwen2.5-1.5B-Instruct/tree/d54957e3af1be04546436e874a60fe386041b244/onnx)、[ORT量化](https://onnxruntime.ai/docs/performance/model-optimizations/quantization.html)。
