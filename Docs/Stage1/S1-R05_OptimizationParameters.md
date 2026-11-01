# S1-R05 剔除、合并与SRP Batcher v0.1

日期2026-10-25；依据连续S1授权选择。R05-A12/12、B2/2、C2/2分别通过归档；D最终63/63、Windows退出0与9项审计通过。整体功能通过，目标硬件性能未验收，见R05总报告。

| 内部分块 | 范围与门禁 |
| --- | --- |
| A 静态Mesh合并 | 同房间/同材质/同Lightmap/同渲染状态的显式装饰组；空间变换/UV2/碰撞保留/错误输入独立测试，通过写报告 |
| B 视锥与烘焙遮挡 | 独立双房间夹具；Unity Umbra静态遮挡数据实际烘焙、Camera.useOcclusionCulling；真实相机与绘制证据，通过留档 |
| C 动态SRP Batcher | 移动URP兼容材质代理，无MaterialPropertyBlock；Frame Debugger实际SRPBatch路径；与静态合并分开验证 |
| D Windows与回归 | 独立Player正常帧ProfilerRecorder实际Draw Calls/三角形等计数对照，全部前模块回归、哈希保护 |

## 实施边界

- 合并在Editor离线执行，不在运行帧里合Mesh。以房间为边界，禁止跨房间造成过大剔除包围盒。
- 单组共享一个材质、一个烘焙图集、相同渲染状态；多材质按组处理，不擅自烘成图集。保留源GameObject和Collider，只关闭源Renderer。
- 用目标根worldToLocal×源localToWorld；Mesh.CombineMeshes保留法线/UV，烘焙UV2按lightmapScaleOffset变换，输出offset归一。超过65535顶点用UInt32。
- 未烘焙合并保留输入UV属性，源UV2可能相互重叠；调用方在第一次Lightmap烘焙前须重新展开合并体UV2。W01-B负责此真实接线与验证；不对已经同图集烘焙的合并结果重展UV。
- 拒绝非静态、重复/空目标、不同材质/图集/层/阴影状态、负/零缩放、LODGroup、额外顶点流/属性块、非可读Mesh；在任何对象修改前校验。交互/动态物体不加入显式合并清单。
- 默认遮挡起始值Smallest Occluder=2m、Smallest Hole=.25m、Backface=100；通过序列化/本机API核对实际支持，双房间与小屋分别烘焙。相机视锥由Unity实际剔除，不手写几何隐藏代替。
- 动态SRP Batcher保留已启用项目开关，用URP材质共享、无MPB移动对象实际验证；不把批次数当底层draw数，也不把兼容性标签当执行证据。
- 项目Pipeline/Renderer及R01–04模板保持当前值，不为凑降Draw数关闭后处理/硬约束。独立量化夹具可明确隔离不相关效果并恢复。

参考：[Unity CombineInstance](https://docs.unity3d.com/cn/6000.0/ScriptReference/CombineInstance.html)、[静态剔除标记](https://docs.unity3d.com/kr/6000.0/ScriptReference/StaticEditorFlags.html)、[遮挡排错](https://docs.unity3d.com/ja/6000.0/Manual/occlusion-culling-troubleshooting.html)。以Unity6000.6.7f1本地API与真实数据为准。
