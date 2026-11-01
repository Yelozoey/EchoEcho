# S2 场景与资产补充规范

沿用S1预制体/命名/许可规则。S2资产集中在Assets/EchoStation/Station/{Scenes,Generated,Runtime,Editor}，场景ES_Station_Greybox，生成导航StationNavMesh，自己的LightingData/Lightmap/PVS和Forward管线。不复用可被烘焙器改写的S1导航/场景数据；源S1资产只读，保护清单475文件逐字节校验。

五区稳定ID：valley/cabin/lab_a/vent/core；保持连通顺序与专用Agent。唯一交互ID来自WorldTarget/catalog，显示名可本地化，不能从GameObject.name猜ID。三个门、设备、线索不得合并进静态几何；禁用中的终端数据只在授权后激活，TargetRegistry不能暴露其原文。

每个MonoBehaviour独立同名.cs，避免StationGate等内嵌第二组件脚本GUID丢失（F090）。Runtime按asmdef直接声明所用类型的程序集，不依赖传递引用（F069/F083）。纯Memory/Dialogue/Story/Puzzles/Persistence数据层不引用Unity；Scene事务在Station层。测试CLI仅非Editor且存在明确参数时创建，无参数正常游戏不得运行自动操作者。

隐藏标记用7顶点箭头Mesh与简单发光材质，显现只允许真实扫描或校验后的持久化恢复；不制作新手绘贴图。静态房间按材质合并、独立UV2、保留Collider；动态机器人轮子/探头/灯挂点沿用S1。S2灰盒复用既有PBR/中文字体与许可，无新增大资源包。

测试报告位于Docs/Tests/Stage2/Reports，原始回执/截图/逐帧/隔离档位于Evidence。失败标签永久保留，复测使用新标签/目录；不得用缺少job_id、0个测试、构建成功或字段设置代替实际验收。Player构建分别为Builds/Tests/S2-*和Builds/S2，不覆盖Builds/S1。
