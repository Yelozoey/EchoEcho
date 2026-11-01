# 第三方资源许可记录

2026-10-28新增独立回收原型原始资源：Quaternius两套免费Standard、Poly Haven工具箱/油罐/油桶、ambientCG两套材质，均按官方CC0来源接收。详细作者、版本、用途与许可见[回收资源清单](../SalvagePrototype/AssetSources.md)，原始文件与SHA/包内许可在Sources/SalvagePrototype，实际校验见[接收报告](../Tests/SalvagePrototype/Reports/R0-A01_SourceIntake_TestReport.md)。本次仅开发期原始文件接收，尚未进入Unity运行时资产，不改变下方既有资产与字体许可记录。

资源准备工具：Powered by Poly Haven（https://polyhaven.com）。工具使用独立User-Agent，仅开发期获取，游戏离线使用下载后的CC0资源，不接入实时API。

| 资源 | 作者 | 官方来源 | 许可与用途 |
| --- | --- | --- | --- |
| Concrete Floor 02，1K BaseColor/Normal/ARM | Rob Tuytel | https://polyhaven.com/a/concrete_floor_02 | CC0-1.0；小屋地/顶 |
| Painted Plaster Wall，1K BaseColor/Normal/ARM | Amal Kumar | https://polyhaven.com/a/painted_plaster_wall | CC0-1.0；小屋墙面 |

2026-10-25读取[官方资产许可](https://polyhaven.com/license)，确认其允许商业项目、修改、随项目分发，无强制资产署名；此处保留作者与来源便于追溯。官方页面和逐资源页面共同确认CC0。API条款与资产许可独立，工具遵守[官方API条款](https://github.com/Poly-Haven/Public-API/blob/master/ToS.md)中的可识别User-Agent和来源展示要求；不代表Poly Haven背书本项目。

获取时间、官方metadata原始JSON、每文件下载URL/大小/MD5/SHA256、许可页面SHA在Sources/PolyHaven/Manifest.json；API条款原文快照同目录。原始ARM只作为开发转换源保留在Docs，生成Unity Mask通道映射记入W02报告；运行时无API、账号或授权密钥依赖。

## Noto Sans CJK SC Regular UI字体

2026-10-25从[官方Noto CJK仓库](https://github.com/notofonts/noto-cjk)当前raw快照取得未修改静态Regular OTF，字体元数据Version2.004，Adobe版权2014–2021。许可[SIL OFL1.1](https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE)允许随软件嵌入/再分发，保留完整LICENSE及COPYRIGHT；不单独销售字体、不修改/重命名字体内部名称。每次出包复制通知到ThirdPartyNotices。

字体16437364字节，SHA256 2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b；完整来源/许可哈希/字体name元数据见Sources/Noto/Manifest.json。原文件包含CFF1，无CFF2/fvar，使用静态字型避免依赖可变CFF2。Unity动态字形纹理并包含源字形数据，按实际文本生成，不预烘焙全部中文。运行期Resources本地加载，避免依赖Windows可选中文字体或联网。
