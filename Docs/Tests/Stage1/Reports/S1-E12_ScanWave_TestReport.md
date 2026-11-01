# S1-E12 全屏扫描波纹验收

2026-10-25，A状态3/3、B真实深度图像1/1及遮挡补测1/1、C1真实扫描事件1/1按顺序通过并分别归档。C2 Windows构建Succeeded/0错误，实际1080p D3D11 HDR/SDR播放器图像通过、退出0、338个有效CPU/GPU普通帧样本。受影响回归8/8通过（含E08实际扫描、E11表现、新波纹状态/图像/事件），AffectedRegression.json。

实现单并发6m/.18m/1.2秒球壳，真正读取相机深度重建世界表面，蓝环只附着可见匹配半径表面。实际地面蓝增量.30364，远处/天空/前景遮挡后的地面增量0。FrameDebugger活动时1个全屏DrawProcedural、闲置0；无粒子。相机显式来源避免串台，Echo事件代次启动，取消/失败立即停，成功后自然结束。

当前开发机GPU区段中位OFF .4526/.4531ms、ON .4772/.4777ms，增量约.0246ms，仅小型夹具诊断。正式小屋集成/目标配置稳定60FPS不在此结果内；最低i5-8400/GTX1060 6GB/16GB中画质、推荐i5-12400/RTX3060 6GB/16GB高画质1080p未实机验收。

Unity6000.6.7f1/URP17.6普通Forward，开发Win11/i7-12650H/RTX4060 Laptop/16GB。新Gameplay管线/Renderer变体仅加波纹，原13保护文件SHA一致，基线默认/Quality绑定还原。故障F055已修复并重新编译/测试；F011依赖警告、F019禁用后处理剥离警告如实保留，P06须验证正式景深Shader。证据/图像/逐帧计时/构建日志见Evidence/S1-E12。

2026-10-25追溯补充：P03发现F061实际默认/PC绑定为空，旧还原结论缺少独立后置检查，最初漂移时点未证实。已为诊断Awake/临时覆盖清理加门禁，恢复确认基线；P03/E12-RestoreCycle真实图像1/1通过且RenderSafetyAfterE12Cycle.json明确证明默认/Quality/实际均ES_PC_ForwardHDR、普通Forward/HDR/SDR/D3D11。此后置证据取代先前仅根据finally代码推断还原的结论。
