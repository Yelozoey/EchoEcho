# PK06 第一人称进场查看

2026-10-27。用户明确要求给已交付跑酷场景添加Player，以第一人称进入查看，可复用现有实现。

单模块PK06：复用现有PlayerInputReader和FirstPersonMotion的输入/视角逻辑；独立VectorCityFirstPerson使用CharacterController，步行4.5m/s、Shift8m/s、跳高1.6m、重力18m/s²。现有S1 Motor的3m/s/.85m跳高不适合此场景1m高差，因此不修改S1常量，另建本场景移动组件。无额外谜题、AI或攀爬系统。

出生X/Z=(-22,-25.5)、Y通过当前屋顶碰撞射线确定为实际地面上方.10m，眼高1.6、FOV70，面向主路线。Hero相机保留但禁用，Player相机为唯一启用的MainCamera，继续原颜色Volume/HDR/SDR/Forward。Esc释放、左击重捕获；R回起点，跌至y<6自动复位。角色不增加可见模型，不改已有材质/几何/反射烘焙。

验证：干净场景和新鲜RenderSafety的module_tests入口；实际PlayMode虚拟输入移动、鼠标、跳跃/落地、护栏碰撞、Esc/重捕获、R/掉落复位、第一人称非黑图像；完整场景回归；输入设置/管线/AA/播放选项及保护SHA还原。实际失败归档Troubleshooting，不将这些测试冒充完整跑酷手感或目标硬件验收。
