# 仓库第三方资源与许可索引

本仓库不将第三方资产统一改为项目代码许可，也未给项目自有代码新增统一开源许可证。以下通知随可重建工程保留；原下载包本机留存，不要求新克隆重复下载才能漫游当前场景。

| 内容 | 来源/许可 | 仓库内通知 |
| --- | --- | --- |
| Modular Sci-Fi MegaKit / Sci-Fi Essentials Standard、Lowpoly Robot | [Quaternius](https://quaternius.com/)，CC0 | Assets/EchoStation/Salvage*/ThirdParty/Quaternius/License_Standard.txt；InfiltrationArt/ThirdParty/lowpoly-robot/License.txt；InfiltrationRefinement/ThirdParty/SOURCE_NOTICE.txt |
| Echo Mars Rover | [JamesWhite / OpenGameArt](https://opengameart.org/content/mars-rover)，CC0；原项目已核验 | Docs/SalvagePrototype/R4_AssetSources.md、InfiltrationPrototype/A00_AssetSources.md和派生来源记录 |
| PBR材质与少量道具 | [Poly Haven](https://polyhaven.com/license)、[ambientCG](https://docs.ambientcg.com/license/)，CC0 | Docs/Assets/ThirdPartyLicenses.md；Salvage/ThirdParty/PolyHaven/MetalToolbox/LICENSE_CC0.txt及来源说明 |
| UI素材 | [Kenney](https://kenney.nl/assets)，CC0 | Assets/EchoStation/SalvageNext/ArtV2/UI/Kenney/*LICENSE.txt |
| 中文字体Noto Sans CJK SC | [Noto CJK](https://github.com/notofonts/noto-cjk)，SIL OFL1.1 | Assets/EchoStation/ThirdParty/Noto/LICENSE.txt |
| ONNX Runtime native/managed | [Microsoft ONNX Runtime](https://github.com/microsoft/onnxruntime)，MIT及相关第三方通知 | Assets/EchoStation/AI/ThirdParty/OnnxRuntime/Licenses |
| Qwen模型元数据/tokenizer/模型卡 | Apache-2.0；权重未上传，社区导出不冒称官方ONNX发行 | Assets/StreamingAssets/EchoLLM/*/LICENSE.txt、NOTICE.md、MODEL_CARD.md、PROVENANCE.json |
| 嵌入式Unity MCP编辑器包 | [CoplayDev Unity MCP](https://github.com/CoplayDev/unity-mcp)，包声明的MIT | ThirdPartyNotices/UnityMCP_LICENSE.txt，完整官方版权/许可随仓库分发 |
| Unity registry包 | 按各包独立许可经UPM还原，未把Library缓存分发进仓库 | Packages/manifest.json与packages-lock.json |

本轮涂层色图从已有许可资产派生：整数面积平均到512、灰度低对比重新着色，保留原法线/AO。原包及源纹理未改；此处理没有将原素材来源改成“自制全部美术”。完整历史来源清单见Docs/Assets/ThirdPartyLicenses.md、Docs/SalvagePrototype/AssetSources.md和R4_AssetSources.md。
