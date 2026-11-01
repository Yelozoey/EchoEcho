# S2-W06 核心与五区渲染验收

2026-10-25；Unity6000.6.7f1/Windows11、D3D11/普通Forward/内部HDR→SDR、1920×1080，UseLLM=false；开发机i7-12650H/RTX4060Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中1080p60FPS及推荐i5-12400/RTX3060 6GB/16GB高1080p60FPS尚未实机验收。

按子步骤依次通过并独立归档：A核心实际交互2/2；B1合并/UV/光照配置2/2；B2真实CPU烘焙153.68秒、2图集/325探针/13接收器，并2/2数据验证；B3真实23,792字节PVS，并1/1验证。各报告与首次失败/环境限制均留存。

最终C-Rendering实际2/2通过、0失败/跳过，跨模式15.6293964秒。证据../Evidence/S2-W06/C-Rendering.json。五区实际1920×1080 RenderRequest截图均非黑，平均线性亮度依次.02441/.02801/.01006/.02112/.01351；五图已逐张视觉检查，冷蓝照明、原小屋PBR、各区边界与线索可见。其余四区仍为灰盒，不写成正式美术。

受控关闭SSAO/PP/实时故障光等时间变量后，Off→Off稳定基线差0，PVS Off→On差0；FrameDebugger真实DrawOpaqueObjects SRP批次3→2。批次可含多个Draw，不能称Draw Calls减少1。实验室烘焙灯源关闭后差0，非黑照明仍存在，证实烘焙结果实际参与渲染。原始图像、FrameDebugger记录和Controlled.json保存在C-Controlled；所有临时Feature/相机/光照设置在finally还原。

C宿主正常还原当前干净S1 Demo/Greybox起点和播放选项；475份S1资产/交付SHA全部不变。B2期间的布局改变另见F091，未掩盖为恢复成功。没有用当前开发机或受控截图代替目标硬件性能；Windows普通帧Draw Calls/GPU/GC在V03继续实际测试。局部体积雾在P03独立实现，尚未计入本模块。
