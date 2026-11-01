# S1-W01-B2 小屋烘焙、探针与Volume实际验证

2026-10-25，v0.2。最终World8/8通过（A/B1/配置/实际图像），4.920秒，0失败/跳过；`Evidence/S1-W01/B2-Readability.json`。前置A/B1已归档，B2配置7/7后才进行烘焙。

实际Progressive CPU烘焙30.185秒，生成2张方向性Lightmap、141个探针，8份静态合并Renderer绑定有效，原场景布局恢复。数据见LightingBake.json。主房/储物室/门斗动态采样位置SH幅度1.2463/3.9216/3.7763，均有实际间接受光数据。

同相机禁用所有Baked灯后画面差0，证明静态照明保留；移除Renderer Lightmap索引后平均差0.083304且像素亮度0，再恢复原索引/offset。量化控制临时关闭SSAO/后处理与故障实时灯，最后完整恢复并拍摄三室Volume实际图像；最终线性平均亮度.03232/.06611/.07203，均超过修订可读性门槛.01。配置保持内部HDR/SDR、R02 ACES/颗粒/色差，未改共享Profile或曝光0。

人工检查主房/储物室截图，通口与灰盒物件可辨认，无粉色/整片缺图。仍是尺寸代理，不冒充正式PBR美术。首版功能通过但过暗的截图与烘焙JSON保存在EditorLightingFirstLook及LightingBake-FirstLook.json，F-038说明为什么提高灯能量、降低蓝饱和度并调整储物室取景；没有删除原失败/不足记录或降低光照因果断言。

环境Unity6000.6.7f1/URP17.6普通Forward/Windows Editor D3D11/1920×1080；开发机i7-12650H/RTX4060 Laptop/16GB，非目标硬件性能验收。最低i5-8400 GTX1060 6GB 16GB中档、推荐i5-12400 RTX3060 6GB 16GB高档1080p60FPS仍未实测。

本报告允许B3小屋实际遮挡接线；W01整体和玩家/Echo模块尚未完成。
