# P01a-2b3 房间灯具流送整合

2026-10-30，LampStream_A8/8、45.166秒，完整module_tests门禁/Console0。两项实际房间灯具与依赖/释放、光照/PVS/距离/压力六项回归均通过。

三区各6实例，共18灯面，与现有烘焙18 AreaLight对应；无新实时灯/Collider/MeshRenderer，原37个烘焙Renderer与118 Collider保持。RoomPresenter绑定相机，最后引用释放；自有共享组ES_Next_CeilingLights仅Mesh＋两材质，Main依赖图无这些灯资源强引用。实际Bundle对象AssetDatabase路径为空，共享实例相同、原生InstancedMesh记录与开关图像达标；首区卸载保持共享Mesh/材质，最后房间卸载后两者实际销毁、句柄/Bundle归零。数值见Runtime_GUID.json。

本地九包Build_47cc…passed43.766秒，新增灯资源包1,439,860字节；两个构建scope恢复原生设置/磁盘/dirty，旧保存请求均拦截。GPU实例灯不作为PVS遮挡器，固定墙体/37静态Renderer未变，静态因果对照隔离这些灯并实际重跑，不沿用旧结论替代本轮证据。独立原型的HDR3.334证明保留在b2；b3为正式ACES后图像，不误要求SDR色调映射后数值>1。

P01a-1文字、a-2危险和灯具已完成，下一a-3画质/显示设置，之后仍须Windows实测/完整流程/保护还原。尚非目标硬件60FPS或最终试玩交付。
