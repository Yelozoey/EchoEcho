# S3-I04b1 单Owner宿主协议

2026-10-26，通过26/26，9.879秒，job `7ba5db2bd6c443f6b74a5e7cadf6fcaf`，证据`Evidence/S3-I04/I04b1_A*`。先行a11项通过后才实现宿主；b1通过后进入b2实际模型。

环境/目标硬件口径沿用I04a：Unity6000.6.7f1/Win x64/D3D11/1080p普通Forward内部HDR→SDR；开发机i7-12650H/RTX4060Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中档60和推荐i5-12400/RTX3060 6GB/16GB高档60未在该协议测试验收。

入口module_tests.py，assembly EchoStation.AI.Editor.Tests，groups SessionHostTests、SessionFenceTests、RequestSupervisorTests。前置干净场景/Console/实际RenderSafety通过。新增7项宿主，另19项回归。

5种运行中世界变化逐次丢弃晚结果且不重载权重；结果必须先实际投递才能Claim、Claim仅一次、取走后读档仍拒绝；关闭时阻塞旧Dispose、提前开启不能新建，释放后共享完成戳=5000，5299拒绝/5300接受；不可中断加载时反复开关，旧返回并释放后才创建第二实例；两种替身均PeakActive=1；Fatal后Poll不无限重试，仅显式Restart可重建；SceneUnloaded/Dispose关闭并不可重新启动已销毁宿主。

替身只证明资源/通知协议，不算实际LLM质量/游戏读档接线。真正场景和存档事件接线由后续E/S模块验收。模型实例间冷却保留；实际模型b2继续。
