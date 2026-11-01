# 空间与HUD反馈版交付

2026-10-31，Windows构建19e0fe。运行`Builds/SalvageNext/EchoStationSalvageNext.exe`，整个目录一起使用。

已完成泊位/储藏4.5m、检修间5m层高，三区地标/区标、光照与流送重建，以及常驻HUD、任务板、Echo/设置/交付窗口改版。旧版包、旧场景与旧存档保留，玩法/权限保持已确认规则。

最终208/208回归、七类Windows验证与1620有效CPU/GPU样本通过；[实际报告](../Tests/SalvagePrototype/Reports/Feedback02_Final_TestReport.md)列明失败修复和边界。最低/推荐硬件及Windows10另机未测试；当前仍为脚本Echo，R3模型质量未通过。

Unity新入口：`Assets/EchoStation/SalvageNext/ArtV2/ES_ArtV2_Streamed.unity`。已恢复编辑器原S3干净布局，若要在Unity体验本版，主动打开该ArtV2场景；普通双击Windows包已使用新场景。

[操作说明](WindowsPlayGuide.md)与[首次试玩记录表](FirstPlayTestForm.md)已随包提供。建议接下来找3–5名未玩过项目的人独立体验，先记录问题，再决定下一轮少量修改；真实首次用时和体验不能由自动路线替代。
