# S1-P04-C1 提示面板生命周期

2026-10-25，C1-OverlayLifecycle.json独立1/1通过。实际产品Player实例化uGUI ScreenSpaceOverlay，底部显示[E]及注册目标名，UI占用隐藏；Scaler参考1920×1080、排序20、文字RichText关闭，销毁提示组件同时清理独立Canvas。

这证明实际Canvas/Text状态与生命周期，不等同屏幕像素；C2使用Windows普通屏幕截图验证。初测使用已确认存在的Microsoft YaHei系统字体；为覆盖无此系统字体的目标Windows，后续改用随包免费Noto CJK并单独重测。目标硬件1080p60FPS尚未验收。
