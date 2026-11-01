# R4-R03b-3a 真实场景Bundle与共享资源

2026-10-30，Scenes_A2/2、2.096秒，完整门禁/Console0。ES_Next_Rooms三Scene PackSeparately；36重复Material/Texture/Mesh归ES_Next_Shared PackTogether。全部本地路径，禁止启动远Catalog更新。

实际内容构建Build_c361…成功61.605秒；三场景Bundle约521672/514201/505180B、公共Bundle7207396B，另有内置/脚本Bundle与原smoke。首次文本内容缓存已归档后复用本轮缓存，旧R1/R2构建保留。构建与分组SDK保存均在范围保护内。

PackedPlayMode实际三Scene加载约0.434/0.029/0.023秒；根节点仍inactive。Native Bundle数0→4→5；首区卸载后降4，独有Mesh/Lightmap对象已销毁，第二房间光图与公共材质仍有效。第三房间也实际加载成功并使用相同公共材质；最后场景卸载后Bundle归0、公共材质/剩余光图销毁。资源来自Bundle（AssetDatabase路径为空），不是FastMode。

SDK源码证实SceneProvider卸载完成会消费原load句柄；测试只另行释放autoReleaseHandle=false的unload句柄，没有双Release。本项尚未把视觉根启用或绑定全局光图/相机，没有主场景距离驱动和晚完成清理；性能不是目标硬件证明，0.434秒只是当前开发机观测。
