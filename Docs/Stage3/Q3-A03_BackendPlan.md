# Q3-A03 正式张量与后端拆分

2026-10-27，依赖Q3-A01包三叶、A02分词三叶实际通过。固定已安装优化图SHA3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303、ORT1.31 CPU4/inter1/无自旋、实际后台Owner，不下载/加载其他模型。

1. **A03r 独立参考**：Python ORT在专用后台线程加载同一新包模型，输出真实metadata、1/16/64/669/1536输入及各三步连续解码的logits和贪心token参考。短1/16/64保存全部56个KV为有界二进制；长输入核验全部输出有限/shape并存logits，不能把未存长KV称逐值数值对照。参考数据计划上限128MiB，先资源预检；冷30秒、单次Run20秒、不主线程释放/强杀，错误纯数据传回，保留Owner释放记录。已有Equivalence_B原图/最后位置图对照保持，不重写成FP32质量证据。
2. **A03a 纯契约**：独立Qwen3TensorContract，28层/8KV头/128维、151936 vocab、59输入57输出；保留旧24/2/64契约不变。读取真实参考metadata而非仅手造成功值；坏名称/dtype/rank/旧几何/logits维、输入/上下文边界、NaN/±Inf与KV形状拒绝；完整有限值仍用已通过的位扫描。先独立测试报告。
3. **A03b 正式ORT后端**：显式新catalog/已验证租约、仅Owner创建/运行/清KV/释放，CPU4/inter1无自旋。IO形状/类型/数量/所有float核验，NativeActivity与分配/释放计数；接口只承载tensor和数据，不含Unity对象。各长度三步logits与短KV逐值maxabs≤1e-4，贪心token精确匹配独立参考；多轮创建释放、外线程拒绝、坏输入不进入native、无跨请求KV。原图变换的旧精确阈值不改，此为同优化图跨语言后端对照。

通过后才接入正式C#上下文/获选v5与Owner服务，复测完整生成及独立100条，再J01总复核/J02权限。不能把Python输入或诊断CandidateBackend当正式后端完成。Windows、3GiB隔离内存、主线程帧/GC、压力与三结局仍须后续模块实测。
