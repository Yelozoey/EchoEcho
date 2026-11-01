# S2 五区灰盒渲染配置

2026-10-26。Unity6000.6.7f1/URP17.6.0；当前S2专用`Station/Generated/ES_S2_ForwardHDR.asset`和`ES_S2_ForwardRenderer.asset`，保留S1及PC模板。以下为实际资产/场景，不把继承参数都称为性能调优。

| 项目 | 实际配置与证据 |
| --- | --- |
| 管线/API | Renderer m_RenderingMode=0普通Forward；D3D11；S2默认/PC/实际绑定经门禁及Windows核验 |
| HDR与显示 | supportsHDR+Camera.allowHDR，ACES→SDR；显示器HDR路径关闭；1920×1080基准，窗口缩放/全屏实际通过 |
| 尺度/抗锯齿 | RenderScale1、MSAA1；沿用原型配置，高中低正式预设在S4 |
| 深度/颜色/SRP | 深度和OpaqueTexture开，OpaqueDownsampling1；SRP Batcher开，DynamicBatching关 |
| 主要阴影 | 主光2048、4级CSM、40m、分割.1/.25/.5、边界.1，Bias.1/Normal.5；距离投影30m关/28m恢复/.1s轮询 |
| 附加光 | 每物体4盏，阴影图2048，Tier256/512/1024；实时手电/扫描/故障灯沿用S1已验收模板 |
| 烘焙 | ES_Station_Lighting，12tex/m、1024图集、Padding4、64/128/64采样、2反弹、Shadowmask；2方向图集/325探针/13有效静态接收器，实际读回/禁用烘焙源对照见W06 |
| 合并与剔除 | 按房间/材质合并并保留Collider/无重叠UV2，不跨门墙；原生视锥及独立已烘焙PVS；W06的Opaque SRP batch3→2是批次数，真正Draw Calls另见V03 |
| 动态物体 | Echo/玩家不设静态；URP兼容材质与BlendProbes；3门独立碰撞+NavMesh雕刻，解锁权限不由Echo写入 |
| Volume | 复用S1专用Volume：ACES、Thin1颗粒.10/Response.80、色差.02、LUT32；交互景深Bokeh50mm/f5.6/5叶片，UI Overlay清楚，见S1 P06与S2 Windows截图 |
| SSAO | 当前S2 Renderer实际开启，Intensity.4/DirectLighting.25/Radius.3，继承值；受控图像验证必须控制这个Renderer的Feature |
| 局部体积 | A区Bounds中心(0,-2,30)/尺寸(12,4,16)，通道中心(0,-2,48)/尺寸(5,4,20)；半分辨率960×540、16步、最多2灯，深度截断+4tap合成；供电3秒Lab密度.025→.008、Vent.17→.015，体积只在两个盒内积分 |
| 山谷/相邻视角 | 山谷不提交局部体积；临近坡道/核心相机可看见盒内雾，盒外空间不造雾。ShouldRender是相机开销门禁，不是体积边界 |
| 扫描/粒子 | 深度全屏蓝波纹，1.2秒/6m，空闲不提交；无大量粒子。公共粒子池4×64=256既有预算，当前灰盒不引入无用途粒子 |

P03原生GPU对照480/480有效、两轮中位增量约.160ms，仅开发机局部效果；整场CPU/GPU/GC/内存及真实Draw Calls见[性能文档](../Performance/S2_Performance.md)。V01的55场景/导航和V02两原生进程分别验证当前配置/玩法，不代替最低/推荐硬件的60FPS。

S2为单场景灰盒。Addressables距离预加载/卸载及贴图/Mesh/Lightmap释放、高中低正式档、四区正式美术在S4；当前没有植被/碎石新资源，不宣称已有其3LOD/Impostor/风Shader成品。所有新装饰引入时必须同步履行硬约束。渲染故障与回归见Troubleshooting，不通过关闭已要求效果来消警告。
