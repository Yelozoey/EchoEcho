# S1-E12-C2 Windows深度波纹与开销诊断

2026-10-25，实际Windows Player，Unity6000.6.7f1、URP17.6普通Forward内部HDR、SDR显示输出、D3D11、1920×1080。当前Win11/i7-12650H/RTX4060 Laptop/16GB。目标最低i5-8400/GTX1060 6GB/16GB中画质和推荐i5-12400/RTX3060 6GB/16GB高画质1080p60FPS未实机测试。

最终构建Succeeded/0错误/2警告，24.578s；实际可见窗口运行退出0。打包后WaveOff/On图像差.001186099，WindowsWaveOn.png中可见地面蓝色波壳。随后普通相机帧OFF/ON/OFF/ON四区段，每段60帧预热+90候选，采样期间无ReadPixels/PNG；有效样本338、无效22、重复0。

|区段|波纹|有效数|CPU中位ms|GPU中位ms|
|---|---|---|---|---|
|0|关|85|.4803|.452608|
|1|开|89|.5004|.477184|
|2|关|88|.47575|.453120|
|3|开|76|.5523|.477696|

GPU中位开销约.0246ms；CPU存在区段漂移，不据此作精确差分承诺。小型夹具诊断不代表小屋完整产品或目标硬件60FPS验收。原始每帧时间戳/CPU/GPU在PlayerTiming.json，TimingSummary.json、构建记录、完整Player.log、退出码和图像同目录保留；首轮仅计时版本也保留TimingOnly文件。

构建Services未关联/System.Windows.Forms依赖提示按F011跟踪；播放器四条禁用Gaussian/Bokeh/Panini剥离警告按F019跟踪，当前无启用景深/Panini，P06启用实际景深时必须重建验证，不能把警告当作修复。波纹Shader本身没有缺失/异常。13份保护资产SHA全部一致，诊断构建后默认/Quality管线和FrameTiming设置已还原。
