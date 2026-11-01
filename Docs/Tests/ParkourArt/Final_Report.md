# ParkourArt 最终实际结果

同日后续已按用户要求追加可操作第一人称Player，详见[PK06实际报告](PK06_Player_Report.md)。Player最终独立2/2通过；追加后的全场回归12/13，旧起跑楼体与路线标记高度差F178仍保留。下文11/11为追加Player前的原美术交付历史，不混算为当前全场13/13。

2026-10-27（Asia/Shanghai）。Unity6000.6.7f1、URP17.6.0、Windows11、Direct3D11；开发机NVIDIA GeForce RTX4060 Laptop GPU。范围为用户本次明确委托的独立TA/场景美术场景，不改S3进度、不推进S4。

## 交付与验证

入口 `Assets/EchoStation/ParkourArt/Scenes/ES_VectorCity_Parkour.unity`，184个Unity原生Cube构件、五主屋顶/十背景塔楼、三上升坡道、护栏、四组跳口和16路线标记。六种纯色PBR材质、可直接使用的Shader、专用Forward HDR/SDR管线、8192/4CSM日光阴影、五份512 HDR区域离线反射Cube、两面2048宽实时平面反射、仅颜色Volume。规划、完整参数、源码位置、结构和微调指令见[交付说明](../../Rendering/ParkourArt_Handoff.md)。

| 实际步骤 | 最终结果与证据 |
| --- | --- |
| 基础几何/碰撞/路线 | PK01 Geometry_C 2/2；原生Mesh、五屋顶Collider、16落点射线、四组跳口距离 |
| PBR | PK02 Materials_C 2/2；实际编译、非黑图像、粗糙度实际差；最终资产精简为6份，由全量回归覆盖 |
| 光照/还原 | PK03 Lighting_Final_B 3/3；8192/4CSM/普通Forward、真实阴影对照、普通及已存美术绑定两种恢复路径 |
| 探针/平面反射 | PK04 Reflections_Final 2/2；重开后五份Cube有效且SHA不同；地面/墙面独立实际对照和资源清理 |
| Volume/自查 | PK05 Finishing_Final 2/2；仅两颜色组件、无表面贴图/轮廓/量化阴影、实际图像对照与三视图 |
| 最终全量 | `Evidence/Final/FullRegression_v2.json`：F175修复后11/11通过，9.8756178秒；原FullRegression的11/11保留，不是目标硬件性能采样 |
| PlayMode检查 | `PlaySmokeActual_v2.json`：真实Application.isPlaying=true、D3D11、VC_ForwardHDR，1080p显式RenderRequest平均亮度.565102339，两平面RT有效且各实际渲染3次，Console0错误 |
| PlayMode退出 | `PlaySmokeAfter_v2.json`：F175修复后原S2默认/PC路径、AA0、播放选项true/1还原，临时相机0/RT0、场景干净 |
| 保护 | `Evidence/ProtectedAfter.json`：1571份既有资产/配置SHA全部相同，0改变/0丢失；首Play后置失败独立保留 |

`Final/Hero_1080.png`、`Runner_1080.png`、`Atrium_1080.png`为最终编辑器实际渲染并人工查看；`PlayMode_1080.png`为播放模式显式相机渲染。不是AI概念图。无外部模型、表面噪点贴图、法线/Detail/磨损AO、描边或Cel步骤。

## 历史失败与限制

完整保留所有首轮失败与拒绝启动的门禁证据。F162/F163为编译与空场景测试前置，F164/F165为截图清理与导入，F166/F168为URP API边界，F167保留公共渲染白名单，F169为Unity API废弃，F170/F171修复真实区域烘焙与重复组，F172修复实际后处理/反射组合黑图与清理，F173/F174验证磁盘绑定与重复配置恢复，F175补齐真实播放后画质索引/AA还原。修复后重跑受影响模块、真实播放与完整11项。

本次交付为可碰撞、带路线布局的场景美术，不包含完整蹬墙/攀爬/翻越控制器。玻璃使用不透明着色反射面，没有折射/透射。没有执行Windows Player构建、目标GTX1060/RTX3060性能验收、完整玩家跑酷手感或长期压力验收；PlayMode一张显式渲染不冒充这些结果。

最终现场：已在Unity打开干净的美术场景，未播放；场景作用域内使用VC_ForwardHDR，D3D11，Shader错误0。`DeliveryWorkspace.json`留档；打开现场后的1571份磁盘SHA仍全部一致。关闭该场景的恢复路径已单独实测，未把临时管线写成原全局基线。
