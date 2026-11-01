# S1-E09 待命子树验收

2026-10-25，Wait.json 3/3，0失败/跳过；本模块功能通过，允许E10。

计数端口验证进入即停止、持续100次Tick无Move，底层意外Moving再次停止；切换回真实FollowNode后旧Wait不再停止新路径。待命持续Running保持wait，不擅自跟随或扫描。

真实小屋/E04 Dispatcher/E05根/EchoNavigation：Follow实际移动.50017m后接Wait，清路径/StateIdle，目标持续变换并观察.75游戏秒，位置漂移0；模式始终Wait。新Follow命令后实际继续移动.30041m。ActualWait.json保存实测；扫描器同时绑定真实根模式，wait退出follow自主扫描通道。

源码WaitNode.cs。没有增加第5行为、门/机关动作或玩家输入。Unity6000.6.7f1/Windows Editor D3D11，当前开发机；无新增故障，复用F-047实际时间观察/F-048模式切换门禁。无独立产品Windows或最低/推荐硬件60FPS声明。
