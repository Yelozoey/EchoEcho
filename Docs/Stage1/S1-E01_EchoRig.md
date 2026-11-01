# S1-E01 轮式Echo灰盒预制体与独立测试

2026-10-25，前置W01–W04均通过。连续授权明确允许先核查免费模型、缺合适资源时采用轮式灰盒。检索发现[Jason_Phillips的Low Poly Rover](https://sketchfab.com/3d-models/low-poly-rover-95750b91a9c042c588e6ab98efee3f31)页面标为CC Attribution/16.2k三角，但未核实适合独立轮组/探头的文件、绑定与导入；官方下载指南访问403。Kenney Robot Pack为2D，不适配本需求。本轮不使用上述文件，也不声称没有其他适配模型；采用已授权原创灰盒，无第三方模型许可负担，场景PBR策略继续沿用W02。

原点在地面，Y上/Z前，root scale1；四轮半径.14m、轮轴X，左右中心±.315、前后Z±.22、Y.14；机身.52×.22×.60m。机身与罩/探头为冷灰，暗灰轮子；总外廓约.72m宽/.85m高/.75m长，小于两处1.6×2.4m门洞。根单BoxCollider覆盖全体，无子Collider，无面部、蒙皮或表情动画。

显式Body/Wheel_FL/FR/RL/RR/ProbePitch/ScanOrigin/Status挂点；扫描方向+Z，探头俯仰X，轮子绕X。Status提供独立蓝/黄/红URP Lit发光材质，E01默认蓝，E11才实现状态动画。所有Renderer动态/BlendProbes/URP Lit、不用MaterialPropertyBlock。只制作可实例化prefab，不提前实现NavMesh、行为树或将临时测试代理放进产品场景。

独立用例：结构/挂点唯一、Collider/外廓/scale；实例化后独立转轮/探头不移动root/Body且ScanOrigin跟随；材质兼容和三种状态色、无Static/粒子/面部依赖；真实PlayMode相机渲染正反两张1080p图，无粉色/整体缺模型。故障留档后再进入E02。
