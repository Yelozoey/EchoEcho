# S2-W06 B1 静态合并与光照参数

Unity6000.6.7f1/Windows11、D3D11/普通Forward/HDR→SDR。B1-StaticSetup实际2/2通过、0失败/跳过，0.5897115秒，证据../Evidence/S2-W06/B1-StaticSetup.json。

验证8–24个实际静态接收器逐个使用S2独立Mesh、UV2有限且三角形内域无重叠、房间/材质合并、原主室源渲染器禁用、交互设备未合并、无PropertyBlock；12texels/m/1024/Indirect128/Shadowmask、独立设置、>200探针和≥13静态点光、实时故障灯、真实相机遮挡开关与30米阴影策略均符合记录。

此处仅通过配置和离线生成，不表示Lightmap或PVS已经烘焙。下一步B2实际烘焙并独立验证所有静态接收器分配与图像。
