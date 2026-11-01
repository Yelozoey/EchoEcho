# S1-E11-A 状态与机械表达独立测试

2026-10-25，A-Presentation.json 4/4、0失败/跳过，.690秒。A通过，允许B实际图像/SRP与C实际行为绑定验证。

共享蓝/黄/红材质切换、.6秒黄灯亮灭相位；正转/反转轮角符合speed/.14m半径、不移动root/Body；思考探头实际偏转、平静回中。Scanner持有探头时表现层完全不改其已设旋转，扫描实时光开启；退出扫描光关闭。

实际嵌套R04扫描Spot，Realtime/Intensity3/Range8/Angle70/Soft Shadow/Tier1，挂在ScanOrigin；动态URP Lit/BlendProbes、无Static/MPB/Animator/蒙皮/粒子。测试在隔离PreviewScene直接驱动表现帧，未宣称已证明实际灯色像素或行为自动绑定，这些在B/C完成。

Unity6000.6.7f1/URP17.6/Windows Editor D3D11，当前开发机，无新增故障或目标硬件FPS声明。
