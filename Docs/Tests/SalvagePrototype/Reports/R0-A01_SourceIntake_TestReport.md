# R0-A01 官方免费原始资源接收报告

日期：2026-10-28（Asia/Shanghai）。结论：文件级接收通过；Unity导入、场景美术、材质/碰撞/LOD适配与性能未执行。

## 范围、环境与授权

用户要求继续CC0资源准备，并确认新独立回收原型、单件手持与交付、Echo信息协作。使用Windows、本项目既有隔离Python及系统已安装curl；没有安装新依赖，不付费、不登录账号、不改全机网络/证书，不执行包内文件。

原始文件仅在 `Docs/Assets/Sources/SalvagePrototype/`；实际来源与作者见 [资源清单](../../../SalvagePrototype/AssetSources.md)。本报告不标旧S3完成，不恢复LoRA或Qwen2.5。

## 实际接收

| 资产 | 文件数 | 完整下载字节 | 许可依据 |
| --- | --- | --- | --- |
| Modular Sci-Fi MegaKit Standard | 1 ZIP | 48,559,316 | 官方CC0 + 包内License_Standard.txt |
| Sci-Fi Essentials Kit Standard | 1 ZIP | 166,910,545 | 官方CC0 + 包内License_Standard.txt |
| Metal Toolbox 1K FBX/依赖/ARM | 6 | 7,645,803 | Poly Haven官方CC0；Mateusz Sadek |
| Metal Jerrycan Green 1K FBX/依赖/ARM | 6 | 8,600,997 | Poly Haven官方CC0；Ulan Cabanilla |
| Barrel 03 1K FBX/依赖/ARM | 6 | 5,733,975 | Poly Haven官方CC0；Serhii Khromov |
| MetalPlates015A 2K JPG | 1 ZIP | 13,308,727 | ambientCG官方CC0；包内未发现独立许可文本，来源页留证 |
| Concrete034 2K JPG | 1 ZIP | 10,598,720 | ambientCG官方CC0；包内未发现独立许可文本，来源页留证 |
| 合计 | 22 | 261,358,083 | 7组，均为已校验完整文件 |

MegaKit有1195个压缩条目，包括190份glTF、378份FBX等不同格式/变体；Essentials有297条目，包括37份glTF、74份FBX。格式和变体数量不是独立模型数量，不使用官网完整付费套装总数冒充免费实际内容。两个Standard包内许可明确说明仅含完整套装的一部分，不含已配置Source工程。

## 执行与实际结果

| 检查 | 实际结果 | 状态 |
| --- | --- | --- |
| 官方免费版本与来源 | 两套Standard通过作者匿名免费入口，价格0；不选择Pro/Source | 通过 |
| 许可 | 7组官方CC0页面有URL/SHA留证；两套Standard包内CC0文字核实 | 通过 |
| SHA256与完整长度 | 22个清单文件重新计算，全部一致；部分文件不纳入完整文件计数 | 通过 |
| 官方MD5与依赖 | 18个Poly Haven文件与原始files.json逐项核对大小/MD5，FBX依赖和ARM齐全 | 通过 |
| ZIP校验 | 4个ZIP全部CRC通过；路径、符号链接、条目数、体积限制检查通过；总展开271,729,960B | 通过 |
| 审查器负向用例 | 正常ZIP不解包；父路径/Windows绝对路径/Unix绝对路径/符号链接/坏CRC拒绝；空字段不覆盖有效元数据；缺资源不能通过 | 9/9通过 |
| 目录隔离 | 工具限定Docs原始目录；不写Assets、不调用Unity、不执行下载资产 | 通过，文件级范围 |
| 资源预算和当前工程文件 | 另存最终统计与当前工程SHA前后对照；以FinalAudit_A.json为准 | 见最终回执 |
| Unity6000.6适配、实际画面、碰撞、LOD、实机帧率 | 未启动相关测试，不以本报告替代R0-A02或R4 | 未执行 |

可执行复核入口：

```powershell
& 'Library/EchoStation/S3Python/Scripts/python.exe' -u -X utf8 Tools/Assets/test_salvage_intake.py
& 'Library/EchoStation/S3Python/Scripts/python.exe' -u -X utf8 Tools/Assets/receive_salvage_sources.py --verify
```

## 原始证据与失败保留

- [Manifest](../../../Assets/Sources/SalvagePrototype/Manifest.json)、[完整校验](../../../Assets/Sources/SalvagePrototype/Verification.json)。
- [独立SHA/官方MD5/ZIP交叉检查](../Evidence/R0-A01/FileCrossCheck_A.json)、[9项安全检查](../Evidence/R0-A01/ArchiveSafety_A.json)。
- [补收前失败清单](../Evidence/R0-A01/Intake_BeforeSupplementMerge_A.json)、[Essentials真实补收](../Evidence/R0-A01/Supplemental_Essentials_A.json)、[Toolbox真实补收](../Evidence/R0-A01/Supplemental_Toolbox_A.json)。
- [当前工程文件基线](../Evidence/R0-A01/WorkspaceFiles_Before_A.json)、[最终对照/预算](../Evidence/R0-A01/FinalAudit_A.json)。

实际经历公开表单字段解析、短时签名、低速/断连/短正文、尾部分段超时和独立补收合并。首次完整接收任务因清单仍有失败项退出1，未标通过；补收真实文件并逐项校验后合并，最终 `--verify` 退出0。详见故障经验F-181至F-188。历史不完整文件若仍保留，计入实际目录预算，不作为可用资产或下载成功计数。

## 后续门禁

R0-A02先适配一个设施模块与一个工具箱：实际比例、材质、碰撞、UV/LOD统计与截图。所有原型玩法、AI新任务能力、原S3质量门禁及目标硬件60FPS仍未通过本次接收验收。

最终文件级回执：当前工程2403份文件前后SHA一致，18处本地文档链接有效；原始资源目录扫描约255.08MiB，既有S3与新增准备资源合计快照约11.8364GiB，低于20GiB。数值为FinalAudit_A扫描时点，回执本身和之后的文档补充不冒充已扫描字节。无Unity导入、无下载代码执行、无玩法模块通过。
