# A01-W03 浅灰涂层修订实际记录

2026-11-01。用户看W02裸金属图仍觉太暗，明确选择浅灰半哑光涂层墙＋裸金属边框。本次仍是同一块入口South_4及背面的局部对照，未自动铺全场。

初版自有场景/meta、运行时与两份测试源码压缩留存BareMetalPreview_Archive.zip，CRC及逐项SHA见BareMetalArchive_Manifest.json；WP_WallBrighter材质与原三张图、所有失败/阻止回执保留。旧金属大图、旧模型/构建不改、不删除。

槽0改WP_WallCoated：Metallic0、Smoothness.42、关闭裸金属遮罩关键字，不让旧遮罩R继续把涂层变金属。由原色图整数面积平均到512，灰度重新映射为`.82 + (luma-.5)*.14`并轻微冷色，保留低对比原图变化；原法线/AO/几何继续使用。槽1裸金属边条、槽2标识保持。默认F6涂层，无额外预览灯；F5旧裸金属，F7涂层＋强度2弱补光。原曝光.55/ACES不变，复用旧GI/探针，未宣称全场重烘焙完成。

**W03_B实际7/7、0跳过、20.865秒，output全部为空。**统一module_tests.py新鲜场景/Prefab、RenderSafety和Console门禁通过：独立场景与72地块/净1007.70074㎡/四岗位、仅两个槽变体、真实纹理灰度范围/细节与法线AO、半哑光涂层参数、裸金属边条与默认灯关闭、三态保护槽、实际F5/F6/F7与移动、禁用还原/重开、真实1080p受控画面回切。

WallDeltas_80454423：同视角/世界/曝光，冻结颗粒与动画；ROI原.058553367，涂层无补灯.339809016，旧裸金属仅弱补灯.069347204；全图原/涂层差.04384644，切回原像素差0。旧原图同条件已保存，不重复编码同一Before图占预算；新图Coated_NoFill_80454423.png为1920×1080/1,001,625字节，代理已查看，中央涂层墙整体明亮且保留边缘、接缝，旁边仍是旧裸金属参照。这里的ROI含部分灯/标识，不能当纯墙物理反射率，也不能用它代替本人审美确认。

W03_A因纹理资源更新门禁未启动，无Job；refresh compile:none后B真实运行。编译后失联/窗口激活两次超时经本人手动前台恢复，F-374记录；未绕过安全门禁或保存/停止用户Play。W01/02历史通过仅代表初版工程，初版视觉未通过；本版视觉等待本人。

NativeRestored_B与NativeVerified_PreLayout_B已验证5设置native/disk/dirty和运行时精确还原。原布局恢复、全15381旧文件SHA/预算/新场景交付状态另补收尾实际回执。Windows10/11 x64/D3D11/1080p目标保持，最低i5-8400/GTX1060 6GB/16GB中60FPS、推荐i5-12400/RTX3060 6GB/16GB高60FPS均未实测；不打包或启动AI/守卫开发。

收尾：Protection_Final_A全15,381旧文件SHA重算一致、changed为空。Wall-WorkspaceRestored先还原原金属场景布局/播放选项；EditorHandoff_A随后仅打开新局部预览。FinalState_A再次验证5设置native/disk/dirty及运行时精确一致，PC1/AA4/D3D11，clean/Edit未播放、Missing Script0、Mode1/补灯关闭、Metallic0/Smoothness.42，两个目标Renderer；72地块/净1007.70074㎡/4岗位/2 Lightmap/8探针，临时反射相机0。Console_Final_A错误0。完成后停止，待本人视觉反馈；当前只改一块墙，不能写成全图涂层或重新GI交付。精确最终资源总量见Budget_Final_A。

Budget_Final_A实测19.998550/20GiB，本轮新增约8.3MiB，低于修订计划9MiB；旧模型/构建/证据均保留。未再启动测试/下载或后续模块。
