# R4-R01c-1 蓝灰分级与全局Volume

2026-10-30，Post_A联合11/11、12.615秒：本项2＋照明5＋渲染4，Console0。中性灰原文件已在Raw中存在且SHA与官方包一致（NeutralReused_A），没有覆盖或重新下载；仅两个新版静态材质绑定替换albedo，原蓝色／动态货物／源基底保留。重新实际烘焙Bake_f9d5adef558d404cb188aac384d3de9d，142接收器／2Atlas／180Probe、bakeErrors空并还原宿主。

实际1920×1080内部HDR帧逐效果隔离：ACES差0.017815，FilmGrain差0.000033011，Chromatic差0.000143899，均高于原1e-6图像门槛；随机SSAO与实时灯临时隔离后finally恢复，运行时Volume副本销毁，未写共享模板。Profile4组件与参数、源SHA／sRGB／mip／1024上限通过。照明对照额外隔离新增颗粒／色差，保留原Lightmap／动态SH门槛，没有拿噪声当光照贡献。

已查看AllEffects_3f6ad6a080f64e2082eb33886d2640bf.png：蓝灰金属表面，室内仍偏暗，后续装饰、灯面和UI可读性继续处理；本项不是最终美术／Windows／目标机60FPS验收。DOF与新版局部雾本项尚未启用，按c-2／c-3分别开发测试。
