# S2 M02 记忆 JSON 编解码

用户已确认复用项目安装的 Json.NET，不升级依赖。本地包为 com.unity.nuget.newtonsoft-json 3.2.2、Json.NET 13.0.2。M01 独立通过后实施本模块，仍无场景接线和磁盘访问。

格式标识 `echostation.memory`，schema version 1。封装字段为 format/version/payload/sha256，payload 只含 events/clues/characters 三数组；摘要及可选已知事实保持 M01 上限。SHA256 校验 payload 的 Json.NET 紧凑规范表示，用于损坏检测，不用于加密或防作弊。UTF-8 最大 2MiB、解析深度最多 16。

所有字段和类型显式校验，未知字段、缺失字段、重复字段、超限、非法枚举、非有限时间、重复 ID 和不连续事件编号拒绝。未来版本返回 UnsupportedVersion，保留给 M03 的不覆盖策略。解码先建立隔离的新存储，通过全部校验后才返回；不部分写入调用方的存储。运行时数据继续无 Unity 引擎依赖，不使用 TypeNameHandling 或执行外部类型名。

独立用例覆盖三类/中文/可选数据/空库往返、修改校验、字段缺失/未知/重复、非法类型与值、容量、未来版本、过大/过深/非 JSON、多根内容和原存储不变。2026-10-25 实际 M02 12项及 M01 回归14项共26/26通过，见 [实际报告](../Tests/Stage2/Reports/S2-M02_MemoryJson_TestReport.md)。F083 首编译失败和后续修复证据保留，可进入 M03。
