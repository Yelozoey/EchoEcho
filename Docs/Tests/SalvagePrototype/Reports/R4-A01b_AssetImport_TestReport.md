# R4-A01b 静态设施资源适配

2026-10-30，最终Import_B实际7/7通过、0失败、0跳过、5.409秒。Unity6000.6.7f1／Windows开发机／D3D11；仅资产兼容门禁，不是完整关卡／动画／烘焙／FPS验收。

从已校验Standard复制26 FBX与所需PNG／CC0许可，源字节逐项SHA不变。11个项目URP Lit材质统一冷蓝灰，实际ORM→金属／遮蔽／平滑度通道逐像素验证，法线与1024纹理上限／Mip／线性色彩检查通过。未知材质明确中止而非默认错配；两次止步及修复F228／229保留。

全部26派生Prefab有非空网格、法线／UV、正确材质槽和持久基底／法线引用，主要静态模块有UV2，最大边≤10m。原带骨骼的Chest／Fan已静态姿态BakeMesh(true)，无运行骨骼动画；源装饰光锥三角在派生模型排除。视觉Prefab不自动添加整框Collider，避免封死门洞；关卡碰撞／导航在后续A01e独立验收。

首次Import_A6/7因运行时whiteTexture非持久绑定失败，修复为自有2×2白数据贴图，不放宽断言。人工看图又发现静态皮肤被100倍缩放，虽然非黑帧数值通过却无效；F231原生BakeMesh true／false对照定位并修复，新增尺寸／镜头在模型外／七物体入镜断言。原坏图、原材质／Import_A与Probe留存。

最终真实HDR 1920×1080图Bench_bccffa3a4e784e969a2cfdfd984b0cc9.png与同名JSON已人工复核：多个场景部件形态可辨、无粉材质或巨型遮挡。当前是隔离画廊，部分金属正面因未布置正式环境反射显暗；不把它当最终场景照明。实际使用已确认S2普通Forward资产、内部HDR／SDR，后续新场景专用渲染与光照仍待R01实测。

证据Evidence/R4-A01b/Import_B.json、Job／SceneSafety／RenderSafety，Geometry_GUID.json、Extraction.json及前版、ModelProgress、MaterialsPrepared、SourceMaterialSlots.txt、SkinScaleProbe_A、BeforePersistentWhite_A。测试结束回原空保存宿主、原相机RT／类型／aspect还原、Console0入口；本轮最终VC／AA4／磁盘恢复尚待所有阶段结束，不在此处写已完成。
