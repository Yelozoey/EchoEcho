# Q3-A01c 已安装包后台校验通过

2026-10-27，依赖A01a读取8/8和A01b清单6/6通过。原生检查Scene/Prefab/编译/测试空闲与项目路径通过；确认新目的目录不存在、来源和目的绝对路径位于各自命名根、来源不是重解析点后，只移动本次新包至Assets/StreamingAssets/EchoLLM/Qwen3_06B_Q4。旧包、catalog和S1/S2不搬动。InstallGuard/InstallPaths记录实际门禁与路径。

Unity6000.6.7f1，新DLL242176B及域重载完成。作业d7ee4f3217f3476791e261fa52bda165，实际1/1通过、7.401秒。Owner196后台验证全部九文件长度/SHA，verify7.242秒；使用显式16MiB有界读取真实tokenizer，10,635,788 UTF16单元/11,422,654 UTF8字节完全匹配；原始Apache许可存在。模型只读租约期间写打开被拒，Owner196释放后九文件均可重新打开，未写入任何字节；completed/cleanup均true，总记录7.294秒，未超过30秒冷限制。

证据Evidence/S3-Q3-A01c/Installed_A-Job/Installed_A.json、安全记录和Package_20261027T0134522537879Z_136cd737bac843ffb085bfc534877f62.json。包930,558,465B，模型SHA3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303。测试没有创建原生推理session，不把文件验证当模型推理/分词/游戏或硬件通过。

A01正式包叶完成；下一叶按Q3-A02拆分适配26 added tokens/数组合并规则及动态字节缓存，独立官方golden后再适配运行时。S3仍11/24、J01未总验收，正式可玩版本仍保留原脚本模式。
