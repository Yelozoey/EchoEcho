# R01c-1 蓝灰材质与后处理

2026-10-30，N08常规适配，R01b门禁11/11已过。

同一已校验CC0 Standard包内T_Trim_02_BaseColor.png为中性灰版本，1,125,543B／SHA7b13c6a7…ff6293，ZIP CRC通过，NeutralSource_A有准确来源；复用原法线／UV／粗糙／AO，只替换新版静态ES_NextLit材质里蓝色Trim02的albedo，原导入基底与动态货物不改。原蓝色文件保留。新增来源另存补充收据，不改历史Extraction。替换会影响反弹与反射，因此重新实际烘焙并复测R01b。

F243核验：原Raw内已存在完全相同中性灰文件，复制保护停止后按SHA复用；NeutralReused_A是实际收据，不记新增下载或覆盖。

专用全局Volume：ACES，FilmGrain Thin1／0.06／response0.8，Chromatic0.012，ColorAdjustments曝光+0.25EV／对比5／饱和-30，颜色滤镜白；不启用显示器HDR或未要求的重效果。保留旧模板，仅新场景替换Volume引用。每项真实图像A/B，随机SSAO与其他效果临时隔离并finally恢复；基准1080p／D3D11／内部HDR→SDR。

c-2与c-3另做独立模块（R4_NextImplementationNotes），本项不提前启用DOF或雾。实际图像须看过，静态资产字段检查不算最终画面或目标机60FPS。
