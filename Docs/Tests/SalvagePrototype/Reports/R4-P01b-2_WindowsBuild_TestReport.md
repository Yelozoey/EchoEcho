# P01b-2 Windows构建

2026-10-30，最终Build_9064c6cc6fa64ec7bb422a446c7b5dd3：Succeeded，0错误，29.911秒。输出Builds/SalvageNext/EchoStationSalvageNext.exe，259,966,856字节（约248MiB，验证截图/Profiler尚未产生）。

四项原生Player/Graphics/Quality/UnityConnect的JSON、磁盘字节、dirty状态恢复全部true；烘焙变体scope和SDK保存scope也通过。旧配置保存被拦，三自有管线参与构建，原场景列表未改。仅新版输出移除1,902,692,739字节EchoLLM模型，工程源/旧构建保护另由最终SHA审计验证。随包CC0来源、Noto OFL、ONNX Runtime许可。

最终增量构建有1条Unity Services未关联项目警告；未连接或启用云服务。首次完整构建还记录网格未来预烘焙碰撞提示、旧Development编译指令过时及System.Windows.Forms依赖警告；增量缓存不再重复不代表已修复，实际运行需继续验证。首次e4fa虽引擎Succeeded，但恢复检查失败，保留为失败证据；F267/F268修复与PlayerScope_C3/3后才重建。

本项只证明构建与保护恢复，未证明Windows流程、原生显示或目标硬件性能。下一运行真实可见Player，不用静态文件检查替代。

F269修订后最新Build_712b8e68b6bd488aa800f57daee3d8a7通过，37.844秒/0错误/4条已记录警告；四设置/光图/保存scope再次全部通过。268,442,728字节包含本轮Windows_A验证产物，不将含证据目录的总大小冒充纯发行体积。此版开始Windows_B完整重测。

DRoute_E后Build_a7b6f998130547fdb7853f0ca2f4580b通过，全部恢复scope验证true；含玩家真实步行让路与原生坏槽验证的新夹具，开始Windows_C完整验证。构建字节含之前本轮运行证据，不代表纯发行大小。

最终交付构建为Build_9e6d0080a96146a98ac1963712c26d40：Succeeded、0 errors、32.576秒、4条已记录警告，原生设置/光图/保存scope全恢复。与a7b6的646项产品SHA实际零差异，后续Display_F/Perf_F已原生通过，玩法Gameplay_D已完成。测试产物/符号归档后纯包精确体积与SHA以Evidence/R4-Final/DeliveryManifest_G.json为准；本轮最终结论见R4_Final_Delivery_TestReport.md。
