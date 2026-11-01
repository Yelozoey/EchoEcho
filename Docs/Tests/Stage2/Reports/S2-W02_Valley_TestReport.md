# S2-W02 山谷灰盒测试

2026-10-25，Unity6000.6.7f1/Windows11/D3D11/普通Forward/HDR→SDR，开发机i7-12650H/RTX4060Laptop/16GB；最低/推荐1080p60FPS基准仍未实机验收。

实际B-ValleyRetest 2/2通过、0失败/跳过、框架跨模式5.0855339秒。证据../Evidence/S2-W02/B-ValleyRetest.json及门禁。T01真实E读取两份山谷资料、重复读取幂等、日志显示完整原文/记忆仅摘要、不自动完成PZ01；T02真实D/S/W输入验证东西/南边界碰撞与北向出口保持通行，玩家没有跌落。预期与实际一致。

首次编译API拼错被Console门禁拒绝（F088）；首次运行夹具没有在根切换PlayMode，2/2失败（F089），记录A-Valley保留。修正模式切换与所有失败路径的标志恢复后复测；FinalFixtureFlags实测playing/background/playerSetting均false，原场景和播放选项已还原，475份S1 SHA不变。

山谷当前是无植被/碎石的原生灰盒，无局部体积雾。Lightmap/UV合并/PVS实际全区烘焙保留到W06-B子步骤独立验收；本报告只通过地形边界与线索交互，不冒充最终美术/光照/25–40分钟体验。
