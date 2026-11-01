# A00-A 资源适配实际测试记录

日期2026-11-01；本项是A00美术短任务的首子步，不代表样板或玩法已交付。

环境：Unity6000.6.7f1、WindowsEditor、D3D11；module_tests.py使用干净S1_P09_RegressionHost，默认/PC/实际ES_Gameplay_ForwardHDR，普通Forward/内部HDR/SDR，空Console。原环境场景/VC与AA4及三项设置native/disk/dirty已在NativeBefore_A捕获，最终还原待A00-C。

补记实际开发机信息（CreatedScene_A原生采集）：i7-12650H、RTX4060 Laptop、16122MiB、Windows11 10.0.26300。只记录测试机器，不替代最低或推荐硬件验收。B阶段材质视觉修正后，独立Assets_D再次8/8通过。

资源：LowPoly Robot作者免费包7,015,268字节，14段验证接收，ZIP安全路径/CRC/许可页CC0通过；选择11个数据文件共10,390,221字节进入独立目录，旧源只读。原包/许可/摘要见Docs/Assets/Sources/InfiltrationPrototype/Manifest.json，映射见A00/SelectedSources_A.json。15种派生实体预制体，包括墙地门/灯、桌椅终端、工具箱、门卫和已有Echo。

实际测试：Assets_A共7项5过2失败；源地板12顶点不应被额外>24阈值拒绝、门卫缺Animator，见F-336。修正后Assets_B被external_changes_dirty拒绝，无Job不计测试。核对新DLL后compile:none刷新，Assets_C实际8/8、0失败、0跳过、0.882秒。

覆盖：15角色非空/源网格追溯（小型状态灯单独限定尺寸）、URP自有材质、门卫源Idle与控制器、Guard/Chair/Door米制尺度、桌椅门实际纹理图集、工具箱与终端源模型区分。门卫实际Idle长3.333秒且440曲线已读到；本子步只检查引用与资产，真实播放效果在后续PlayMode验证。

证据：Evidence/A00/Assets_A.json、Assets_B-ExternalChangesPending.json、Assets_C.json及同标签SceneSafety/RenderSafety/Job、PreparedAssets_A/B.json、InspectAdapted_A/B.json。所有路径相对Docs/Tests/InfiltrationPrototype。

资源预算：Budget_AfterAssets为19.695519/20GiB；14,426旧文件保护基线已捕获，最终SHA复核未执行，不能声称最终旧版保护已通过。未对目标硬件60FPS或最终美术完成度作验收。A00-A独立工程门禁通过，允许进入A00-B场景与照明。
