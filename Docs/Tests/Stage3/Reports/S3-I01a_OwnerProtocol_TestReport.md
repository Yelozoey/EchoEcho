# S3-I01a Owner通信/生命周期叶模块测试报告

2026-10-26。前置A04归档。**8/8通过**，job `c6be1f829b3647f0acbeab4d77bc80c6`，3.245秒；可进入I01b真实模型Owner服务，I01整体未完成。

Unity6000.6.7f1/Windows11 x64/D3D11/1080p/普通Forward内部HDR-SDR；i7-12650H/RTX4060Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中档1080p60、推荐i5-12400/RTX3060 6GB/16GB高档1080p60不在本叶验收。

InferenceOwner单一BelowNormal后台线程，显式Start才加载；请求复制ID数组、一个请求槽/一个结果槽、不传可执行请求委托；只轮询状态/结果，不后台UI回调。StopAsync只取消/唤醒，资源释放完成后才进入Stopped/Faulted，主线程不Join或等待。纯同步对象在最终状态锁内关闭，防止停止与Handle销毁竞争。

测试分别验证：构造不加载/Start一次/Owner释放；请求复制与1536边界；运行及未消费结果不排队；真实进入测试工作后停止调用<100ms且Owner释放；初始化故障为数据；执行故障返回Failed/释放；加载中取消；未启动停止不建资源。工厂/Runtime均为明确标记的测试替身，**不计真实LLM推理**；场景/Console/RenderSafety门禁通过。下一步真实模型两次完成/长prefill运行中取消及资源归属；300ms政策与逻辑超时细化仍I02/I03。
