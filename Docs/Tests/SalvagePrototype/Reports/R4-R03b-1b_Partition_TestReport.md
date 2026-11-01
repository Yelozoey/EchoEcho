# R4-R03b-1b 常驻主场景与三区视觉资源

2026-10-30，Partition_A4/4，1.993秒，完整门禁/Console0。

新正式入口ES_Salvage_Streamed，独立复制Streamed_Nav；旧ES_Salvage_Next参考场景和Nav的SHA保持。主保留118个碰撞体、7货物、玩家/Echo/实时灯/UI等，3视觉Room移出；清除旧LightingData和PVS引用，使用独立180探针。播放中SH采样差0，退出恢复原探针。

三个视觉场景初始inactive，共37个合并Renderer；无Collider/Nav/Camera/Light，无跨场景对象引用。InstancedRoomDecor与距离阴影的相机引用已清，未来运行时绑定。每房间2套Color/Direction，共12张独立派生512px纹理；与原EXR逐字节SHA和导入设置一致，付出小额复制成本以支持独立卸载。主场景依赖图不再引用这些房间Mesh、光图、旧整包GI/PVS。

SDK保存范围回执实际仅保存4个新场景，拦截Graphics/Quality设置，所有ProjectSettings字节不变。此项只完成资源/结构拆分；尚未注册房间Addressable组、距离调度、句柄加载、全局光图槽或多场景PVS，不是可试玩流送版。
