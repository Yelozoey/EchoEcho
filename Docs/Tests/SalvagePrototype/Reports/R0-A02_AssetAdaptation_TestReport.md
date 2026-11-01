# R0-A02 两件CC0资产适配报告

2026-10-28；通过。I 9/9、P 5/5、V 1/1逐项独立运行/归档后完成，总15个真实EditMode用例。I首次8/9失败已修复并保留，未跳过用例。不是完整玩法、美术或性能验收。

新增目录Assets/EchoStation/Salvage；两个预制体ES_Salvage_Platform/ES_Salvage_Toolbox，两份URP Lit及独立ES_Salvage_ArtBench场景。原始模型/贴图/CC0许可证随目录留存，源12文件字节SHA一致。地板4m×4m；工具箱约0.40002×0.34710×0.29232m，源模型为打开状态（未制作开合动画）；工具箱总11518顶点/14228三角。真实模型全部子Mesh非空；静态地板UV2，动态工具箱不启用有兼容性故障的自动UV2（F194）。

V：普通URP Forward、HDR内部缓冲→SDR、D3D11、1920×1080。场景真实渲染全景/工具箱近景，无粉材质、小部件可见，人工检查实际PNG。关闭Normal平均绝对像素差0.00051207；关闭MetallicMask并设低光滑度差0.00253497；恢复原材料差0，临时材质/GPU资源finally释放。图像为引擎渲染，非概念生成。工作台只用于材料核验，黑色背景为展示背景，不是空管线黑图。

证据：[V真实测试](../Evidence/R0-A02-V/ArtBench_A.json)、[像素统计](../Evidence/R0-A02-V/PixelStatistics.json)、[近景](../Evidence/R0-A02-V/Toolbox.png)、[全景](../Evidence/R0-A02-V/Wide.png)；I/P详见对应独立报告。module_tests门禁全部新鲜通过。

还原：regression_host恢复原ES_VectorCity_Parkour、原选择/播放选项，native实际VC/AA4；发现磁盘仍临时S2/AA0后按F195核对并持久化原值。最终2403份既有Assets/Packages/ProjectSettings逐文件SHA一致，见[最终保护对照](../Evidence/R0-A02/ProtectedFiles_Final_A.json)。无用户场景保存/丢弃，无旧资产覆盖。

未验收：静态烘焙质量、LOD/Impostor、流送、正式场景、动态搬运、目标硬件帧率。性能基准Win10/11 x64、1080p：i5-8400/GTX1060 6GB/16GB中60；i5-12400/RTX3060 6GB/16GB高60。当前开发机图像验证不能证明基准达标。

后续：用户已确认R1首版规则/3区地图/8模块连续执行，按R1授权逐模块推进，不恢复训练，不跳R2。
