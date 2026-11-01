# S1-P05-C2 Windows日志实际UI与屏幕

2026-10-25，Windows可见1080p/D3D11，Unity6000.6.7f1、ES_PC_ForwardHDR普通Forward内部HDR/SDR。构建Succeeded/0错误/2警告，25.587s；实际播放器退出0。

WindowProbe.json全部通过：合成J通过真实InputAction打开日志，合成鼠标经UI输入模块点击笔记分类/长记录行，原文匹配；真实鼠标滚轮到正文底部normalizedPosition=0；点击数据分类仅1条并显示正确标题；Esc关闭并释放焦点。不是直接调用按钮事件冒充窗口路由，也不声称进行了物理键鼠人工走查。

JournalNote/JournalScrolled/JournalData/JournalClosed.png是WaitForEndOfFrame后的实际最终屏幕，已查看前三张：中文段落/分类/标题清晰，长文有裁剪与滚动，尾部原文标记按文字显示。首屏面板白字像素59504。Overlay使用随包Noto，不依赖系统可选中文字体；OFL通知已复制到构建。输入临时配置仅在诊断进程内并在结束还原。

构建/运行日志中Services/Windows.Forms及禁用DOF/Panini剥离提示按F011/F019保留。当前Win11/i7-12650H/RTX4060 Laptop/16GB，最低i5-8400/GTX1060 6GB/16GB中画质、推荐i5-12400/RTX3060 6GB/16GB高画质1080p稳定60FPS仍需目标实机，本次不作性能达标结论。
