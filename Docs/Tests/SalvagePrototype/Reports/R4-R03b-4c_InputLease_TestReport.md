# R4-R03b-4c 加载输入租约

2026-10-30，Input_A4/4、5.352秒，实际PlayerInputReader/输入Action/光标状态，完整module_tests门禁通过。原捕获游戏输入加载后恢复；加载中打开菜单的UiBlocked不清除、鼠标不抢回；原已禁用输入不擅自启用；失焦不捕获，玩家销毁后Dispose安全且重复Dispose无副作用。

租约只暂时禁用游戏输入组件，不写菜单共享UiBlocked标志。此项不代表主场景距离流送已完成，下一步挂载NextRoomStreaming并真实包跨区/读档/退出验证。
