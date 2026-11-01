# 克隆、还原与可选模型

仓库包含当前可漫游主场景所需的源码、Unity导入源/材质/Lightmap/Cubemap及.meta，Packages/ProjectSettings和交接MD。使用Unity6000.6.7f1/Windows x64/D3D11打开根目录，等待UPM和Unity导入，再打开`Assets/EchoStation/InfiltrationCoated/Scenes/ES_Infiltration_Coated.unity`。没有在全新机器上完成完整导入验收；当前主场景的AssetDatabase依赖/Meta配对审计与已有工程实际测试结果见G01/F03报告。

本仓库.gitattributes禁用EOL自动转换，保留原始字节，避免已校验模型元数据、旧Prompt逐字字符串因CRLF/LF变化失效；代码仍可正常diff。只设置了本仓库core.autocrlf=false，没有改全机Git配置。发布审计会对暂存区中的模型小文件逐项复核manifest的SHA。

Library、Temp、旧Builds、原始Profiler、完整私有Evidence、原包与机器连接配置不会下载到新克隆；它们在原开发机保留。历史文档中的原始回执或绝对路径可能只在原机有效。当前精选实际截图和必要烘焙/材质收据例外保留在A01F Evidence。测试新标签会在本地生成新的忽略文件，不应覆盖原收据。

## 没有模型仍可打开当前场景

当前潜入场景只接了运动、美术/路线与反射，未接LLM，因此不需要约1.8GiB模型权重。默认脚本/旧质量No-Go保持。模型元数据、tokenizer、MODEL_CARD/NOTICE/Apache-2.0许可仍在`Assets/StreamingAssets/EchoLLM`；`model.onnx`及其.meta未入Git。不要因缺模型报错便删除现有验证或把LLM质量标为通过。

最可靠的恢复方式是从原开发机复制已经验证的最终包中的`model.onnx`到对应目录，并执行：

```powershell
python Tools/Repository/verify_optional_models.py --verify-present
```

该工具只读manifest/文件并核验大小和SHA，不下载、不转换、不训练。无模型时报告missing-optional；需要旧AI测试时才加`--require-all`，缺文件会非零退出。当前游戏美术测试不要求加此参数。

| 目录 | 文件与校验 | 说明 |
| --- | --- | --- |
| `Qwen3_06B_Q4` | 919096845字节；SHA256 `3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303` | 当前保留的Qwen3-0.6B INT4社区导出＋最后位置Gather派生图，工程可运行不等于内容达标 |
| `Qwen25_05B_W8A8` | 以其manifest.json的model.onnx条目为准 | 历史Qwen2.5分支暂停，不要自动切换它 |

仅有Git克隆时，可核对[社区固定版本](https://huggingface.co/onnx-community/Qwen3-0.6B-ONNX/tree/58ca03a5e7b354b480b1a82b34bde8dc7b854cc0)与[官方模型固定版本](https://huggingface.co/Qwen/Qwen3-0.6B/tree/c1899de289a04d12100db370d81485cdf75e47ca)。**上游文件不是本项目最终打包图**：原INT4图SHA为`d43d836fc5e240df9013733ccd214972c5d21bd9ec47e574e4f1e359cf90aed0`，本项目在lm_head前只取最后hidden位置，最终SHA变为上述3133...。不要直接重命名上游模型当成已验证包。

原转换算法可读`Tools/Stage3/slice_qwen3_candidate.py`、打包/来源记录见`PROVENANCE.json`和`stage_qwen3_release.py`。历史脚本还依赖原机Library缓存、私有Evidence及当时的20GiB检查，**不是当前克隆的一键安装器**。从上游重建需重新建立隔离工具环境、严格校验变换/最终SHA及数值等价；本轮没有执行这一过程，也不保证任意ONNX/protobuf版本序列化可生成同一字节。需要新增下载或模型实验时先按当前范围确认，不自动复活旧训练/Prompt授权。

## 测试与预算

MCP客户端为Python标准库，默认本地`http://localhost:8080/mcp`。先依AGENTS和HANDOFF准备安全宿主/原状态快照，再调用module_tests.py；原生IME只在Windows对应入口验证过，新潜入场景没有T面板。

`A01F/task.py budget`在原机使用历史计账基线，在新克隆缺少私有基线时使用`ResourceBudgetBaseline.json`的项目相对计账目录；实际不存在的原包/缓存记0，当前Assets/Docs与.git按实际字节计数。它不是整个Unity Library缓存目录的磁盘容量报告。本轮已确认21GiB资源上限，旧20GiB文档为历史。
