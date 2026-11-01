# S1-P04 交互射线、E输入与提示

2026-10-25连续S1授权，P03独立通过后实施。A无输入射线/身份/遮挡契约→B产品E边沿及UI/失焦/重新验证→C uGUI提示，逐项测试和报告后推进P05，不提前实现日志。

相机中心射线最大2.5m，只认已注册的WorldTarget精确实例/唯一ID。固定16命中缓冲，满则保守拒绝；忽略玩家自己的Collider和无WorldTarget的Trigger，普通实体遮挡必须生效。隐藏Scan线索未获Echo授权前不可选中/读取/显示提示。设备可查看说明，无门/供电状态修改接口。

E边沿每按一次最多一次交互，保持不重复；UI占用/失焦/未捕获拒绝。执行前重新射线并同步Transform，不能使用已被墙挡住或销毁的旧缓存目标。ClueSource.TryRead仅产生只读事件供P05归档，重复收集的去重由P05负责；原文/摘要沿用W04分离，不接LLM。查看设备给克制的临时提示，不替玩家操作。

uGUI ScreenSpaceOverlay底部短提示与中心准星，显示[E]及线索/设备名，非可交互/隐藏/遮挡/失焦/UI时隐藏提示；后处理不模糊Overlay。C1先验证当前Windows系统中文字体；出包改为随附未修改Noto Sans CJK SC Regular静态OTF，SIL OFL1.1许可/版权/来源SHA留档，Resources本地动态字形，不依赖系统可选字体。A/B在隔离PhysicsScene测试，C检查实际Canvas/Text绑定和窗口图像；独立测试不依赖最终P09场景。
