# S1-P05-C1 日志面板与输入焦点

2026-10-25，C1-JournalPanel.json独立3/3通过。实际J打开、Esc关闭，面板拥有UI焦点；保持W/F/E/Space时真实控制器不移动、不跳跃/开灯/交互，关闭后仍需新点击捕获。Overlay排序100、中文字体沿用OFL本地资源。

真实按钮回调筛选全部/Note/Data/Scan，选中长笔记显示完整原文且RichText关闭；真实合成鼠标滚轮经InputSystemUIInputModule/GraphicRaycaster/ScrollRect改变正文滚动位置。正文80行可滚动，循环筛选10次行池数量保持初始4，没有每次筛选新建对象。禁用面板释放焦点/隐藏Canvas，重启关闭状态；销毁后拥有Canvas/EventSystem清除，日志数据继续归档无异常。

使用隔离Physics3D与已验证SyntheticInputScope，每次还原原InputSettings身份/flags，Scene/RenderSafety通过。本测试是实际UI组件/事件状态验证，C2再用Windows正常屏幕图像核验中文长文、按钮/滚动。目标硬件1080p中/高60FPS尚未实机验收。
