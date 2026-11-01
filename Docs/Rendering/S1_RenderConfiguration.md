# S1 渲染配置汇总

Unity6000.6.7f1 / URP17.6.0。原 PC 模板与 R01–R05 基线资产保留；Demo 当前使用专用 ES_Gameplay_ForwardHDR / ES_Gameplay_ForwardRenderer，在已验收基线上附加 E12 扫描 Feature。

| 项目 | 当前值 / 实际验证 |
| --- | --- |
| 路径 | 普通 Forward，非 Forward+；仅 D3D11 |
| 内部 / 输出 | supportsHDR + Camera.allowHDR；显示器 HDR 关闭；ACES 到 SDR |
| 分辨率 | 1920×1080 基准；窗口/无边全屏及分辨率按钮 |
| Render Scale / MSAA | 1 / 1（无 MSAA），当前继承快照；完整质量预设仍 S4 |
| SRP Batcher / Dynamic Batching | 开 / 关；动态 Echo 兼容材质，已有实际提交证据 |
| 静态几何 | 按房间/材质/Lightmap 约束离线合并；小屋30源网格→8合并体，保留碰撞和UV2 |
| 静态照明 | CPU Lightmapper，20tex/m、1024图集上限、2反弹、64/256/64采样；Demo2方向图集/141探针/8有效合并体 |
| 实时光 | 手电Spot6/range20/55°/Soft Tier2；扫描Spot3/range8/70°/Tier1；故障Point2/range5/Tier1+频闪 |
| CSM | 2048、4级、距离40m、分割.1/.25/.5、边界.1；距离投影30m关/28m恢复、.1s轮询 |
| Volume | 独立层8；HDR LUT32、ACES、Thin1胶片颗粒.10/响应.80、色差.02 |
| 目标景深 | 原生Bokeh、50mm、f5.6、5叶片；有效交互射线.35–2.5m，.15s光圈进退、焦距10/s平滑；独立优先级20 Volume，UI Overlay保持清晰 |
| 扫描波纹 | 深度重建单全屏三角形，1.2s/最大6m/宽.18/强度.35，蓝色(.08,.4,1)，空闲0提交，无大量粒子 |
| 粒子预算 | 公共4系统×64=256，固定预热/不扩容；仅夹具验证，当前产品场景无粒子 |
| 剔除 | 原生视锥 + 新场景独立PVS5728字节，墙体遮挡；Editor可见图差0、Opaque提交4→1（非Draw Call数） |

R01–R05、W01/W02、E11/E12/E13及P06实际报告提供字段/图像/Windows证据。继承的其他渲染器参数不自动算调优验收；例如 SSAO 的完整质量档选择留 S4。高/中/低画质、Addressables五区距离流送、隧道/实验室体积雾与体积光仍在后续规定阶段。当前未引入植被/碎石资源，不声称已有其LOD/Impostor/风Shader成品。

所有目标硬件验收仍按 Win10/11 64位、1080p：最低i5-8400/GTX1060 6GB/16GB中画质60FPS；推荐i5-12400/RTX3060 6GB/16GB高画质60FPS。当前开发机为Win11/i7-12650H/RTX4060 Laptop/16GB，普通帧记录只能说明该机与当前原型参数。
