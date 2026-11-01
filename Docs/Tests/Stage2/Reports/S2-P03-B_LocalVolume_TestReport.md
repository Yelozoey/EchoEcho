# S2-P03 B 真实局部体积

Unity6000.6.7f1/Windows11、D3D11/普通Forward/内部HDR→SDR，开发机i7-12650H/RTX4060Laptop/16GB。B-LocalVolume实际3/3通过，0失败/跳过，跨模式15.0589332秒。新S2专用管线复制原Gameplay，原文件保留，门禁只增加单一明确S2路径，其余Forward/HDR/SDR/PC/API/三绑定检查保持。

真实RenderGraph半分辨率960×540、16次积分、2局部光；AABB限A区/通道，深度截断。配置/边界/非法恢复和实际图像独立断言通过：雾开关平均差.0607093、局部光开关差.0214372、恢复密度差.0397562；只让通道有密度、前景立方体遮住通道时，中心192×108ROI差0，其余区域差.0002790。没有普通RenderSettings距离雾，也没有粒子堆积。

原始证据../Evidence/S2-P03/B-LocalVolume.json、B-Images/VolumeEvidence.json及Off/On/LightsOff/Recovered/Depth图。已视觉检查Off/On/Recovered/DepthOn，局部蓝灰散射与不透明遮挡吻合。运行期Feature/光照/密度设置finally还原，BHost正常恢复原布局/播放选项。此处用渲染夹具移开隔离做深度对照，不写成真实玩家通关。

GPU实际开销仍待D可见Windows对照；目标最低/推荐1080p60FPS未实机验收。C接玩家面板和三秒时间联动后再验证通路。
