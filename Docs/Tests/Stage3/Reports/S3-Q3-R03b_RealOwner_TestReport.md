# Q3-R03b 新模型真实Owner取消/停止通过

2026-10-27，R03a纯调度35/35通过后，使用正式LocalQwen3Factory、InferenceOwner和InferenceScheduler实际测试，不加载旧权重、不使用假生成结果。

Unity6000.6.7f1，作业707f592ef43e4f9991c11a86816797b2实际1/1通过，27.980秒。Owner422冷11.072秒；两个正常请求均真实生成follow且完整C#准备线程为Owner，忙碌/未读取结果拒新请求、299/300ms边界验证。对接近输入预算的实际请求观察nativeActive=true后CancelCurrent用时0.8294ms，迟到结果Generation=null，取消后下一请求恢复同一真实输出。

再次观察nativeActive=true后StopAsync用时0.5901ms，主线程未Join/Abort或释放native；最终Stopped/CleanupComplete，取消结果无Generation，releaseOwner422。清理在UnityTearDown异步等待并记录，避免外部日志中止后遗留未排空Owner。证据Evidence/S3-Q3-R03b/Owner_A-Job/Owner_A.json、安全及Owner_639286674320227960.json。

该叶证明新模型接入现有Owner调度路径的取消/恢复/停止，非30分钟压力或游戏性能。下一步J01解析/语法/生成器纯总复核，归档后才进入J02，所有模型原始语义错误保持计错。
