# Q3-A01 独立正式模型包叶计划

2026-10-27，只在R04的CPU4独立100条/性能复核结束且通过后开始改代码/复制模型；当前仅记录已核对的接口和测试入口。旧包不搬动、不转换、不推理，源StreamingAssets保留历史依赖，S3构建隔离另测。

执行更新：R04已通过；A01a有界读取实际8/8、A01b新包清单6/6通过归档。新包930,558,465B已通过干净Scene/Prefab/空闲检查，从本次独占暂存目录移至独立Assets/StreamingAssets/EchoLLM/Qwen3_06B_Q4，绝对路径/目的不存在门禁及回执见S3-Q3-A01c。当前验证真实安装文件，尚未开始后续分词器代码。

按依赖逐叶：

1. **A01a 有界文本读取**：VerifiedModelPackage.ReadUtf8现在限制8MiB，Qwen3官方tokenizer为11,422,654 UTF8字节/10,635,788字符。保留旧默认8MiB；新增显式受限读取参数，最多16MiB，只给新包已验SHA的文本使用，不能任意大文件读入内存。独立用微型模型+9MiB文本夹具验证默认拒绝、受限显式接受、越界/坏UTF8/释放后拒绝及读锁；测试不加载旧权重。通过后归档。
2. **A01b 新包元数据/目录**：沿用ModelRelease/manifest严格版本、路径、长度、SHA和只读租约。新增Qwen3Catalog与独立Qwen3_06B_Q4目录，保留旧ModelCatalog.Current不变，后续S3工厂明确选择新catalog。使用已有优化图SHA3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303、官方固定revision配置/词表/许可/README、导出来源/变换与数值绑定说明；不下载新权重。复制前重算资源预算，不在推理性能采样时复制或做大SHA。
3. **A01c 真实包验证**：后台验证完整新包、文件数/总量≤1GiB、已固定SHA与来源，冷准备30秒限制不变。缺失/重复/未来版本/错ID/篡改/额外文件/路径/取消用小夹具先测，不反复重读大权重。新包不作为已完成分词或可玩游戏。

包内容预计：model.onnx、tokenizer.json、tokenizer_config.json、config.json、generation_config.json、LICENSE.txt、MODEL_CARD.md、NOTICE.md、manifest.json。官方来源Qwen/Qwen3-0.6B revision c1899de289a04d12100db370d81485cdf75e47ca；ONNX来源onnx-community/Qwen3-0.6B-ONNX revision58ca03a5e7b354b480b1a82b34bde8dc7b854cc0。保持graph优化只取最后位置logits、权重不变的既有记录，不把社区导出称官方ONNX。

每叶实际独立通过后再整合，任何新异常继续写Troubleshooting。最低/推荐硬件1080p60、3GiB隔离AI内存、游戏并发性能、Windows正式加载均在后续模块，不能因包校验通过提前记达标。
