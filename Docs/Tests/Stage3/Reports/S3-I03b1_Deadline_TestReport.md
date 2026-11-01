# S3-I03b1 逻辑截止与迟到响应

2026-10-26，通过25/25，9.571秒，job `706707223b164e38b3ba8fca44bbcbae`，证据`Evidence/S3-I03/I03b1_A*`。b1通过后再开发真实模型b2；I03整体未完成。

环境/性能基准同I03a及I02：Unity6000.6.7f1/Win x64/D3D11/1080p/普通Forward内部HDR-SDR，开发机i7-12650H/RTX4060Laptop/16GB；最低i5-8400/GTX1060 6GB中档60及推荐i5-12400/RTX3060 6GB高档60尚未验收。

独立入口module_tests.py，assembly EchoStation.AI.Editor.Tests，groups RequestSupervisorTests、OwnerCancellationTests、InferenceOwnerTests、SchedulerTests（完整命名空间锚定）。新增8项、既有17项全部通过，测试前干净场景/Console/实际RenderSafety通过。

虚拟单调时钟：请求19999ms无超时，20000ms仅一次RequestTimeout且Generation=null；后台不可中断时保持Draining、拒绝新请求、不释放资源，实际返回后丢弃、299ms拒绝/300ms允许新请求。实际完成19999ms、主线程50000ms才Poll仍接受；实际20000ms完成则拒绝。已发布未消费成功可被玩家取消作废。

冷加载29999ms无超时，30000msColdTimeout并非阻塞停止；假后端晚返回的资源仍由Owner释放、不启动Generate。同样，及时加载但晚Poll不会误报；实际迟到加载即使没有此前Poll也拒绝。加载/后端失败分开类型，均无Generation。

不把虚拟时间替身算真实模型超时或质量。下一叶b2在实际ORT Run期间取消与推进逻辑时钟，验证资源安全及恢复；生产20/30秒常量不改变。
