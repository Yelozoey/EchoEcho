# S1-E10-C 四模式脚本NPC控制器

2026-10-25。C-Brain.json 9/9（危险感知6项复验+控制器3项），C-CooldownRetest.json新增2/2。C通过。

真实小屋/Echo预制体/Native Mono Update：危险面板触发1次自动warn、停止移动，结束恢复follow，短期不重复、位移0；warn中收到新用户wait后持续wait、位移0，不恢复旧follow。脚本Brain以UseLLM=false接通六意图/四子树，answer/refuse不切模式，未知目标拒绝，实际调查扫描隐藏标记后进入wait；手动合法warn结束wait，没有推理/云依赖。

冷却新增实际边界：同危险第二次自动提示间隔8.10343游戏秒（WarningCooldown.json），未提前重复。手动warn后打开自动感知，2.1游戏秒内自动计数0/模式Wait；显式手动再次warn仍允许。首轮手动/自动重复问题见F-053，失败ManualWarningDedup-FirstFailure.json和C-CooldownBoundary.json保留，统一冷却入口修复后2/2。F-052补丁using位置编译错误被门禁拦截后修复。

模型/导航夹具里的Brain不显式Initialize就不运行。自动感知.2秒、每目标自动冷却8秒、32近邻/16射线预算；近目标冷却可过滤且不遮蔽其他可见危险。调查完成wait；自动告警只在没有新用户行为时恢复之前的合法行为。新行为版本号撤销旧恢复记录。

Unity6000.6.7f1/Windows Editor D3D11，当前开发机。初始化测试目标为Transform，不代表正式玩家完成；红灯/机械动画在E11。没有独立产品Windows或目标硬件60FPS验收声明。
