# S1-W02 免费PBR资源与预制体验收

2026-10-25，v0.1。A许可/原文件完整性6/6后，B材质/预制体及实际Mask像素5/5后，C实际重新烘焙与回归。最终C-FinalRetest.json：78/78，0失败/跳过；本模块功能通过，允许W03。

两套Poly Haven CC0素材实际应用到墙与地/顶，作者、许可、获取时间/原metadata/MD5/SHA及API条款留档Docs/Assets；无账号、费用或运行时API依赖。B报告详述1K色彩空间、Normal、MipMaps、压缩、Mask通道、UV米制及3房间编辑复用prefab。保留原灰盒材质用于回滚，未导入未使用的大包；当前家具仍尺寸代理。

实际CPU重烘焙30.955秒，两张方向性Lightmap、141探针、8有效绑定，布局恢复；LightingBake.json。三室PBR最终图像平均亮度约.02331/.05654/.06220，SH幅度1.1793/3.7956/3.6235。预热后同状态基准差0、Baked灯关闭差0、5灯isBaked=true、移除Lightmap图像全黑。截图EditorLighting目录，主房/储物室视觉复查：实际灰泥/混凝土纹理、蓝灰主调、通路可辨，无粉色/缺图；不宣称完成最终美术。

首完整回归77/78的差异记录保留C-Final.json和EditorLighting-FirstFailure，F-042记录预热及稳定性证据；原阈值未降低。W01验收图像已保留并恢复，W02图像独立归档。最终审计：13原资产保护、6原文件SHA、实际图像三项通过，FinalAudit.json；Editor选项实际true/1。F-041编译命名空间问题已修复。

环境Unity6000.6.7f1/URP17.6普通Forward/内部HDR→SDR/Windows D3D11/1080p；开发机i7-12650H/RTX4060 Laptop/16GB。本模块在Editor做PBR图像验收，未另构建Windows PBR程序；W01原独立程序是接PBR前证据，最终P09须做产品Windows验收。目标最低i5-8400/GTX1060 6GB/16GB中档与推荐i5-12400/RTX3060 6GB/16GB高档1080p60FPS均未实测。
