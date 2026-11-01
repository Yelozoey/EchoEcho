# 反馈02 V04b：实际流送、遮挡与导航

2026-10-31，Unity 6000.6.7f1，Windows11开发机，D3D11、普通Forward内部HDR/SDR。目标硬件仍为i5-8400/GTX1060 6GB/16GB中画质1080p60FPS及i5-12400/RTX3060 6GB/16GB高画质1080p60FPS；本报告不是这两台硬件的性能验收。

- 新三区Addressables与独立PVS构建成功：Build_3ab340，133.227秒；旧组/场景保留。
- Streaming_A共6项，5通过，导航夹具失败；修复F293后Navigation_B真实1/1、20.925秒。失败记录未删除。当前六种行为均已有通过证据，尚未替代最终全量回归。
- 真实距离跨区加载0.0539秒，低于10m预加载缓冲/3.5m每秒=2.857秒；远区独占Mesh/贴图释放，其他房共享材质保留。
- 三区方向Lightmap共六槽，52接收器跨Start保持映射，234动态探针保留；分区图与共享图对照一致。读档撤销旧世界任务，退出后迟到加载释放。
- 实际FrameDebugger：关闭PVS61次绘制、开启24次、背向0次；开关可见图差0。四场景共享新遮挡数据。
- Echo实际依次穿越上下两个泊位/储藏门与检修门，再经短路返回；流送开启、无失败。原七物品Nav路径测试已在V04a通过。

证据：`../Evidence/Feedback02/V04/Streaming_A.json`、`Navigation_B.json`、`ActualNavigation_*.json`、`Distance_*.json`、`Presentation/Rendering_*.json`、`PVS/Visibility_ce1a126e1c4347b18c76fe6d668a3175.json`。安全前后快照由module_tests归档，空场景/实际管线门禁通过。

下一U01 HUD样板，随后U02全界面；Windows新版实际流程和性能仍未执行。
