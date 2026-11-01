# S2-W06 B2 真实Lightmap与探针

Unity6000.6.7f1/Windows11开发机、D3D11/普通Forward/HDR→SDR。B2-01/LightingBake.json：实际CPU烘焙153.683584秒，2图集、325探针、13/13静态接收器分配有效，全部Color/Direction资源位于S2专用文件夹，烘焙回调恢复布局。

随后B2-Lightmaps实际2/2通过、0失败/跳过，2.1743613秒；验证所有图集存在且≤1024、每个静态接收器索引/UV缩放有效，实际插值五区探针L0非零。B2-Lightmaps.json及门禁留证。

测试之后布局从本轮开始Demo变为干净Greybox，host leave按F091拒绝强制恢复；B2-PostWorkspace.json留存实际场景/播放选项/管线。保留该新干净布局作为后续恢复起点，不将此写成宿主恢复成功。布局变化来源仍未确认；没有保存/丢弃用户改动。

Editor日志另出现既有Curl35/UnityTLS7，烘焙与本模块结果有效，见F002，未为消除网络提示修改TLS或关联云服务。目标最低/推荐硬件性能尚未实测；图像与实际PVS另在后续步骤验收。
