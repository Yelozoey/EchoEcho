# 反馈02 U01：常驻HUD与CC0样式

2026-10-31，Unity6000.6.7f1/D3D11/普通Forward内部HDR/SDR，Windows11开发机。目标配置和60FPS门槛沿用总文档，本模块只验UI功能与可读性，不作为目标硬件性能证明。

Hud_A 0/2，Noto行高与低电快照夹具修复见F294；Hud_B域重载门禁拒绝，未执行；Hud_C 2/2。图片检查后扩大源图透明留白的准星显示区域，并增加实际中心像素断言，Hud_D最终2/2、7.356秒。所有失败/未执行收据保留。

真实加载ArtV2流送场景、读取实际恢复的手持工具箱、3件18分与18%低电；任务板打开隐藏HUD，关闭恢复，不改变输入所有权、Mission组件或UseLLM=false。旧SalvageHUD与字符准星仅在新场景运行时替换，旧源码/场景保留。

真实UGUI离屏渲染1920×1080/1280×720与明暗背景四图，中文文字宽高边界、实际绘制色彩、中央准星像素通过。已查看1080暗底及720亮底图，分区无重叠；这是独立HUD呈现检查，最终Windows屏幕叠加截图另验。

证据`../Evidence/Feedback02/U01/Hud_D.json`、`Readability_*.json`、`HUD_*.png`、`State_*.json`；安装保存范围`Install_A.scope.json`。Kenney官方三包CC0来源/逐PNG SHA和许可证在`Assets/EchoStation/SalvageNext/ArtV2/UI/Kenney/`；仅9张原PNG、2564字节，未导入整套资源。

U01通过，进入U02各面板样式与互斥/确认/存档回归。
