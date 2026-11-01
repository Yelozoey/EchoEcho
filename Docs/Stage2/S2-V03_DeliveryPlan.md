# S2-V03 性能与交付细分

2026-10-26，V02已归档通过后实施。V03-A：独立Development性能构建，临时开启FrameTimingStats、finally恢复原值；五区固定机位各300普通帧，预热10秒、转区稳定1秒，实际FrameTiming CPU/GPU/主线程/渲染线程/Present等待、Profiler GC/总已用内存/Draw Calls，保留逐帧与Profiler.raw，不把截图读回计入普通帧。探针位置是基准夹具，不冒充完整行走；V01/V02已有真实路线。

V03-B：实际Camera.useOcclusionCulling关/开两轮，当前S2 Renderer控制SSAO/颗粒/色差/景深等时间因素的状态前后恢复，实际Draw Calls对比，图像非黑且一致；不得把SRP opaque batch数当Draw Calls。没有收益则保留实测并定位，不编造收益。

V03-C：无测试参数可正常启动的S2 Windows交付构建、字体许可、独立目录；新进程冒烟（启动、移动、UI及隔离读档），S1保护核验；README、完整玩法说明（剧透另页）、源码架构、渲染参数和Profiler报告、DevLog与30模块索引。阶段完成后停止，不接S3/S4。

开发机仅作诊断。最低i5-8400/GTX1060 6GB/16GB中画质1080p60，推荐i5-12400/RTX3060 6GB/16GB高画质1080p60均待目标机器。25–40分钟首次体验需要人类首次游玩计时；自动操作者已知答案，不能据其113秒宣称时长达标。正式三档/Addressables/四区美术属于S4，当前灰盒不提前宣称成品或Steam发布就绪。
