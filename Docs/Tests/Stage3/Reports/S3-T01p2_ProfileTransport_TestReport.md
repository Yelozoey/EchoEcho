# S3-T01p2 显式版本真实传递

2026-10-27，Transport_A实际1/1通过，15.2608999秒，job `2234965a826f49f184d05a7f20a7e00b`。

测试仅向Factory显式注入带PROFILE_TEST_01前缀的完整v5，默认Selected仍v5。真实权重加载11.3191196秒，请求2.6979955秒，原始输出`{"intent":"follow"}`。完整body和全部inputIDs与独立预期逐项相同；准备/原生调用/释放为同一非主线程，ownerCleanupComplete=true。证据`Docs/Tests/Stage3/Evidence/S3-T01p2/Profile_<ticks>.json`及Transport_A回执/预检。

F153初轮字段名编译失败已修正并重新编译，失败未执行旧DLL；不隐去错误。此叶只证明版本传递与真实一次推理，不计入100条质量分数，S3仍17/24。
