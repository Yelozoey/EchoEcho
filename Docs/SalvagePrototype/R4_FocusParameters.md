# R01c-2 交互景深

2026-10-30，N08普通实现；c-1已独立通过。新场景专用global Focus Volume priority20，默认weight0；独立Profile只有Gaussian DepthOfField，非Bokeh。重用InteractionFocusState的0.15秒渐变；只在CanPlay、空手、Carry.Focus实际选中可搬货物时激活，距离取视点到本体Bounds最近点并按已有State夹到0.35–2.5m。GaussianStart=焦距+0.75m、End=焦距+4.5m、maxRadius0.85、HQ采样关，目标自身保持清晰，远景轻微模糊。Low关闭；菜单、手持、失去目标渐出，禁用立即清零。

组件只改运行时Volume副本，不写共享Profile，不改交互／物品／Echo。相机要求Depth Texture。实测真实射线选择／渐出／菜单／Low／手持、实际图像有无DOF差异与共享参数保护；退出销毁实例及其克隆组件。尚无新版雾，本项不提前开发c-3。
