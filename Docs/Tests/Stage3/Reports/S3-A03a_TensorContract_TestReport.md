# S3-A03a 张量合同叶模块测试报告

2026-10-26。前置A02归档；**8/8测试通过，允许进入A03b原生KV实现，A03整体尚未完成。** job `0ff7695d9f4049c2918e1b02df46febc`，1.192秒，守卫Scene/Prefab/Console/RenderSafety通过。

环境Unity6000.6.7f1/Windows11 x64/D3D11/1080p/普通Forward内部HDR-SDR，开发机i7-12650H/RTX4060Laptop/16GB。目标基准仍最低i5-8400/GTX1060 6GB/16GB中档1080p60、推荐i5-12400/RTX3060 6GB/16GB高档1080p60，不在此叶模块验收帧率。

从最终实际ONNX经ORT1.31.0取得51输入/49输出元数据，保存ActualModelMetadata.json；QwenTensorContract校验精确名称、Int64/Float32、rank、动态序列及batch/2KVheads/64headsize/151936词表，最终logits单位置。独立测试：真实元数据接受；错名/错dtype/错rank/错head/旧全序列logits五项拒绝；prefill1/1536接受、0/1537及越界ID拒绝；运行时坏shape、NaN/Infinity及超2048 KV拒绝。

当前仅纯数据合同与独立负面测试，不把这项结果当作真实新后端前向/缓存生命周期已通过。下一叶模块使用审查过的实际OrtValue API运行真实模型，沿用≤0.002参考容差、后台Owner与旧缓存安全释放。ORT API与shape-info反射记录已保存，不猜不存在的SDK接口。
