# R4 Windows交付实施记录

2026-10-30，P01a-3通过后进入P01b。新目录Builds/SalvageNext，唯一入口ES_Salvage_Streamed；旧R2构建/五区版/模型源不动。本轮Development可分析构建，D3D11、默认1080p可缩放窗口、SDR、允许运行时三画质；开启FrameTimingStats。构建只临时替换原生PlayerSettings及渲染/质量绑定，finally逐项恢复并核对JSON/磁盘/dirty。官方SDK保存仍套自有保存范围。

URP构建时临时质量列表包含三套自有管线，防止运行时切换需要的低/中/高阴影与MSAA变体被剥除；烘焙光图保留Plain/Directional，其余不用的GI模式关闭，仅构建scope生效。独立测试正常/异常scope恢复后再正式构建。

本轮默认脚本，打包后仅移除新输出_Data/StreamingAssets/EchoLLM模型副本；工程源/旧构建模型保留。所有删除校验绝对路径在Builds/SalvageNext内。CC0来源和Noto OFL随包附带。

先Windows启动/显示设置，再脚本主路线与另一条返回短路、独立跨进程混合状态读写、实际可见1080p各画质Profiler/CPU/GPU/GC/内存/绘制/房间释放。测试驱动使用专用验证参数和本轮独立目录，不能覆盖用户生产存档/设置。自动移动/输入夹具不代表自然玩家5分钟体验，也不代替目标最低硬件60FPS。

性能采样10秒预热、每区1秒稳定、每组180帧至少160条唯一/非零CPU与GPU。验证作用域临时60FPS/vSync0以与目标60帧基准一致，finally恢复；产品默认帧率未改，报告必须注明采样节奏。CPUFrameTime可能包含帧率等待，主线程/渲染线程工作时间同时保留，不将等待时间当优化收益。
