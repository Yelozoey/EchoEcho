# R4-A01c-1 Rover视觉资源

2026-10-30，源拆分4/4后，Unity Visual_A独立4/4、0失败、0跳过、2.990秒。Unity6000.6.7f1／Windows开发机／普通Forward／内部HDR→SDR／D3D11。

原OBJ与PNG／许可／SHA保留；8个派生部件保留704源多边形、全部角点和UV。Unity实测X镜像已同步到轮轴／头部位置；实际8网格合并尺寸与原整体模型×0.25误差<1mm，约0.58×0.54×0.77m、脚底Y0、6轮半径约0.054m，三角总数一致。模型没有Collider、Animator或骨骼CPU动画，传统碰撞留给逻辑Echo。

原256贴图以sRGB／Mip／256上限适配URP Lit，蓝黄红三种HDR灯材质持久化；无面部表情。单独源RoverModelParts只保存传统表现引用，无LLM或世界控制接口。已有EchoRig固定4轴已只读核验，c2将保留其原驱动轴、另驱动实际6轮，不改旧源码。

实际1920×1080图Rover_b0ef1f8137274e0586093f35a0b4c6bc.png及同名JSON已人工查看，轮组／探头／主体及PBR贴图可辨，蓝灯可见、无粉材质／漏面；有独立画廊环境反射。它是静态资源图，不冒充实际跟随／扫描动画或完整场景。

证据Evidence/R4-A01c-1/Visual_A.json、Job／SceneSafety／RenderSafety，Geometry_GUID.json、SourceTests_A.json、ObjAxisProbe_A；原场景和相机RT／aspect恢复、测试入口Console0。c2仍未执行；本轮最终原VC／AA4／磁盘保护检查仍待全部工作结束。
