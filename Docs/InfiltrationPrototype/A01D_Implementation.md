# A01-D 实现与维护

本轮场景、派生预制体/材质/光照/导航均在Assets/EchoStation/InfiltrationRefinement，源模型/原包和A01场景保留。运行时复用已有LevelPreview、玩家输入/移动、雾与导向展示；RefinementDecoration和RefinementLightSurface仅为可检查的标记组件，没有AI/交互权限。无新依赖、网络、训练或构建。

RefinementScene负责严格自有目录复制/保存、附加编辑与导航；RefinementAssets处理10类源模型的根旋转/尺度/PBR映射；RefinementDressing布置78件、桌面支撑/墙面净空与导航；RefinementLighting设置50发光面、顶窗结构、分区粗糙度和8区域反射；RefinementBake使用限定SaveScope、异步烘焙和宿主恢复。普通Forward/HDR专用Renderer/Pipeline/Volume/LightingSettings位于Generated/DR_*，不会把旧模板绑定永久替换。

最终重要参数：曝光.55/ACES/Bloom.16，36顶灯＋6壁洗烘焙灯、Mixed主光强度1.9/旋转52,-35；实时2盏且仅1盏阴影；2级CSM/2048/40m。SSAO和3盒半分辨率16步雾沿用已有实现；本轮新增灯面而非把共用图集整块改为发光。HDR仅内部缓冲，SDR输出。6区地面Smoothness .30/.32/.22/.40/.20/.34。

已有交付资产可直接打开，不要为试玩重新生成。工具片段在Tools/InfiltrationPrototype/A01D；所有版本收据采用新标签，禁止覆盖历史。重建应在新的隔离目录/清洁测试宿主做：提取已校验原包所选文件→Prepare→校验图集→只对自有缺UV2网格副本执行repair_uvs.cs→摆放/RefreshProps与导航→Polish解除继承旧LightingData→独立Bake/区域反射→模块测试。Prepare会重建预制体，不能在已修UV的交付资产上随意再运行；downsample_new_data/optimize_mask也是单次转换，当前已是1K，守卫拒绝重复覆盖。原作者ZIP和摄入SHA不变，非重新手绘。

以实际收据为准：SelectedModels/SelectedTextures记录来源；PreparedProps_C与LightmapUvs_A记录比例/UV；Refreshed_E和独立导航/真实CharacterController证明通行；LightingPolished_A、BakeJob_A及.scope、ReflectionBakes_A记录新光照与保存边界。报告D01/D02/D03区分工程、图像检查、性能和待本人视觉反馈。

统一测试入口Tools/MCP/module_tests.py，新鲜SceneSafety/RenderSafety/Console门禁；临时设置最终按NativeBefore_A的native/disk/dirty逐项恢复，而不是只改管线引用。不要用全局SaveAssets保存未知脏资产。遇相似故障先看Troubleshooting F-352–359和此前F-338/351。
