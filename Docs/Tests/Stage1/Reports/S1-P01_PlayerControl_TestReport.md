# S1-P01 第一人称控制验收

2026-10-25，Input System1.20.1/CharacterController；A输入/运动规则6/6、B真实控制器3/3依序通过，补实际Esc/左击/光标生命周期后最终Player程序集9/9通过（FinalRegression.json）。各原始作业/首失败/诊断已保留。

专用ES_Player.prefab：高1.75m、半径.3m、中心Y.875、Skin.03、Step.25、Slope45；相机挂点1.6m、FOV70、近.05远60、内部HDR开/显示HDR关/Volume层8。WASD斜向归一化、3m/s，鼠标每像素.1°且不乘时间，pitch±80，Esc释放/左击捕获，UI/失焦阻断。Update最大水平物理步长.1s。

隔离真实物理场景四向60×1/60秒位移各3.00000024m，yaw90前进X=3m，机身不俯仰；虚拟W经真实InputAction/Mono Update移动.500m，失焦及UI阻断各.3游戏秒漂移0。UI点击不捕获、关闭后新左击捕获、Esc释放；禁用还原进入前光标。临时克隆InputSettings只用于后台合成输入测试，TearDown还原/移除设备，不修改产品资产。键鼠输入尚需P09最终Windows整合复核。

F056浮点精确比较、F057跨程序集internal辅助函数、F058编辑器焦点分流均查因并修正，首失败不删改。P01仅水平/视角/焦点，跳跃重力与环境碰撞在P02，手电/交互/日志/存档尚未实现。

Unity6000.6.7f1/URP17.6普通Forward HDR→SDR、D3D11；当前Win11/i7-12650H/RTX4060 Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中画质及推荐i5-12400/RTX3060 6GB/16GB高画质1080p稳定60FPS尚未实机测试。

2026-10-25追溯补充：P03发现F060，旧临时InputSettings替换会销毁默认HideAndDontSave对象，原P01条件还原不能证明身份还原。已统一改用保护/还原对象及flags的SyntheticInputScope，P03/A-RestoreRegression中P01/P02/手电输入18/18通过并明确断言身份/flags，取代旧夹具还原结论；原9项记录不删除。
