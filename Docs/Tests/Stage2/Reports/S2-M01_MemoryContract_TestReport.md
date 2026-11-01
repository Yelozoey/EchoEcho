# S2 M01 记忆契约测试报告

2026-10-25，通过。用户已确认三类记忆、512/128/64 容量、120 字符摘要、去重与冲突拒绝、状态显式更新，以及后续关键词/片段预算提案；本模块只验收数据与内存存储。

## 环境和前提

Unity 6000.6.7f1，Windows 11，i7-12650H、RTX 4060 Laptop、约 16GB RAM。实际门禁为普通 URP Forward、内部 HDR、显示器 HDR 关闭、D3D11，默认/PC/实际均为 ES_Gameplay_ForwardHDR。测试前后原 ES_Cabin_Greybox 场景均干净，无 Prefab 未保存状态，Console 零错误。

项目性能目标仍是最低 i5-8400/GTX 1060 6GB/16GB 的 1080p 中画质 60 FPS，推荐 i5-12400/RTX 3060 6GB/16GB 的 1080p 高画质 60 FPS。此处纯数据 EditMode 测试不验证渲染性能，也不替代目标硬件实机验收。

## 实现与实际结果

新增独立 `EchoStation.Memory.Runtime`，显式 `noEngineReferences=true`。三类记录不可变，MemoryStore 负责有界入库、稳定事件编号、按类别 ID 去重、冲突拒绝、显式角色更新和固定快照。没有读取或保存玩家原文的接口，没有 GameObject 或组件引用，也没有游戏执行权限。

命令：

```text
python Tools/MCP/module_tests.py start --evidence Docs/Tests/Stage2/Evidence/S2-M01 --label A-MemoryContract --assembly EchoStation.Memory.Editor.Tests --group ^EchoStation.Tests.Memory.MemoryContractTests
python Tools/MCP/module_tests.py poll --evidence Docs/Tests/Stage2/Evidence/S2-M01 --label A-MemoryContract
```

实际结果：**14/14 通过，0 失败，0 跳过；测试框架用时 0.4570026 秒**。这是纯同步数据测试时长，不代表刷新、域重载和工具调用的总耗时。

| 用例 | 实际验证 | 结果 |
| --- | --- | --- |
| T01 | 三类记录、全部线索/角色子类、首次入库顺序与时间字段 | 通过 |
| T02 | 事件 1/2/3 稳定编号、重复和冲突不消耗编号、显示 `[2] 阅读维护记录。` | 通过 |
| T03 | ID 边界与现有 TargetRegistry 逐项一致；非法数据不写入 | 通过 |
| T04 | 摘要 1/120 和标题 1/80 边界保留；空白与超限拒绝，不静默截断 | 通过 |
| T05 | 负数/NaN/无穷时间和非法枚举拒绝，三级优先级可用 | 通过 |
| T06 | 三类重复入库只各产生一次成功通知 | 通过 |
| T07 | 摘要、时间、优先级、标题、分类、密码、标记或目标冲突不覆盖 | 通过 |
| T08 | 实际填满 512/128/64，超限拒绝且不删除；满容量仍识别已知记录 | 通过 |
| T09 | 列表拒绝修改；固定快照不跟随新事件、新线索和角色更新变化 | 通过 |
| T10 | 显式状态更新保留顺序且只通知一次，普通 Add 不覆盖，缺失/空更新拒绝 | 通过 |
| T11 | 密码与标记只接受显式元数据，不从摘要数字猜密码；可选字段边界与目标 ID 校验 | 通过 |
| T12 | 实际程序集无 Unity 引用；三类记录密封且仅只读数据属性，没有 FullText | 通过 |
| T13 | 独立存储实例和新局编号相互隔离；按 ID 查询和旧快照保持 | 通过 |
| T14 | 非法、冲突、容量不足均不产生成功通知，不部分破坏既有记录 | 通过 |

## 证据与限制

- [原始测试结果](../Evidence/S2-M01/A-MemoryContract.json)、启动回执及前置安全证据保留在同目录。
- [模块审计](../Evidence/S2-M01/ModuleAudit.json)：生产/测试文件哈希、测试汇总、后置 SceneSafety/RenderSafety、Console 零错误；S1 exe、两份关键运行时 DLL、两份字体声明的 5 项历史哈希均保持。
- [刷新超时回执](../Evidence/S2-M01/A-Refresh.json) 保留；相关经验为 F082。后续新鲜门禁确认域重载结束后才启动测试，没有把超时回执当成编译完成。

本次没有接线玩家日志/扫描、JSON 编解码、本地文件、关键词检索、对话或五区；它们各有后续模块。MemoryStore 由游戏线程拥有，后台使用 CaptureSnapshot 结果；不承诺多线程并发写。新局整体清空、快照恢复和存档一致性在 M07/F03 验证，不以创建新实例冒充真实读档。

结论：M01 独立通过，可进入 M02。S2 暂定 30 项中完成 1 项，完整剧情/谜题/结局提案仍需用户确认后编入游戏。
