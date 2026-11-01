# S1-P03-A 手电实际输入与生命周期

2026-10-25，Unity6000.6.7f1/InputSystem1.20.1。实际F经正常Mono Update开→保持不重复→再按关闭，UI/失焦不响应；组件禁用关光但保留On意图，重启恢复；嵌套R04实时Spot6/range20/55°/Soft/阴影Tier2参数与相机挂点符合要求。

首轮功能断言完成但TearDown报错，不能判为通过（A-InputAndLifecycle.json）。查F060发现InputManager会销毁旧HideAndDontSave默认设置，临时夹具改为保护该对象、还原其身份/flags，P01统一采用同一修复。最终Player程序集18/18通过（A-RestoreRegression.json），包含本独立用例及全部P01/P02回归，每次Scope清理明确断言同一原对象和原flags还原。F059 API宿主字段编译错误也已修正后实际测试。

这只证明输入/生命周期/接线，B还需真实受光/阴影/方向图像。性能硬件基线沿用总文档，目标1080p中/高60FPS未实机验收。
