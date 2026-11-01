# W01-B2 光照配置前置验证

2026-10-25，实际World7/7通过，1.604秒；证据`Evidence/S1-W01/B2-Config.json`。此前B1及A回归包含其中。仅表示配置前置通过，不是B2整体通过。

已检查专用LightingSettings复制且独立于R03资产；ProgressiveCPU、手动BakedGI/无RealtimeGI、20texels/m、最大1024/方向贴图；5盏Baked Point；探针不少于100且不在任何源BoxCollider实体内；R02 Exploration Volume/Profile及256层掩码、相机Post Processing/HDR/SDR接线；故障灯为Realtime。实际Lightmap/SH和三室画面须等待烘焙及图像测试，尚未验收。

环境Unity6000.6.7f1/URP17.6普通Forward/Windows Editor D3D11；此检查不是目标硬件性能测试。最低i5-8400 GTX1060 6GB 16GB中档、推荐i5-12400 RTX3060 6GB 16GB高档1080p60FPS仍待对应硬件验证。
