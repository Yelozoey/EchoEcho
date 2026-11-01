# P01b-1 Windows临时构建配置

2026-10-30，最终PlayerScope_C 3/3，2.674秒，完整门禁通过。正常退出、主动异常退出、实际UnusedAssets原生回收后，Player/Graphics/Quality/UnityConnect原生JSON、磁盘字节、dirty精确恢复。管线按路径重新载入，不能保留可能被回收的Unity对象引用。

临时1080p可缩放窗口、D3D11唯一API、关闭显示器HDR、启用FrameTimingStats；质量列表只在scope内包含三套专用管线以保留运行时变体。尚未执行BuildPlayer或Windows运行，不能据此称打包通过。详细恢复收据见Evidence/R4-P01b-1/Restore_GUID.json。

历史A2/2只覆盖未发生回收的两条路径，首次真正BuildPlayer暴露F267；B3/3被F268未知脏设置门禁拒绝、无配置变更。两份失败/局限保留，不混为最终通过。首次构建引发的未变UnityConnect脏标记已完整字段/磁盘SHA核验后清理，后续scope正式捕获它，不放宽保存保护。
