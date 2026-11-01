# S3-I01 单Owner服务实际测试报告

2026-10-26。I01a先8/8协议归档，I01b与回归**9/9通过**，允许进入I02。真实包/解析/tokenizer/Session/生成/释放始终由Owner8248负责，Unity主线程1只轮询和发停止。

环境Unity6000.6.7f1、Windows11 x64、D3D11/1080p/普通Forward内部HDR-SDR；i7-12650H/RTX4060Laptop7956MB/16GB。目标最低i5-8400/GTX1060 6GB/16GB中档1080p60、推荐i5-12400/RTX3060 6GB/16GB高档1080p60仍需外部实机，不以本模块声称达标。

InferenceOwner构造不加载模型，显式Start唯一BelowNormal后台线程；纯工厂路径数据、复制tokens、一个请求槽/一个结果槽。无后台UI回调，无GameObject/Transform/执行器。StopAsync只发取消/唤醒，不主线程Join；最终资源Dispose后才标CleanupComplete。原子NativeActivity仅提供次数/线程/是否在Run，未向主线程暴露Session或Tensor。

module_tests.py实际job `40934e205ea9465cb369a8200d94c8da`，9/9、0失败/跳过、26.430秒；场景/Console/RenderSafety门禁通过。I01a测试替身只用于协议，不计真实LLM；以下来自RealOwner_*.json：

| 场景 | 实际结果 |
| --- | --- |
| 真实初始化 | Start在主线程<100ms返回；Owner8248冷加载10.990秒，主线程未等待 |
| 完成请求1 | 19个真实生成ID，2.248秒、EOS，结果经有界槽取回 |
| 完成请求2 | 19个真实生成ID，1.846秒，与请求1 raw完全相同，新请求独立KV |
| 实际运行中取消 | 1536输入prefill时NativeActivity.Active=true、累计Run39；不是仅排队取消 |
| 停止/清理 | 主线程StopAsync耗0.0082ms；Owner安全结束原生调用后Cancelled，Generation=null，再由8248释放模型/文件/线程资源 |
| 协议回归 | 惰性/单Start/复制/忙与未消费结果拒绝/加载故障/执行故障/加载中取消等8项通过 |

本模块没有新产品故障。原始生成仍为F120格式不合规样本，未解析或派发游戏，不把Owner成功当意图质量成功。现只完成资源与有界通信；300ms完成后冷却、请求取消/超时与世界/会话代次分别I02/I03/I04继续独立验证。Editor域/退出与场景宿主协调尚待I04/S01/E，未用短单元测试替代长期生命周期验收。
