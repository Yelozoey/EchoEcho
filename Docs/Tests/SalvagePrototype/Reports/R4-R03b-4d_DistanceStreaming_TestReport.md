# R4-R03b-4d 主场景距离流送整合

2026-10-30，Distance_A3/3、13.244秒，完整module_tests门禁与Console0；两项真实主场景距离/读档/退出，另回归三房光照对照。

NextRoomStreaming只挂新ES_Salvage_Streamed入口，参考单场景不改。玩家、Echo、当前有效调查/警告任务的World/Generation租约联合计算10m预载/18m滞回；常驻碰撞/Nav不卸载。进入前阻止游戏输入、显示加载状态，读档同步撤Ready；错误状态保留并由Owner限频重试。加载完成保留菜单UiBlocked。运行时全局光图、探针、相机、阴影正确重绑，退出交给清理pump处理晚结果。

实际泊位首载0/1区，第二区初始不加载；玩家/Echo到34/33m，检修区实际加载并开启输入，泊位远离卸载后独有Mesh/Map由产品回收。返回5/3m泊位重新加载。原菜单阻塞始终保留；检修加载耗时和10m/3.5m/s缓冲比较见Distance_*.json，低于2.857秒门槛。不是目标硬件加载证明。

实际新槽快照恢复：Epoch当场增加、旧Ready当场false、输入禁用，只有新代次重新提交才恢复。另在检修区Load阶段卸载整个Main，所有句柄/Bundle归基线，pump等待原生回收后销毁，无错误；原生lightmap呈现用例回归通过。

多Scene遮挡数据仍待重建，整段压力/Windows试玩/Profiler和目标硬件性能尚未验收。
