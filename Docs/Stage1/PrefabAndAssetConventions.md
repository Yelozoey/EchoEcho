# 场景预制体与资源命名规范

范围S1；后续模块继续沿用。Unity单位1m、Y向上、Z向前；根scale=1，应用尺寸到Mesh或子节点，碰撞体按实物边界。资产位于Assets/EchoStation/{Rendering,World,Echo,Player,UI,Data}，第三方原文件位于Assets/EchoStation/ThirdParty/PolyHaven，许可与源清单在Docs/Assets。

资源名ES_用途_变体，场景ES_区域_用途，房间根Cabin_Room_Main/Storage/Foyer。合并体ES_Combined_房间_材质；Texture以_BaseColor/_Normal/_Mask区分。唯一交互ID由W03管理，不从可改显示名或GameObject.name推导。

静态几何：ContributeGI/OccludeeStatic，实墙额外OccluderStatic；同室同材质/同Lightmap组离线合并，保留源Collider，源Renderer禁用。不跨门/墙合并多个房间，不将可移动/可交互物体合入静态体。合并体移除BatchingStatic避免重复静态合批。首次烘焙前重新展开无重叠UV2；已烘焙offset合并不能再改变图集坐标。

动态物体：无静态标记，URP兼容材质，BlendProbes，避免MaterialPropertyBlock破坏SRP Batcher。动画部件挂点明确，行为脚本只持有授权接口。灯模板来自R04。粒子接E13池与并发预算。

房间prefab包含单房间边界/静态网格/源Collider，只作编辑复用模板。场景负责烘焙LightingData/PVS/Lightmap绑定、区域Volume/灯/导航与玩家；新增实例必须在自己的场景重新烘焙，不复制旧场景图集索引当有效照明。

每个第三方资源有官方资产页/作者/许可/获取日期/文件哈希。仅纳入免费且允许项目用途的资源；不能把平台全部资源视为同一种许可。原资源不得出现粉色、缺Shader/Mesh或未使用的大包。PBR通道、sRGB、Normal类型、MipMaps、分辨率/压缩逐资产验证。引入植被/碎石时同步承担GPU Instancing+3LOD+远距Impostor要求。
