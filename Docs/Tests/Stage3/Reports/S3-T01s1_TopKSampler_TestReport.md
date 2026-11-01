# S3-T01s1 可复现有界采样器

2026-10-27，Sampler_A实际11/11通过，1.9500213秒，job `86135a1447614ae89c891e85b89bb793`，Evidence/S3-T01s1。

新增只读Temperature/TopK/TopP/Seed配置及局部xorshift32采样状态；先原语法/EOS可行集合，再TopK、温度softmax、TopP和概率抽样。参数有界（K≤20、0<T≤2、0<P≤1、seed非零），无语义关键词/评分标签/目标推断。所有概率/数组只在该采样器局部。

实际覆盖官方默认、非法数值、相同/不同种子、TopK=1、核截断/稳定同分ID、512次75%软分布、极端温差、语法允许集合、EOS和角色特殊token、全无候选/NaN/Infinity/形状拒绝。本叶无模型权重或原生调用；TokenGenerator/Factory尚未使用采样，默认greedy不变，不算模型质量通过。
