# S1-W02 免费PBR资源与预制体规范

2026-10-25，v0.1。依据连续S1授权选择资源；前置W01验收通过。内部顺序A许可/下载完整性 → B材质/UV/预制体配置测试 → C实际重新烘焙/图像与回归，每步通过归档后推进。

A选择Poly Haven的concrete_floor_02（地/顶）与painted_plaster_wall（墙），1K Diffuse、OpenGL Normal、ARM。官网CC0允许商用/修改/随项目分发；保留作者、来源链接、获取日期、官方许可快照、原文件MD5及本地SHA256。公共API仅在开发下载工具使用，添加明确User-Agent并在文档保留Powered by Poly Haven；运行时离线，不访问API。

B使用URP Lit，BaseMap sRGB，Normal线性/NormalMap，ARM转Unity遮罩R=Metallic、G=AO、A=1-Roughness，避免将ARM直接接入Unity错误通道；1K、MipMaps、Repeat、压缩、运行时不可读。保留冷灰蓝tint、普通Forward/HDR/SDR、同室合并/Collider/Lightmap UV2；重新生成按米平面UV0、保持UV2和几何不变，2m一重复，避免灰盒Cube贴图被整墙拉伸。

静态房间prefab只包含对应房间内几何/合并体/Collider，不含灯/相机/交互ID/烘焙场景状态。烘焙数据归场景，禁止把lightmapIndex当跨场景稳定ID。命名与校验见PrefabAndAssetConventions.md。不引入植物/碎石、风动画或额外粒子。

测试：A校验6个下载文件官方MD5/大小与许可覆盖；B验证线性色彩/Normal设置/Mask通道、材质Shader/关键词、8合并体/30Collider/UV2不变、3房prefab无缺失引用/同室边界；C实际重烘焙、三室图像和前置回归。未执行不标通过，不凭材质配置宣称最终美术完成。
