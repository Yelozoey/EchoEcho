# S3-A03 正式ONNX张量/KV后端测试报告

2026-10-26。A03a先8/8归档，A03b与合同回归**10/10通过**，实际后台5次创建/释放、20次原生Run、短/1536长输入logits与Python均误差0。允许进入A04。

环境Unity6000.6.7f1 / Windows11 x64 / D3D11 / 1080p / 普通Forward内部HDR-SDR，i7-12650H/RTX4060Laptop7956MB/16GB。最低i5-8400/GTX1060 6GB/16GB中档1080p60、推荐i5-12400/RTX3060 6GB/16GB高档1080p60仍为外部目标，本模块不据开发机声称达标。

实现QwenOnnxBackend：只接受当前已校验模型Lease，创建/执行/缓存重置/释放均检查Owner，构造前拒绝Unity主线程；ORT遥测关闭、显式CPU、2/intra+1/inter、顺序执行/禁自旋。51输入及49输出逐名/类型/形状验证，mask全1/位置按实际消耗递增，初始48个空past；全部logits/KV有限值校验。OrtValue保留原生present到下一步，新的Run完成并验证后才释放旧集合，未复制48份缓存到托管数组。logits仅Owner有效期内只读Span；失败使会话不可继续，缓存清理。调用者保持包Lease直到Session释放。

独立入口`TensorContractTests`与`OnnxBackendTests`，用module_tests.py门禁，job `310ea293533648208f88bf5025464d81`：10/10、0失败/跳过、36.833秒。A03a被重跑，不与旧8项再次累计。实际NativeBackend_*.json保留：Unity主线程1，Owner8154，20次Run全走Owner，5轮Dispose也在8154。

| 范围 | 实际结果 |
| --- | --- |
| 元数据 | 最终原图真实元数据51/49接受；错名/dtype/rank/head/旧全序列logits拒绝 |
| 189tokens自回归 | 5轮各三步73594→2236→198；每位置151936 logits最大差0，KV消耗189/190/191 |
| 1/16短输入 | 真实前向及单位置词表输出正常，ConsumedTokens与输入一致 |
| 1536长输入 | 三步105043→74994→3837，每位置logits最大差0，KV长度1536/1537/1538；三步总7.640秒 |
| 缓存重置 | 长/短序列重置后长度0；未prefill即Step拒绝，未沿用跨序列KV |
| 非法输入 | 0/1537长度、负/超词表ID拒绝；实际非法调用前后NativeCalls不变 |
| Owner | 主线程构造拒绝；实际异Owner重置尝试拒绝；不在Unity主线程Join或推理 |
| NaN/类型/形状 | 合同独立负例拒绝非有限logits、坏KV/词表shape及>2048长度 |
| 生命周期 | 真实包只读Lease覆盖Session；5次完整Owner创建/运行/释放成功 |

此模块把G01 Spike转为正式数据后端，尚无完整生成/EOS/JSON约束、请求调度器或游戏对话路由。原生API经实际DLL反射审查；shape-info为无IDisposable的快照，未猜接口。G01的Windows/内存证据为先行兼容证据，A03不能据此冒充正式后端最终Windows/长会话验收，V01/V02仍需复测。既有Unity Curl35日志沿用F002，Console/渲染门禁实际通过，未改TLS或网络设置。下一步A04完整自回归/停止/UTF8/预算，随后I01正式单Owner服务。
