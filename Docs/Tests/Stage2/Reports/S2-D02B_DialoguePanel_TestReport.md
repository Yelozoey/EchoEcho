# S2 D02B 面板与模态测试报告

2026-10-25通过。Unity6000.6.7f1、Windows11开发机，普通Forward/HDR到SDR/D3D11。A4项独立报告后才实施B；临时空工作区下以独立PhysicsScene加载真实S1 Demo，加入运行时对话组件，不改原场景/预制体。测试后原场景布局、播放选项与Gameplay绑定已恢复。

独立运行DialoguePanelTests，B-PanelAndModal：**4/4通过，0失败/跳过**，框架跨模式汇总4.5066386秒，不代表总墙钟。

实际验证：T打开、InputSystem中文事件到Enter只提交一次、空白拒绝；Echo气泡保留字面标签且全部Text关闭rich text；输入时W/E/F/空格/J/Tab不触发玩法或其他面板；Esc关闭后仍需点击捕获鼠标。真实JournalPanel/CabinControls与对话双向互斥，失焦关闭且禁止提交。120/后续100条消息只分配100行并复用，读旧消息不强制跳底。Bind/禁用/重启不显示旧会话或留下输入订阅。

[原始结果](../Evidence/S2-D02/B-PanelAndModal.json)、B-PanelHost-WorkspaceBefore/Entered/Restored保留；后置缓存stale时使用已有主线程空闲证明后才还原（F064），不绕过干净场景门禁。本步骤无新故障。实际Windows截图和进程退出仍待C；不将层级配置或Editor组件通过冒充Windows中文可读验收。目标硬件性能未在此验收。
