# R01b 静态烘焙与动态受光参数

2026-10-30，N08授权。18盏静态面积灯（每区2列×3行，Y2.82m，冷白／灰蓝，强度3，尺寸2×0.4m），两个危险位置少量橙红静态辅光与实时故障灯；手电／扫描保持Realtime。主方向光降0.25，环境降0.04／0.055／0.075，使用真实三区反射探针替代明亮摄影棚反射。静态专用材质保留贴图，发光仅弱蓝指示，双面GI与阴影防薄天花漏光；不修改导入基底或原R2材质。

UnityComputeGPU（6000.6推荐的本机计算着色器烘焙；原CPU初值因弃用在F237调整），自动烘焙关，RealtimeGI关，BakedGI开，12 texel/m×Renderer0.5，最大Atlas512、padding4、CombinedDirectional、压缩HQ；Direct32／Indirect64／Environment32，反弹2，Gaussian降噪无Optix依赖。180个动态LightProbe（162室内网格＋18门洞补点），动态货物／Rover实际BlendProbes。三反射探针128 HDR实际离线捕获、Custom纹理持久引用。

只在干净工作区临时单独打开新版场景烘焙，异步等待最多600秒；不让空宿主参与烘焙，不调用全局SaveAssets。完成后仅保存新版Scene和明确自有LightingData并还原场景布局。先实际检查Atlas／UV2／接收器覆盖／探针能量／反射差异，再做静态灯关闭、Lightmap移除与动态探针0／1／2倍图像对照；本轮开发机不能替代目标硬件。

调优记录：Lighting_C／D／F表明数据已载入而画面接近黑，camera.scene单变量无改善，未认定它是根因。原生灯单位实际Candela，初值3cd偏暗；现把18个静态矩形灯调60cd，其余颜色／位置／贴图／门槛不变，重烘焙并重做图像验收。旧低能量完整数据LowEnergyBake_B保留。新故障灯纳入与手电／扫描灯相同的动态阴影tier预算（共4个实时局部光）。

Lighting_G6/7：动态SH单调通过，静态图像仍黑。MaskMetalInspection_A实读Trim01／02约99.8%像素metal=1，纯金属几乎无漫反射GI，封闭室内需涂装表面参与。只为ES_NextLit静态材质派生Mask，Rmetal×0.15，G/AO、B、A/smoothness逐字节保持；源导入基底、动态货物材质保持。以源法线／粗糙度／AO和涂装金属度适配柔和蓝灰，不靠增环境光掩盖Lightmap接收；重新烘焙与实际图像复测。前版MetalSurfaceBake_C保留。

Lighting_H8/8功能通过后，人工西墙受光未通过，追加F242面朝向修正与双面隔墙几何。两侧各0.15m位于原Box厚度内，完整碰撞／Nav不变；外围西／北正面朝房内。四向实际墙面画面中心平均RGB须>0.001，保留其余原图像门槛，重烘焙重测；不会以Cull Off参数代替背侧烘焙验证。

同次正式烘焙前，将旧回收点的平蓝方块视觉替为3×3m的现有PBR CenterPlate，采用同一静态涂装材质，放Y0.081贴原0.08高Box顶部；Box／DropSurface／交付半径与位置不变。视觉归泊位RoomVisual参与烘焙，旧灰盒Root只保留低成本逻辑碰撞。
