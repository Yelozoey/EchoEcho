# Q3-A03a 正式张量契约通过

2026-10-27，A03r真实metadata和15步参考通过后，新增独立Qwen3TensorContract，不修改旧24/2/64契约。新契约28层/16查询头/8KV头/128维、151936词表、59输入57输出；所有logits/KV float使用已验证位扫描完整检查。

Unity6000.6.7f1，DLL257024B与域重载、安全门禁通过。作业6e91353653a74cfda0d80f313ecc3a9b实际19/19通过，0.181260秒：新11项使用真实metadata与官方config；错名称/dtype/rank/旧几何/logits维/batch拒绝；输入长度/ID边界；含padding区域末尾NaN/±Inf拒绝；空KV、2048上限及错误runtime形状。旧纯TensorContractTests8项同时通过，没有加载旧权重。

证据Evidence/S3-Q3-A03a/Contract_A-Job/Contract_A.json及安全记录，参考A03r/ActualModelMetadata.json。数值参考和契约测试不是正式后端已完成；下一叶实现Qwen3OnnxBackend并在真实Owner上对照全部logits、短KV与多轮释放，S3仍11/24。
