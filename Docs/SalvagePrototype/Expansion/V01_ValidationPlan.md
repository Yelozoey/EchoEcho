# V01 Windows验证与恢复计划

B06最终通过归档后实施。本轮新输出Builds/SalvageExpansion/EchoStationSalvageExpansion.exe，旧包保留。目标D3D11/1080p/内部HDR转SDR，UseLLM关闭。

构建前刷新预算、记录原模型SHA；构建期间独立监测既定资源根，22GiB仅为打包临时上限。构建成功后仅移除新输出_Data/StreamingAssets/EchoLLM副本，路径先验证位于新输出根；不移动或删除项目权重。记录去除字节数、实际峰值、交付后20GiB检查。构建设置使用可恢复作用域与SDK保存拦截；保留第三方许可证。

真实Windows至少验证：常规启动与正确场景/管线；供风上路完整目标；关阀下路＋可选数据完整目标；显式交付与结算/重开；独立进程写档再读档；压力失败/有效读档；背包/Echo/任务/设置互斥；原生分辨率/窗口/画质确认与回退；可见1080p三画质三区有效非零GPU样本。自动移动路线仅证明可执行，不当真人首次5–8分钟体验。当前开发机不代表i5-8400/GTX1060或i5-12400/RTX3060目标机，也不代表Win10另机。

最后完成Run01临时作用域恢复：module_scope.py restore，然后regression_host.py leave；核对原S3场景、native/disk/dirty、渲染绑定、Console、旧代码/配置保护、预算及无残留测试/构建进程。原VC管线恢复后不在测试白名单属已知正常，不为凑门禁改白名单。完成交付后停止，不进入模型实验或下一阶段。
