# S1-W01-B3 小屋实际遮挡验证

2026-10-25。Editor两项通过（B3-EditorRetest.json）；独立Windows构建成功、0错误，Player退出0（VisibilityPlayer/Exit.json）。PVS5728字节/资产12275字节，场景持久化引用有效。使用8个同室合并体、30/28m距离阴影策略，未修改源模板。

Windows D3D11、1920×1080正常相机帧：遮挡关闭Draw Calls中位13，开启9，进入储物室10；每状态10帧预热、16个不同帧采样，48个值全部非零有效，状态内一致。开启/关闭同视角图像平均差0。原始计数/帧号、PNG和日志在Evidence/S1-W01/VisibilityPlayer。

Editor采集Opaque SRPBatch三状态均为1，不能用此量代替实际Draw Calls。首轮错误批次断言保留B3-Visibility.json并记F-039；Windows严格下降断言通过后才验收B3。首次构建因Editor专用Light.lightmapBakeType失败，保留VisibilityPlayerBuild-FirstFailure.json，运行时改用显式Baked组，复构建成功（F-040）。两条既有构建环境警告见F-011，未连接云服务。

开发机i7-12650H/RTX4060 Laptop7956MB/16122MB/Win11；这是当前小屋的功能和计数诊断，不是最低i5-8400/GTX1060 6GB/16GB中档或推荐i5-12400/RTX3060 6GB/16GB高档1080p60FPS性能验收。允许进入W01-C；W01整体尚未通过。
