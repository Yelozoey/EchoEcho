# G01 候选追踪

2026-10-26，当前最多三候选/90分钟上限保持。

1. HF社区Qwen0.5B INT4 block32：原模型embeddings和norm抽样逐值一致，完整图可在ORT后台执行，与Python相同图logits误差0；但原始FP32参考cosine/RMS未达预先门槛。保留失败，不当成正式选型。
2. 同源动态INT8：512096557字节，三步真实输出“遵循玩家输入”，仅数值探针，不称为JSON验收。审查与原始模型差异，不按体积最小即选用。
3. 第三候选采用官方ORT1.31.0工具，从固定的未量化ONNX图重新做block32、对称、weight-only INT8：仅168个常量MatMul主权重量化，embedding及共享输出头保留FP32，激活FP32，accuracy_level0。预计约0.9GiB，仍须实测SHA/真量化/体积/数值/性能；不使用未经授权的框架或全模型FP32运行冒充量化。

未量化ONNX只作诊断参考，不作为游戏候选或随包模型。其首步greedy与原始PyTorch一致；后两步如使用不同greedy前缀，不拿错输入做logits一致性统计。第三候选将在同一teacher-forced前缀上按既定阈值验收。

第三候选accuracy0的逐token约0.9秒，预示无法满足长回复20秒上限；同一批INT8权重先调整ORT accuracy_level=4核路径并复测。此处不改权重、不把失败当通过；如调优仍失败，按上限报告，不无限更换候选。
