# S1-P04-B 实际E输入与执行前复核

2026-10-25，B-ActualEAndSafety.json独立5/5通过。真实InputAction/Mono LateUpdate下E每个新按沿读取一次，保持5帧不重复，第二次新按产生第二次只读事件；两次同ID/原文稳定且与摘要分离（ActualERead.json）。UI/失焦时不读取、无提示；再次聚焦不自动捕获。

缓存目标之后新增实体墙，TryInteract立即重新射线拒绝；禁用/销毁目标也拒绝，Unity伪null不残留提示。设备只返回扫描信息说明，位置不变，没有任何门/供电执行接口。源数据/事件只供后续P05日志归档，本模块不实现去重日志或LLM。

隔离Physics3D，合成键盘使用已修复SyntheticInputScope并在TearDown断言原InputSettings身份/flags还原；渲染安全门禁通过。C继续真正uGUI和Windows屏幕图像。目标硬件1080p中/高60FPS未实机验收，基线沿用总文档。
