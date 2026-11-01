# Q3-A03r 独立数值参考通过

2026-10-27，依赖新包/分词/非思考包装各叶已通过。reference_qwen3_backend.py仅加载已安装固定Qwen3图，Python ORT1.31 CPU4/inter1/无自旋；后台Owner43372创建、执行、清缓存与释放，同主线程分离，无Unity对象/新权重/网络。

实际15次Run通过：输入1/16/64/669/1536，各三步自回归，真实59输入57输出、logits[1,1,151936]、56组KV[1,8,total,128]，每个输出float有限。冷3.459秒，总15.382秒，单Run均<20秒，模型开始/结束SHA一致；没有超时、错误，释放线程43372。

短1/16/64的全部KV及所有长度logits保存小端float32二进制，共66,918,912B；长669/1536只保存logits，实际形状与所有float有限已检查，不冒充长KV逐值数值对照。实际输出名称顺序单独核对为logits和逐层key/value，符合二进制打包约定。

证据Evidence/S3-Q3-A03r/Reference.json、ActualModelMetadata.json、BudgetBefore.json及Library/EchoStation/S3Cache/qwen3-native-reference-A/。开始账本11.499GiB、计划增量128MiB内。来源为同一已验优化INT4图，既有Equivalence_B原图/优化图数值证明保持；此参考不等于原始FP32质量或游戏性能。

下一叶纯C#契约使用实际metadata做正常及坏形状/类型/有限值边界，再实现正式Owner后端与二进制逐值对照。当前没有把参考脚本当作正式运行时，S3仍11/24。
