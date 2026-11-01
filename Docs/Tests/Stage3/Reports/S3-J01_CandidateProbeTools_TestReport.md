# 候选诊断工具与绑定门禁测试

2026-10-26。此报告只验收诊断工具叶和Qwen3候选的短数值等价，不表示候选模型质量、内存或S3通过。正式Qwen2.5包、场景和三层架构未变。

原候选来源官方Qwen/Qwen3-0.6B revision `c1899de289a04d12100db370d81485cdf75e47ca`；ONNX转换版onnx-community/Qwen3-0.6B-ONNX revision `58ca03a5e7b354b480b1a82b34bde8dc7b854cc0`，不是Qwen官方ONNX。Range下载大小919096585、LFS SHA `d43d836fc5e240df9013733ccd214972c5d21bd9ec47e574e4f1e359cf90aed0`一致。变体仅lm_head前最后位置Gather，919096845字节、SHA `3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303`，196个MatMulNBits，全部原始initializer dtype/shape/SHA不变。标准ONNX检查器旧默认域contrib问题及明确检查范围见F128和Checker_*.json，不宣称标准checker完整通过。

Equivalence_A真实CPU2线程、后台65716/控制16872，64-token prefill与一步续算的logits/KV最大绝对差均0。但原工具没有把实际文件SHA绑定到通过报告，不能用旧passed字段任意放行后续文件；A证据保留，不补写成当时已经绑定。

修正probe_qwen3_candidate.py：纯stdlib导入；显式equivalence-tag；completed/mode/version/稳定双份绑定与两步有限数值阈值一起核验；绑定两份实际模型、CandidateCases、官方四配置/分词文件、变换与下载记录SHA。运行前后复核不是不可变租约，报告明确无法排除两次检查之间修改后又恢复。异常保存已生成tokens、部分raw与stopReason，Owner finally清理引用，跨线程只传错误纯数据；不因报告写入失败而返回假成功。真实异常原生释放/泄漏仍需实际运行验证，纯工具测试不替代。

纯工具独立入口`python -m unittest discover -s Tools/Stage3 -p test_candidate_probe_guards.py -v`，13/13通过，0.363秒，原始GuardTests_A.log。覆盖旧/不完整/错模式/错版本/不稳定/模型或输入或配置篡改/错数值拒绝，正确当前绑定接受，导入无native依赖，以及错误跨线程不保留traceback。临时小字节夹具不是ONNX推理。

随后真实Equivalence_B，CPU2/控制66244/Owner66452、进程44604，两步logits/KV最大差均0、completed/passed/bindingsStable均true；文件绑定前后完全相同，含案例SHA `186bec0153bad9f113bc925f034725e86a2c1cfc823d66b5469214343539bcff`。原始Equivalence_B.json。此前Raw_A真实无约束18条：严格JSON10/18、Schema及意图/ID同时正确9/18；缺字段者计错、不修补、不剥围栏。A缺绑定边界保留，未覆盖为新门禁结果。

接下来仅隔离Editor测试同款C# TokenGenerator/JsonIntentConstraint。其使用官方HF输入IDs，输出临时复用已核验相同的旧基础词表，新增4个ordinary输出标记禁用；这不算新tokenizer适配完成。候选继续门槛17/18不替代最终100条≥90%/格式≥95%和安全零违规。每条原始输出先落盘再断言。

环境Win x64、ORT1.31.0 CPU、Unity6000.6.7f1；i7-12650H/RTX4060Laptop/16GB开发机。最低i5-8400/GTX1060 6GB/16GB中档1080p60、推荐i5-12400/RTX3060 6GB/16GB高档1080p60、Windows可玩包/完整断网/3GiB实际内存/并行性能均未在本工具报告通过。当前资源盘点12743575557字节=11.8684GiB，ResourceBudget_A.json，含候选分段与变体。
