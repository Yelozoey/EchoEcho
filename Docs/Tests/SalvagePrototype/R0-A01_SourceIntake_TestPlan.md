# R0-A01 原始资源接收独立测试计划

日期：2026-10-28。执行范围是开发期官方免费原始文件，不包括 Unity 导入或运行验收。

工具：`Tools/Assets/receive_salvage_sources.py`；接收 `python -u -X utf8 Tools/Assets/receive_salvage_sources.py`；独立重新校验 `python -u -X utf8 Tools/Assets/receive_salvage_sources.py --verify`。使用既有隔离解释器，不安装新环境。工具不解包或执行下载文件。

| 用例 | 前置与步骤 | 期望 |
| --- | --- | --- |
| A01-01 | 核查每个组的官方页面与许可证据 | 7 组明确 CC0；保留来源、获取时间、网页 SHA 与版本，不把网页宣传图算资产 |
| A01-02 | 检查 Quaternius 免费入口与文件名 | min_price=0，匿名免费页只提供所选 Standard，pricePaid=0；不选择 Pro/Source，不提交支付/账号 |
| A01-03 | 下载完成后检查 HTTP 长度与文件 SHA256 | 有完整字节和 SHA；残留 .part 不算接收完成；HTML 错误页不能当 ZIP |
| A01-04 | 对实际 ZIP 调用 testzip 并统计条目 | CRC 全部通过；拒绝绝对路径、盘符、.. 和符号链接；解包体积与条目数在界内 |
| A01-05 | 查看 ZIP 清单中的许可文件 | 提取许可文本到 inventory 供人工审查；缺项时明确依赖官网证据，不伪造包内许可 |
| A01-06 | 根据 Poly Haven 官方 files JSON 检查 FBX 与 include 依赖 | 1K FBX 及其原始依赖、ARM 源文件齐全，所有大小/MD5 与官方逐项一致 |
| A01-07 | 独立 --verify 再读本地文件并重算 SHA | 与已写 manifest 全部一致；不存在同一路径多次计入 |
| A01-08 | 统计原始目录、ZIP 总展开尺寸及现有 S3 资源 | 接收 ≤512MiB、待解包总量 ≤2GiB，与 S3 合计 <20GiB；无祖孙目录重计 |
| A01-09 | 检查持久化目录和源码行为 | 所有下载在 Docs/Assets/Sources/SalvagePrototype；不导入 Assets，不执行第三方文件；匿名令牌不写日志 |
| A01-10 | 使用人工构造的越界/符号链接/坏 CRC ZIP 检查审查器 | 分别拒绝，且不产生解包文件 |
| A01-11 | 检查最终报告限定语 | Unity 材质、比例、碰撞、LOD、动画、实机性能均标记未执行 |

如果官方访问失败，不改证书验证/系统网络、不用不明镜像；保留错误与部分文件，报告已接收/失败/未执行，不提前宣布完成。完成后实际数据写 Reports/R0-A01_SourceIntake_TestReport.md 与 Evidence/R0-A01。
