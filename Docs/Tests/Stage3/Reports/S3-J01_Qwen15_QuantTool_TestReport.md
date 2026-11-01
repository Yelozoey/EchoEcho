# J01 Qwen1.5量化工具独立叶

2026-10-26，本机Windows，项目Python3.13.12、onnx1.23.2、ORT1.31.0。只用微型权重及真实1MB descriptor，不下载/读取真实external、不改Unity/生产模型。实现Tools/Stage3/quantize_qwen15_candidate.py，CLI转换真实权重尚未执行。

先声明toy量化门槛maxAbs≤0.15、cosine≥0.98，未量化改图要求dyadic数值最大差0、KV完全相同。这些只约束toy，不能把0.15直接借作真实模型或C#/Python参考容差。

实际root运行unittest discover -s Tools/Stage3 -p test_qwen15_quant.py -v，**5/5通过，0.953秒**，QuantToyTests_A.log；两工具py_compile退出0。原生创建/执行位于专用后台，CPU2、inter-op1、不自旋，prefill与KV续步均实际运行。

| 用例 | 实际 |
| --- | --- |
| 官方14→21迁移、最后位置Gather和共享头常量化，未量化两步比较 | logits最大差0，KV全等；ReduceMean.axes改为输入 |
| 官方RTN对称INT4 QOperator，核心/embedding/head量化 | 两步最大差0.003854863/0.004151195；cos0.999962032/0.999973059，通过预声明toy门槛 |
| external按需加载、剪除大float原权重、保留其他float常量 | checker与实际CPU两步通过，无external残留；非embedding Gather不量化 |
| 额外头引用或重复改图 | 拒绝 |
| 未授权block/accuracy选项 | 拒绝 |

早期toy3/5失败（标准typed int64_data被误要求raw_data）和未覆盖真实opset风险的不足均保留F132说明。修复支持标准typed字段，并加入opset14 ReduceMean/Unsqueeze/external float回归后再测试。首失败没有独立磁盘log，不编造日志；最终root完整日志已保存。

实际DescriptorMigration_A.json由inspect_qwen15_graph.py产生：固定源SHA正确，**2433个原initializer descriptor保持相同、57个ReduceMean完成迁移**，原embedding与lazy头没有载入external。只做metadata迁移，不声称已全图checker/原生加载。

真实转换CLI限制固定源图/external SHA和size、仅候选cache与测试证据路径、新输出不覆盖、actual20GiB资源门禁、实际模型+10MiB≤1GiB；逐目标加载external，使用官方MatMul/Gather量化器，197个MatMulNBits+1个GatherBlockQuantized，位置常量保留，原大float embedding/head必须剪除，KV输出descriptor不变。actual receipt须completed=true并记录实际hash/bytes/参数/节点。最终量化兼容、语言质量、工具/运行内存、20秒、Windows仍全部未验收；本叶通过仅允许处理真实权重，不进入游戏整合。

2026-10-26最终内存复查与修复（F134）：原5/5并未覆盖protobuf arena的生命周期。真实转换前，仅纯内存探针已证实upb在同一ModelProto上clear字节并不回收源数据；因此旧实现不能称逐目标卸载能限制工具峰值。已改为每目标独立临时GraphProto，官方量化后只复制Q权重/节点，函数结束销毁整块float arena；必需非量化常量在清理后加载一次，不再把大float源赋给长期目标图。

新增第6项为独立子进程、单一32MiB合成external文件复用六次的实际量化内存回归。原arena控制组增长201,748,480字节，新隔离目标图结束点private增量分别7,286,784 /12,697,600 /16,891,904 /21,639,168 /26,378,240 /31,105,024字节，包含12个累计Q initializer；满足预声明新路径≤134,217,728字节、旧路径≥167,772,160字节。没有真实权重或云调用。

最终新日志[QuantToyTests_B.log](../Evidence/S3-J01/CandidateQwen15/QuantToyTests_B.log)：**6/6通过，3.132秒，exit0**；原A历史日志保留。原有两步数值、external/保留float、opset14迁移及错误拒绝用例全部仍通过；两工具py_compile exit0。实际小图只读补查无零元素initializer，197个MatMul和1个Gather目标全部FP32，保护常量约34,153,572字节；没有载入真实external或运行真实图。

当前叶结论仅允许预算内真实转换spike。独立arena消除了随已处理float总量线性积累的问题，不承诺933MB大矩阵量化、checker/序列化的瞬时工具RAM，也不证明模型运行≤3GiB、20秒请求、Unity/Windows兼容或语义质量。输出参数/receipt结构、固定输入SHA、最终≤1GiB和actual20GiB门禁保持。
