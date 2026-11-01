# S3-A01 模型包与加载合同

2026-10-26，前置G01已独立测试/归档。普通实现细节在用户已确认方案内：

- 移除已经No-Go且未被生产代码引用的Barracuda3.0.2依赖，保留G01包前/后快照、官方tgz与失败证据。防止StreamingAssets内真实ONNX再次被其Importer误导入。其他既有依赖版本不改。
- 唯一正式候选为G01最终Qwen2.5-0.5B-Instruct，weight-only8/block32/accuracy4/last-head。模型SHA固定6d04df6470b91cc7a977be0d8eae87e27f3400ef654940ed85bd80e315411055。
- 目录Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8；仅本地文件，运行时无下载/账户/远程代码。A01不实现tokenizer/推理，分别留A02/A03/A04。
- 包内包括model.onnx、原始tokenizer.json/tokenizer_config.json/config.json/generation_config.json、LICENSE.txt、MODEL_CARD.md、NOTICE.md和版本1的manifest.json；所有文件均固定SHA/字节数，模型+tokenizer等总和必须≤1GiB。Unity自动生成的同名.meta侧文件仅在Editor验证时忽略，不进入AI上下文或随包清单。
- 纯C#验证器接收字符串目录和可信的发布描述；严格JSON、拒绝未来版本/重复/未知字段/文件、错误类型/大小/SHA、不合法文件名、路径穿越/重解析点；不信任包内自报的hash来更换模型身份。
- 验证后保留FileShare.Read文件句柄直到Owner释放，防止Windows上校验后被写入/替换。主线程只取得路径，后续I01负责在Owner执行哈希、加载、释放，不主线程等待。
- 缺失/篡改等返回结构化错误，模型与tokenizer版本不能混用。未知/额外物理文件拒绝，目录内不接收任意DLL或脚本。
- 严格JSON词法复用S2已验证的MemoryJson.ValidateLexemes；只将该纯函数从internal公开，算法不改。Models引用纯Memory.Runtime，不依赖Unity对象/游戏执行；除A01测试外重跑S2受影响JSON用例。避免Json.NET默认接收单引号/尾逗号等JavaScript扩展。

独立用例：真实965MB包完整校验；缺文件、改字节/改清单、未来版本、重复/未知字段、路径穿越/重解析点、大小/总预算、只读持有防写与释放后恢复、前次失败后可恢复、S1/S2保护。小测试夹具仅验证加载合同，不当成LLM或真实权重测试；真实包必须另有用例。
