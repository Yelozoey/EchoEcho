# S3-T01p1 Prompt显式版本隔离

2026-10-27，独立叶模块通过，尚不构成T01模型质量验收。

- 实现不可变、有界的`Qwen3PromptProfile`及可显式指定版本的Preparation；默认仍为历史v5。
- Profiles_A实际16/16通过，2.3926902秒；包含原Preparation回归8项、新版本标识/严格UTF-8/16KiB上限/唯一有序占位/单次替换/官方包装/预算拒绝/默认隔离8项。
- 证据：`Docs/Tests/Stage3/Evidence/S3-T01p1/Profiles_A.json`；job `cade3815026343dbb74d88cf59d4884a`，新鲜场景、渲染、原生空闲预检同目录。
- 本叶只加载tokenizer，不加载权重、不推理、不宣称模型质量通过。后续Factory传递与真实生成单独验证。S3仍17/24。
