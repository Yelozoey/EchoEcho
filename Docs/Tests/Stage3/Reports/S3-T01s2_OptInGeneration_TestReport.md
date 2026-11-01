# S3-T01s2 生成器与Factory可选采样

2026-10-27，Integration_A实际22/22通过，3.0334797秒，job `f4faa67261a84a3283967ee0bf14c0b2`，Evidence/S3-T01s2。

TokenGenerator仅在显式sampling参数时创建局部采样器；原Select greedy代码保留。GenerationResult增加只读采样元数据，LocalQwen3Factory/Runtime显式传递可选配置并回传，不改变默认S3启动。每请求新RNG、独立KV，六Schema与原JsonIntentConstraint不变。

9项新Q3 tokenizer协议夹具验证默认精确tokens/EOS、可选元数据、概率分支6次重复请求不共享RNG、高压早EOS不能越语法、前后取消、20秒逻辑超时、无候选不修补、显式TokenLimit和非法预算先拒绝；采样纯叶11项、正式Factory参数/拒绝主线程2项回归通过。均非模型推理、不是质量分数。下一叶才用种子42做同v10/同已见60真实诊断。
