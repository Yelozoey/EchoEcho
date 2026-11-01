# S3-J01：Qwen2.5-1.5B 候选下载工具叶测试报告

日期：2026-10-26（Asia/Shanghai）。状态：**下载工具叶初轮 14/14、前缀续传修复后 20/20；元数据、小文件和 FP32 小图已取得。后续真实外部权重 Weights_A 失败，权重未完成。模型兼容性、数值、量化、质量、速度与内存均未验收。** 本报告不将 S3-J01 或 S3 标为完成。初轮实际结果原样保留，下文末尾记录真实失败与修复。

## 范围与环境

新增 `Tools/Stage3/download_qwen15_candidate.py` 与 `test_qwen15_download_guards.py`。使用项目专用 `Library/EchoStation/S3Python/Scripts/python.exe`（Python 3.13.12）和标准库；没有安装新依赖、执行远程代码、修改 Unity/生产模型/场景、修改系统网络或启动推理。

来源固定为官方 `Qwen/Qwen2.5-1.5B-Instruct` revision `989aa7980e4cf806f80c7fef2b1adb7bc71aa306`；ONNX 转换来源 `onnx-community/Qwen2.5-1.5B-Instruct` revision `d54957e3af1be04546436e874a60fe386041b244`。后者是社区转换，不能称官方 Qwen 导出。官方 LICENSE 已下载并通过固定 Git blob 校验，内容为 Apache License 2.0。

`CandidateQwen15/original_metadata.json` SHA256 为 `a6c93ae4ae94792ad92b8e3bc83ad477ef9d91c02c6b2681cb8be4134c6c202f`；`onnx_metadata.json` 为 `37c48edb11f40445705e3e6a349397923526838e16b4ee43e2a289103db53113`。两份均从 Hugging Face 固定 revision 的 `?blobs=true` API 保存，核对 repo/revision、公开且未 gated；没有以可变 main 名称取权重。

## 独立用例与实际结果

命令：

```powershell
& 'Library/EchoStation/S3Python/Scripts/python.exe' -X utf8 -m unittest discover -s Tools/Stage3 -p test_qwen15_download_guards.py -v
```

首次执行 14/14，0.440 秒；归档复跑 **14/14，0.296 秒**，日志为 `Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen15/DownloadGuardTests_A.log`。所有数据为临时目录中的约 1.8 KB 夹具，模拟 HTTP 响应；没有 Internet、Unity、ORT 或真实大文件测试。

| 用例 | 实际结果 |
| --- | --- |
| 导入工具不解析参数、不联网、不加载模型 | 通过 |
| 4 路定位写入同一预分配文件，最后短块正确；无独立 chunk 副本 | 通过 |
| 已有完整文件重新检查最终 SHA，正确时不联网 | 通过 |
| 同长度但损坏的完整文件拒绝复用 | 通过 |
| 严格要求 206、完整 Content-Range/总长、Content-Length、identity 编码 | 通过 |
| 截断响应不记完成；每块最多 3 次；失败保留错误与 partial | 通过 |
| 块本地校验均一致但整文件与 LFS SHA 不符时拒绝重命名 | 通过 |
| 恢复时只跳过 manifest 已持久化且重算 SHA 一致的完整块 | 通过 |
| 已完成块被改写时重新下载该块 | 通过 |
| revision 变化或孤立 partial/manifest 拒绝恢复 | 通过 |
| 当前资源加待分配加量化/安装预留不得超过上限；已有预分配仅计一次 | 通过 |
| repo/revision/private/gated/LFS size 或身份不匹配拒绝 | 通过 |
| 小文件 Git blob 校验；大文件禁止走小文件路径 | 通过 |
| 新证据禁止覆盖；路径穿越/绝对盘符路径拒绝 | 通过 |

工具使用 OS 文件锁，同一目标并发启动不能同时写。恢复 manifest 中的块 SHA 是本地完整性记录，不能替代上游全文件 LFS SHA；只有最终 LFS 校验成功才将 `.part` 重命名。下载失败后已完成块可重验复用，正在写但未完成块从块起点重下，不把预分配的零字节当成功。任一块耗尽重试后停止组内其余任务；单个读取等待上限 45 秒，不改 TLS。不存在全局跨工具磁盘预留事务，root 必须协调其他同时写入的大资源任务；工具在分配前与完成后重新统计全部已授权 S3 根目录及 AI 测试源码。

## 真实小文件获取

执行 `download_qwen15_candidate.py --label Small_A`，未带 `--weights`。UTC 14:13:29 至 14:14:53，进程退出 0，`completed=true`、`verified=true`。记录见 `CandidateQwen15/Downloads_Small_A.json`。非 LFS 文件按固定 Git blob SHA1 验证，LFS 文件按固定 SHA256 验证；下表另保存实际 SHA256。

| 文件（缓存 qwen15-candidate 下） | 字节 | 实际 SHA256 |
| --- | ---: | --- |
| original/LICENSE | 11343 | 832dd9e00a68dd83b3c3fb9f5588dad7dcf337a0db50f7d9483f310cd292e92e |
| original/README.md | 4917 | 2e1bcd8bd964728a820be709fa0f7b9dd54817a94fd2254c535df70c5e67fada |
| original/config.json | 660 | 98d2ff8cc47488d08a2b0b3acf4eb99ef210779b42bd48605f6b8e36acdbf670 |
| original/tokenizer.json | 7031645 | c0382117ea329cdf097041132f6d735924b697924d6f6fc3945713e96ce87539 |
| original/tokenizer_config.json | 7305 | 5b5d4f65d0acd3b2d56a35b56d374a36cbc1c8fa5cf3b3febbbfabf22f359583 |
| original/generation_config.json | 242 | e558847a8b4402616f1273797b015104dc266fe4b520056fca88823ba8f8ebe6 |
| onnx/README.md | 545 | 6a2b2c767dc22ef6b7bdb6010f10be7aa8324480f153631efad4737ab8f8180e |
| onnx/config.json | 809 | 215eb99c4955b0c42ea9f6e0980d922c228950c5dbc09bde6dc451fbba4d21f3 |
| onnx/onnx/model.onnx | 1057843 | 8f7da5d0bb5e5b6eba0ba0e9c006fcbc8b670134405ef5e02aaaf738361e2074 |

小图不含外部权重，仅取得小图不证明模型能加载。后续待下载 `onnx/model.onnx_data` 固定 size **6208411648**，LFS SHA256 **7111acf0e546fe192035357890402832315b4c243e56b01b47ce9358285fc82e**。本叶没有启动该请求。

## 资源与后续门禁

Small_A 完成时实际 S3 统计 **10861951693 字节**，上限 **21474836480 字节（20 GiB）**。工具始终额外预留 **2321966696 字节**，由量化产物与安装副本各向上取整 1.05 GiB，加 64 MiB 余量构成。按当前统计加外部权重与预留，预计 **19392330037 字节**，仍低于上限；这只是下载前预算，不是量化成品体积/峰值内存通过证明。后续实际下载前会重新统计，不能沿用本次数值放行变化后的环境。

可由 root 在确认本叶报告后运行新的证据标签：

```powershell
& 'Library/EchoStation/S3Python/Scripts/python.exe' -X utf8 Tools/Stage3/download_qwen15_candidate.py --weights --label Weights_A
```

巨大权重的真实 206/断点恢复/最终 SHA 尚未执行；量化、数值等价、真实 JSON/意图、Unity 后台、3 GiB RAM、20 秒请求、1 GiB 随包、100 条正式质量门槛均须后续独立实测。当前工具叶通过后方可取得依赖它的权重；不得直接进入玩法整合。

## 后续真实失败 Weights_A 与前缀续传修复

root 在初轮工具验收后实际执行 `--weights --label Weights_A`，UTC 14:17:24 至 14:24:12，报告时间跨度约 408 秒，进程退出 1。`Downloads_Weights_A.json` 明确 `completed=false`、`verified=false`、`Truncated range response`。0-based chunk 2 三次分别截断、读取超时、截断；只有 chunk 1 的 67108864 字节完整并持久化 SHA `e32de8195247121c4157399f8b893704839670e787abf1da02ab6ee46f761c99`。其余正在下载任务因同组停止而中止。全部 `RangeError_*.json` 与失败报告保留；失败结束时的 manifest 已额外保存为 `Weights_A_ResumeManifest.json`，没有把 partial 重命名为完整权重。

旧实现只登记完整的 64 MiB 块，块中已写入但未完成的前缀在同进程重试或新进程恢复时会从该块头重下。旧失败没有记录实际前缀长度，无法事后将预分配文件中的非零字节猜作已验证前缀。不能因为纯小文件用例通过就称长文件传输可靠；本次属于真实网络中断触发的工具恢复效率问题，不是模型加载或语义失败。关联故障 **F-133**。

修复保留原 version 1 及完整块记录，增加可选 `partial: {index: {bytes, sha256}}`。每次失败在单一原预分配文件内 flush/fsync，重算该块已收到前缀的磁盘 SHA 与响应载荷 hash 一致后原子持久化；重试请求起点为块基址加已验证字节。新进程恢复先核对固定 repo/revision/文件 SHA/总长/chunk 长度，再重读前缀核 SHA；损坏则丢弃该前缀信用并从块头重下。不存在记录的旧块前缀不会被猜测采用。`IncompleteRead.partial` 只在此前已通过精确 206/范围校验，且其长度不超过当前读取及剩余范围时写入并记账；超界数据拒绝。失败响应即便已给足全部字节也不直接升级成完整块，至少重取最后一字节后再走完整校验。4 路上限、每块每次进程最多 3 次请求、45 秒读取超时、磁盘预算和最终全文件 LFS SHA 门禁不变；无独立 chunk 副本、无 TLS/网络改动。

修复首测 **20/20，1.108 秒**；归档复跑 **20/20，0.741 秒**，`DownloadGuardTests_B.log`。原 14 项回归及新增 6 项全部通过：

| 新增独立用例 | 实际结果 |
| --- | --- |
| 首次只得到 37 字节后，下一请求严格为 bytes=37-255 | 通过，最终夹具全字节相同 |
| 三次各获得 23 字节失败后持久化 69 字节，新进程严格从 69 继续 | 通过，最终夹具全字节相同 |
| 持久化前缀被损坏，新进程从块头重新下载 | 通过 |
| 前缀文件绑定/chunk 长度变化、零/整块/布尔长度、坏 SHA、额外字段、完整与前缀重叠拒绝 | 通过，未联网 |
| IncompleteRead.partial 合法的 31 字节保存，下一请求严格从 31 继续 | 通过 |
| IncompleteRead.partial 超过请求读取范围时拒绝写入/记账 | 通过，文件对应片段仍为零 |

这些仍是约 1.8 KB 的独立工具夹具，不构成 6.2 GB 下载成功。修复 agent 没有执行 Weights_B；root 需用新标签实际复测、核对最终固定 LFS SHA 后才可量化。Weights_A 的单一预分配文件、原完整块与失败证据均保留。
