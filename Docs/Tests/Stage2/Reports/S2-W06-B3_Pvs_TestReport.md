# S2-W06 B3 墙体PVS烘焙

Unity6000.6.7f1/Windows11/D3D11/普通Forward/HDR→SDR。B3-01/OcclusionBake.json实际0.5155948秒完成，S2独立资源48,648字节，序列化PVS23,792字节；原生统计bytes=0按既有F经验保留，验收用实际保存资源及后续渲染对照。烘焙恢复原布局。

B3-Pvs实际1/1通过、0失败/跳过，0.4420859秒。验证场景smallestOccluder=2、smallestHole=.25、真实GUID指向S2资产、PVS非空且超128字节。证据../Evidence/S2-W06/B3-Pvs.json与门禁。

本步骤不把PVS资源字节数解释为Draw Calls减量；C继续图像与正常渲染路径验证，V03另测Windows实际计数。目标硬件性能仍未实测。
