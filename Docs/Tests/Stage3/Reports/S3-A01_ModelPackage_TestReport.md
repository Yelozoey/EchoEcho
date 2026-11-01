# S3-A01 模型包/许可/校验/加载测试报告

2026-10-26。前置G01已归档。**20/20 A01测试通过，受影响S2 JSON回归12/12通过，允许进入A02。**

环境：Unity6000.6.7f1、Windows11 x64、D3D11、1920×1080、URP普通Forward内部HDR/SDR；开发机i7-12650H/RTX4060Laptop7956MB/16GB。最低目标i5-8400/GTX1060 6GB/16GB中档1080p60与推荐i5-12400/RTX3060 6GB/16GB高档1080p60沿用确认基准，本模块不声称目标机器验收。

## 实现和版本

- 正式本地目录`Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8`，8个固定内容资产加manifest，共972132844字节，低于1GiB。实际ONNX965074339字节，SHA `6d04df6470b91cc7a977be0d8eae87e27f3400ef654940ed85bd80e315411055`。
- 包含官方原始tokenizer/config/generation_config、Apache2 LICENSE、原始模型卡，以及明确记录社区ONNX来源和本项目量化/词表头修改的NOTICE。来源revision沿用G01，清单及每文件SHA见Evidence/S3-A01/InstalledRelease.json。
- 纯C# ModelRelease/ModelCatalog提供可信发布身份；ModelPackage严格验证版本、字段、类型、文件名、固定字节数/SHA和总预算，拒绝重解析点/额外内容。VerifiedModelPackage持有FileShare.Read句柄，阻止Windows校验后写入，并在Dispose释放。
- 复用既有MemoryJson.ValidateLexemes，只把纯函数从internal改为public，不改算法；Models只依赖无Unity对象的Memory.Runtime。拒绝Json.NET宽松JavaScript语法，受影响S2 JSON用例已回归。
- Barracuda在G01实际No-Go归档后移除；安装前原生检查证明干净、空闲且无Barracuda程序集。正式模型DefaultImporter，未触发旧Importer。原G01包/版本快照、tgz和失败证据保留，原有其他包版本不升级。

## 独立执行及结果

使用`Tools/MCP/module_tests.py`，每轮有新鲜SceneSafety/RenderSafety和空Console错误门禁，无自动保存/丢弃用户场景。

| 用例组 | 执行和实际结果 | 状态 |
| --- | --- | --- |
| 正常完整包/路径/释放 | 读取已校验文本；ModelPath仅允许清单文件；持有期间写入实际IOException，Dispose后可写 | 通过 |
| 缺失/同长度篡改 | 正确MissingFile/HashMismatch；失败后修复恢复验证，无遗留锁 | 通过 |
| 清单替换身份/hash | 不能以自报新hash更换可信模型；WrongRelease/HashMismatch | 通过 |
| 未来/负版本 | 两项均UnsupportedVersion | 通过 |
| 严格JSON | 单引号、无引号、注释、尾逗号、多JSON、重复字段六项拒绝 | 通过 |
| 字段/类型/重复文件 | 未知字段、字符串版本、重复文件名均拒绝 | 通过 |
| 路径 | 三个绝对/穿越文件名均UnsafePath；真实Windows junction在跟随前拒绝 | 通过 |
| 物理目录与元数据 | 额外DLL/目录拒绝，仅允许精确对应的Unity.meta侧文件；从不执行其内容 | 通过 |
| 取消/预算 | 预取消无遗留文件句柄；超1GiB可信库存或超64KiB清单拒绝 | 通过 |
| 真实模型字节与原生加载 | 全部972131513内容字节实际哈希；持校验锁时，后台ORT真实加载StreamingAssets路径并完成三步参考推理，全部通过 | 通过 |

首轮A01_A为20/20，9.749秒；补入“实际原生SDK在只读Lease下加载”的未决集成验证后，A01_B仍20/20、14.600秒，job `f155202a151044a5b67670869c0d8e11`。真实包哈希7.535秒，含后台原生加载/三步的整个实际包用例12.896秒；原生输出独立保留NativeUnderLease文件。

S2 MemoryJsonTests回归12/12、0.764秒，job `7479a8d16cef411bb242c1a09d9a0621`。两组范围独立但不加上重复A01首轮冒充更多唯一用例。源码测试入口`EchoStation.Tests.AI.ModelPackageTests`，复跑使用新label。临时数据仅在Library/EchoStation/S3Tests/A01的唯一GUID目录；清理验证路径前缀、不递归跟随junction，不删除用户数据。

## 保护、故障和限制

S1/S2保护767项仍全部一致。末次Editor空闲、dirty为空、FrameTiming getter/disk false/0、Profiler false/无载入捕获。新增资源仍低于20GiB预算；无运行时下载器、账户或远程代码。A01没有新产品报错；审查读取旧猜测文件名再次失败，沿用F106经验，改为先列出实际MemoryJson.cs。

本模块确认安全的本地模型包及加载通路，尚未实现生产tokenizer、完整生成、Owner调度或LLM游戏路由。测试的小权重夹具只验证文件合同，不算真实LLM；真实包用例另行执行。真正断网、Windows最终随包路径/跨进程验证在V01，完整回复初始化/时延与内存由I/T/V模块继续验证；不能把本模块哈希时间当主线程推理或完整请求速度。
