# R4 后续实施工作笔记（非验收）

2026-10-30，N08普通实现委托；均须到对应模块才开发与实测。R01b仍以最新执行状态为准，不因本笔记跳门禁。

R01c建议再细分三步：c-1专用ACES／FilmGrain0.06／轻微Chromatic0.012／ColorAdjustments饱和度-30、曝光+0.25，逐效果真实画面对照；c-2复用InteractionFocusState，按实际Carry射线选择渐入Gaussian远景DOF（0.15秒），菜单／手持退出，独立运行时Volume副本；c-3局部雾。旧LocalFogField／Shader硬编码旧Station坐标，不能直接绑定当新版生效。新版专用参数化三个短连接AABB：(12,1.3,6)、(12,1.3,18)、(24,1.3,12)，尺寸3×2.6×3，初始密度0.06／0.08／0.06，半分辨率16步。4个真实实时局部灯的位置／方向／颜色／启用状态驱动散射；Low关闭、远离跳Pass；实测960×540和Fog／LightStrength开关，不以Feature存在当成功。

R02a拆静态合并与装饰实例化：按房间、材质、lightmapIndex合并所有submesh，CombineInstance带旧lightmapScaleOffset并hasLightmapData，最终scaleOffset identity；重开与实际帧A/B证明UV与光照未变，归档后移除派生源实例，避免仅disabled却留MeshFilter强引用。静态对象LightProbeUsage可Off，动态货物／Echo保持BlendProbes与SRP Batcher。装饰需实际GPU实例绘制、三层LOD与远广告牌，不能只打enableInstancing（SRP Batcher可能优先）；显式RenderMeshInstanced或经FrameDebugger证明的实例Shader。室内可加少量墙侧设备／箱体与Shader驱动的软带，风不挂CPU动画。静态装饰碰撞留主场景并重烘焙本轮自有Nav（先备份原字节／Meta），保护旧R2。池／并发需在实际使用对象验证，扫描继续全屏波纹，不用大量粒子。

R03只在R02门禁通过后装官方Addressables2.11.2，核对本机Registry完整依赖图，不自动升级旧包。主场景保留碰撞／Nav／逻辑货物与全局探针，三区静态视觉／反射／自有Mesh与Lightmap进入本地Addressable场景；到Bounds预载10m／卸载18m，取玩家、Echo、任务区域并集。合并／重分配实际Lightmap槽，卸载先去全局引用再释放Scene handle；共享贴图最后引用才释放。2026-10-30原生反射已证LightmapSettings.lightProbes可写，后续可验证单独复制LightProbes资产，解除Main LightingData对全部Atlas的强引用，尚未试验CreateAsset／独立生命周期。原始烘焙数据保留作证据但不强制随包。每区唯一资源实际释放、快速往返、取消、退出、句柄和过期回执必须实测。

P01实际高中低／分辨率／窗口全屏UI、独立新槽、完整回收路线、跨进程恢复与坏档、Profiler CPU/GPU/GC/RAM／加载时延／内存释放；可见1080p Windows测试已授权。新输出Builds/SalvageNext可在留证后复用，旧Builds/Salvage不碰；No-Go版本不随包带模型，项目原权重保留。3.5m/s和10m缓冲约2.86秒，必须用实际最慢加载验证。世界标签应处理朝向／墙体遮挡和菜单可读性，避免旧TextMesh穿墙显示。最终对照3610保护SHA与20GiB预算，regression_host leave仅R3R4_D，原VC/AA4的原生和磁盘精确还原；未持有目标机与外部首次5分钟时长均待验收。
