# S1-R04 实时光与阴影参数 v0.1

依据2026-10-25连续S1授权实施。状态：A13/13、B2/2独立通过；C最终47/47与Windows Player退出0，交付审计通过。见实际报告；目标硬件性能未验收。

模块内顺序：A配置/可复用距离策略与独立单测 → B实际CSM/实时光/停投渲染测试 → C Windows Player与既有回归；每步通过留档后再开发下一步。

| 参数 | 数值/实现 | 验证 |
| --- | --- | --- |
| 主光 | Realtime Directional，软阴影 | 实际灯开关、阴影开关图像对照 |
| 主光阴影图集 | 2048，4 cascades | GPU矩阵/图像级联与绘制记录 |
| 最大阴影距离 | 40m | 跨距离采样 |
| 四级累计分割 | 0.10 / 0.25 / 0.50 | 约4/10/20/40m视锥深度；真实包围球/矩阵以引擎实测为准 |
| Cascade Border | 0.1 | 连续移动、边界图像检查 |
| Depth / Normal Bias | 0.1 / 0.5，继承 | 不为本测试擅调偏移 |
| 附加灯图集/档位 | 2048；256/512/1024，继承 | 手电/扫描Spot高档，故障灯中档；实时灯开关渲染 |
| 物体投影距离 | 默认30m，重新开启28m | 相机到Renderer.bounds最近点；2m滞回防边界抖动 |
| 更新策略 | 0.1s检测、指定目标相机 | 不为每物体每帧Camera.main查找；禁用时恢复原ShadowCastingMode |
| 权限范围 | 只改变Renderer投影，不隐藏物体/不关receiveShadows | 远物体仍可见；回近处恢复TwoSided/ShadowsOnly等原模式 |

复用预制体：Rendering/Prefabs/ES_Flashlight（Realtime Spot，白色、Intensity6/Range20/Angle55、Tier2=1024）；ES_ScanLight（Realtime Spot，蓝色.2/.55/1、3/8/70、Tier1=512）；ES_FaultLight（Realtime Point，橙红1/.23/.07、2/5、Tier1=512）。均软阴影；故障灯FaultLightFlicker周期.8s，低段为原强度8%，禁用组件恢复原强度。不是同时启用全部灯的性能授权；场景按实际预算复用。

真实CSM GPU矩阵测得105.15/42.69/21.45/10.75像素每米；四级GPU索引图像均有有效像素。Frame Debugger主光+Spot夹具实际阴影提交SRPBatch为4→0；详细单draw数据不可用，不混淆两种计数。

CSM每个级联的图集像素分块通常同尺寸；近级联覆盖更小世界区域，得到更高世界空间采样精度。这不是声称URP支持逐级联任意不同贴图分辨率。实际密度与边界必须测量。

共享项目Pipeline仅修改m_ShadowDistance、m_Cascade4Split、m_CascadeBorder，其余继承输入继续受R01漂移检查保护。Renderer、原模板、R01/R02/R03场景不改。单独R04夹具不算正式小屋。

参考：[Unity配置级联](https://docs.unity.com/en-us/engine/6000.0/manual/lighting-overview/shadows/shadow-realtime/shadow-cascades/use)、[Unity阴影分辨率](https://docs.unity3d.com/ja/6000.0/Manual/urp/shadow-resolution-urp.html)；准确API/运算以本机URP17.6源码为准。
