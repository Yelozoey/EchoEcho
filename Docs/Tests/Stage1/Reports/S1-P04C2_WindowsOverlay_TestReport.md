# S1-P04-C2 Windows真实屏幕提示

2026-10-25，Windows可见窗口1920×1080、D3D11、Unity6000.6.7f1/URP17.6普通Forward内部HDR/SDR。构建Succeeded/0错误/2警告，31.708s；播放器实际退出0，WindowProbe.json通过。

采用正常相机/ScreenSpaceOverlay最终屏幕，WaitForEndOfFrame后ScreenCapture读取，未用不能包含Overlay的单相机RenderTexture冒充。屏幕提示[E]读取：测试线索，随包NotoSansCJKsc-Regular字体。底部文字ROI亮像素1030，新增前方实体遮挡后0；PromptVisible/PromptOccluded.png保存，已经实际查看中文清晰且无缺字。诊断驱动器显式允许门禁、固定视角以只验证显示，此测试不冒充物理键盘或窗口焦点人工操作；实际E/焦点在B独立合成输入路径测试。

OFL许可/Adobe版权已复制到构建ThirdPartyNotices/Noto，FontAudit.json校验全部字节/SHA256和静态CFF1。构建后的RenderSafety仍默认/PC/实际ES_PC_ForwardHDR。Services与Windows.Forms构建提示及禁用DOF/Panini剥离警告按F011/F019留档，未为消提示关联云服务或改第三方代码。

当前开发机Win11/i7-12650H/RTX4060 Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中画质及推荐i5-12400/RTX3060 6GB/16GB高画质1080p60FPS仍需目标实机验收，本次UI窗不作性能结论。
