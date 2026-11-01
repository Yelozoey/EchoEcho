# S3-S01a 存档目录与显示标签绑定

2026-10-27，Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR统一门禁通过。开发机i7-12650H/RTX4060 Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中1080p60与推荐i5-12400/RTX3060 6GB/16GB高1080p60非本存档测试结论。

StationPersistence.ConfigureDirectory增加可选S2/S3标签，默认S2，旧调用保持；标签非法或恢复Busy时不部分换目录，构造绑定本身不创建/覆盖文件。保存/恢复提示使用该标签，完整codec/原子文件存储不变。S3默认路径最早绑定由下一叶Bootstrap实施。

Binding_A作业7031f186060a4b5d9ee104ef169f1204：11/11、0失败/跳过、14.8928724秒。新3项验证S2原默认/旧调用、S3独立root与拒绝非法标签无部分变更、事务期间禁止换目录；原S2 StationPersistenceTests及StationLifecycleTests共8项保存/扫描/回路/还原/回滚/新局/未来档完整回归通过。文件操作仅既有夹具临时目录，未写用户S1/S2单槽。

入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-S01a --label Binding_A --assembly EchoStation.AI.Execution.Editor.Tests --assembly EchoStation.Station.Editor.Tests --group '^EchoStation.Tests.(AIExecution.S3PersistenceBindingTests|Station.StationPersistenceTests|Station.StationLifecycleTests)'`。regression_host Binding_A原工作区/Play选项/管线已还原，a通过，进入专用场景/导航与启动绑定。
