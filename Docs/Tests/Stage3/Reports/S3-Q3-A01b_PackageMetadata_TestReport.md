# Q3-A01b 独立包与清单通过

2026-10-27，前置A01a有界读取8/8通过。stage_qwen3_release.py仅使用已缓存Qwen3文件，无联网/新权重下载/训练/旧模型处理；校验原始下载元数据固定revision/SHA、优化图SHA/权重不变及Equivalence_B两步绑定后，逐文件校验来源、复制及目的SHA。

暂存Library/EchoStation/S3Staging/Qwen3_06B_Q4，9个信任文件合计930,558,465B（约887.45MiB）加manifest，低于1GiB；编译Qwen3Catalog.generated.cs为独立release echo-qwen3-06b-q4-last-v1，旧catalog/包不改。保留Apache-2.0、原模型卡、来源revision、社区非官方ONNX声明、只取最后位置变换及非FP32整体质量声明；PROVENANCE明确正式分词/游戏尚待验收。复制前资源预估11.499GiB/20GiB。

Unity6000.6.7f1，新DLL237056B及域重载完成，安全/Console门禁通过。作业030a9d5722af471495feb9e9d8f9e04a，实际6/6通过、0.099193秒：精确9文件/模型长度SHA/总预算、manifest与编译catalog一致、11.4MB tokenizer边界、原许可与来源记录、错release/未来版本拒绝、旧catalog未被替换。仅元数据测试，无原生推理。

证据Evidence/S3-Q3-A01b/StagedRelease.json、Metadata_A-Job/结果及Scene/RenderSafety。源优化图SHA3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303、官方tokenizer SHAaeb13307a71acd8fe81861d94ad54ab689df773318809eed3cbe794b4492dae4。后续先干净场景检查安装新目录，再在后台用正式ModelPackage验证全部文件与有界读取；没有宣称正式分词器、运行时、游戏或最低硬件性能已完成。
