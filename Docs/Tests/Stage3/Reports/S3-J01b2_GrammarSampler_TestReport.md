# S3-J01b2 采样器与结构预算协议

2026-10-26，通过30/30，4.562秒，job `4c749acaf6304d31b8a126168d08d5cf`，J01b2_A。先行b1为81/81归档，随后才接Provider；下一叶c运行真实模型，本叶不计模型质量。

Unity6000.6.7f1/Win x64/D3D11/1080p普通Forward内部HDR-SDR，i7-12650H/RTX4060Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中档1080p60和推荐i5-12400/RTX3060 6GB/16GB高档1080p60未实机验收。干净场景/Console/实际RenderSafety通过。

入口module_tests.py，assembly EchoStation.AI.Editor.Tests，groups JsonGrammarSamplerTests、JsonConstraintTests、TokenGeneratorTests。新增4项加26项回归。

两组有/无目标使用明确标注的uniform logits +高EOS测试替身（**不是模型**）：未完成对象时EOS被掩码，真实byte-BPE词表选token，240–256 token内完成并EOS，严格解析通过、缓存Reset一次、Mode记录json-intent-v1-reserve16；Provider每请求新状态且空Prepared拒绝；目标a/aa分别接受，aaa拒绝。真实模型/事实/语义正确率不在此断言。

ITokenConstraintProvider在Inference声明，纯Json模块实现，避免程序集循环；LocalQwenFactory可显式注入，当前默认null保留原始greedy对照。约束仅在实际Snapshot准备后、Owner内创建，使用被1536预算裁剪后的EffectiveTargets。生成仍逐次用原生logits决定合法token中的最高值，无脚本选意图或生成后补写。S3实际路由将在E阶段显式使用Provider。
