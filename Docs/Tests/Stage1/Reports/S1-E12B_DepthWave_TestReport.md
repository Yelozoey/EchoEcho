# S1-E12-B 深度全屏波纹实测

2026-10-25，Unity6000.6.7f1/URP17.6普通Forward内部HDR→SDR/D3D11，1080p。开发机Win11/i7-12650H/RTX4060 Laptop/16GB；目标中/高画质60FPS硬件尚未验收。

ScanWaveImageTests独立1/1通过；增补真实前景遮挡后再测1/1通过（B-WaveImages、B-WaveDepthOcclusion.json）。实渲染开关差.001185832、1.5→3m半径图像差.006167123；预期波壳地面蓝通道增.3036415，远处0、天空0。放置前景不透明实体遮住该地面像素后增量0，未穿透显示隐藏地面。

真实FrameDebugger活动DrawProcedural/Echo Scan Wave恰好1个，闲置0个；Shader用场景深度重建世界坐标，不使用粒子。WaveOff、WaveRadius1_5、WaveRadius3、WaveOccluded.png与WaveImages.json/FrameDebugger原始记录同目录保存。查看图像可见地面蓝环随空间半径扩散。

新增ES_Gameplay_ForwardHDR/Renderer独立变体，测试临时绑定后还原默认/Quality绑定；原基线13份保护文件不主动修改。当前独立场景证明效果/深度/提交路径，不证明完整产品GPU预算；C继续真实Echo事件与Windows普通帧诊断。编译故障F055已记录并修正后测试。
