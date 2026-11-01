# S3-I04 会话代次与单实例生命周期

2026-10-26，通过，S3第9/24。a代次11/11，b1宿主/回归26/26先通过归档；真实b2首轮A为7/8失败，F121保留。修正计时夹具、不改100ms阈值或生产代码后，B为8/8、40.967秒，job `a611c4fcdfc84480a88ba9be9c4eab04`；独立重复C为1/1、34.128秒，job `79fd41ac421a44a4b9a74a5edbb2b12d`。

环境Unity6000.6.7f1/Win x64/D3D11/1080p普通Forward内部HDR-SDR，i7-12650H/RTX4060Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中档1080p60和推荐i5-12400/RTX3060 6GB/16GB高档1080p60仍待实机，不以本协议测试替代。干净场景/Console/实际RenderSafety通过。保护复核767/767未变化。

入口：module_tests.py，assembly EchoStation.AI.Editor.Tests，group `^EchoStation.Tests.AI.RealSessionHostTests`，可加SessionHostTests回归。原始回执/测试/RenderSafety/事件时间戳见Evidence/S3-I04及RealHost_*.json，a、b1报告另存。

| 验收 | B/C真实结果 |
| --- | --- |
| 实际原生Run期间LoadGame作废 | nativeActive=true；调用1.0825/0.0073ms，调用内GC0，旧结果未投递 |
| 真实已取走回复后手动覆盖 | 原回复成功，但ManualOverride后TryClaim=false |
| 开关重载与资源顺序 | 创建→完整Dispose→创建→Dispose；B线程8506/8507、C线程8522/8523，主线程1；每轮峰值1，结束Active=0 |
| 真实新回复一次性授权 | 新戳Claim一次成功、第二次拒绝，旧戳拒绝；每轮39次Run（旧20/新19） |

新局/读档换session；世界/手动/结局/开关等修改revision；请求号递增且失败提交不签新戳；结果必须投递成功才能领取一次执行资格。取消/关开关/Dispose都不主线程等待；冷却观察完整释放戳，先等旧Cleanup才创建新Owner。Fatal不在Poll中无限重试，显式Restart才恢复。

A首轮179.2487ms来自仍在运行的计时器，计入了JObject写入，但未保存局部时分无法分解。B记录报告写入0.4456ms，C为0.0032ms，不能据此唯一归因A为JIT或GC。计时范围已修正、两轮真实复测通过；瞬时外部调度/回收影响仍由V02完整Profiler记录，不能称目标机性能已达标。

本模块仅纯宿主协议与实际模型，不宣称Unity新局/存档按钮已接线（留E/S集成），不执行GameObject动作。原始JSON质量F120仍待J01/T01。
