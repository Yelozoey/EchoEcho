# S2-P03 通风供电与局部体积验收

2026-10-25；Unity6000.6.7f1、Windows11 10.0.26300、Direct3D11、1920×1080，普通Forward/内部HDR→SDR，显示器HDR关闭、UseLLM=false。开发机i7-12650H/RTX4060Laptop7956MB/16122MB RAM。

A纯回路/三秒状态6/6，B体积配置/边界/真实渲染3/3，C玩家面板/三秒开放/实际移动/Echo只读3/3，均先独立通过并归档。真实半分辨率960×540、16步、2局部光，深度遮挡ROI差0；详见分报告。

D首次构建器类型错误CS0266（F093）修复后，D-PlayerBuild-01成功、0错误、2既有ServicesCore/Windows.Forms警告，333.2376906秒；实际frameTiming设置还原=true。没有关联云服务或改第三方程序集来消警告。

WindowsGPU01可见窗口实际运行并自动退出0，Report.passed=true，51.09秒进程墙钟。四段各120帧全部有效，无无效/重复，480条原始记录保留；先10秒实际Capture/GetLatest预热，每段再60帧预热。只切LocalFogField.enabled以开关对应RenderGraph采样，视点/分辨率/画质不变，关闭输入和动态UI避免交互干扰。

| 段 | 效果 | GPU中位数ms | GPU P95 ms |
| --- | --- | --- | --- |
| 0 | 关闭 | 1.064960 | 1.329152 |
| 1 | 开启 | 1.224704 | 1.473536 |
| 2 | 关闭 | 1.064960 | 1.338368 |
| 3 | 开启 | 1.224704 | 1.550336 |

两轮中位增量均0.159744ms。原始时间/截图/Player.log/Process.json/Exit.json见../Evidence/S2-P03/WindowsGPU01；已检查关闭/开启截图及日志，无Exception/Shader error。475份S1资源/交付SHA不变。该诊断包是特定GPU测试构建，完整S2交付仍待后续模块。

当前结果证明本机实装和有效GPU对照；不代替最低i5-8400/GTX1060 6GB/16GB中1080p60FPS、推荐i5-12400/RTX3060 6GB/16GB高1080p60FPS的实机验收。正常全流程CPU/GC/内存/Draw Calls仍由V03补测。
