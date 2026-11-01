# B06 完整切片实际报告

V01补测：双Image准星与热点查询优化后FinalRegression_A89/89；新增拾取物动态NavMesh carving实际成孔/恢复测试后FinalRegression_B90/90，113.830秒，Job247ba1807ab74e8c984a7f13a3a35885。该轮含10完整场景测试和80细分回归；Windows仍以最终V01报告为准。

2026-10-31，最终FullExpansion_A，Job 4eb315194ff24275b2c41ba679a4551d，89/89真实通过，109.649秒；其中完整场景9项、新旧细分模块回归80项。结果fullName明确包含实际雾渲染与旋转回收用例。安全门禁Unity6000.6.7f1、普通Forward/内部HDR/SDR/D3D11。

新场景Assets/EchoStation/SalvageExpansion/Scenes/ES_EnvironmentalSalvage.unity，三区ArtV2 Addressables/烘焙/探针只读复用，4.5/4.5/5m层高，独立Nav_Clearance.asset；七物品、七设备、两危险。Rover模型复用。默认脚本Echo，原R3 No-Go不变。

实际通过：供风上路完整行走/核心取出/返程/预览交付/显式结算；关阀下路＋可选数据归档完整行走；Echo出生点走到高处设备扫描且不改设备；失败阻断保存、有效读档和完整重开；三UI互斥、教程跳过/回看、时间暂停；三画质与实际场景PNG（已人工查看）；蒸汽雾实际提交半分辨率640×360 RenderGraph pass，供风后不再渲染；核心旋转前样本无法放入，实际拖拽旋转后可多带样本，取消交付无收益、确认后4额外价值且不能重复确认。每次卸载等待Addressables全部句柄关闭。

失败保留：Integration_A34/34仅旧组，F306，不算整合通过；B38/40通路设备挡路F307；C14/16货物绕行/机器人让路与调查高度F308；D17/17针对性通过后追加雾/格子取舍并跑全量89。测试Runner新增DLL新鲜度及每请求组实际执行检查。

证据Evidence/Expansion/B06/FullExpansion_A*.json及Scene_*.png，Created_A/Clearance_B记录独立资产。当前预算18.797GiB。尚未新Windows打包、真实跨进程/原生显示/GPU采样或真人首次5–8分钟；这些不由Editor结果替代。目标Win10/11 x64/1080p，最低i5-8400/GTX1060 6GB/16GB中档60FPS、推荐i5-12400/RTX3060 6GB/16GB高档60FPS仍需目标机另验。
