# Q3-R01b 全量有限值扫描独立测试

2026-10-27，Unity6000.6.7f1 Mono开发环境，普通Forward/HDR/SDR/D3D11与场景/Prefab/Console新鲜门禁通过。Finite_A job9fd6a09380bd4862b0ff78dd28d1a5f6，**3/3通过，1.0458262秒**。

FiniteTensorValidator.RequireFinite使用MemoryMarshal.Cast<float,uint>零拷贝读取每个IEEE754 binary32的指数位；全1指数同时覆盖正负Infinity及所有NaN payload，零/负零/正常值/次正规值保留。未跳过任何KV或logits元素，dtype/shape/count仍由调用方完整核验。尚未替换现有生产后端；先用于隔离Qwen3 runner。

| 独立用例 | 实际 |
| --- | --- |
| 16种边界位模式及8192个确定性随机位模式，对照框架NaN/Infinity定义 | 全部一致，坏值拒绝 |
| 空数组、切片，坏值位于首/中/尾 | 合法切片通过，所有坏值位置拒绝 |
| 同一1048576个float、8轮、预热后的受控扫描 | 原检查62.4556ms、位扫描4.3366ms；只为微基准，不作推理/游戏性能验收 |

证据Tuning/Finite_A/Finite_A.json、Job/SceneSafety/RenderSafety，以及Tuning/FiniteScan_20261026T1618068510478Z.json。微基准约14.4倍仅针对该扫描，不外推整体响应。下一步真实模型用同一v2/18条再次逐token对照和分项计时；旧语义15/18仍未达17/18，S3不增加完成数。
