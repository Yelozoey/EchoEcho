# A01-C1 烘焙、SSAO与反射实际记录

2026-11-01。A01-A/B已独立通过并归档后进入本项。独立IL_ForwardHDR/Renderer/Volume/Lighting资产；普通Forward、内部HDR/SDR、D3D11。1080p、renderScale1、MSAA1/SMAA；2级CSM/2048/40m；半分辨率SSAO、ACES曝光最终.55、轻微Bloom/颗粒/暗角。旧模板和源材质未改。

36烘焙顶灯、1 Mixed间接烘焙主光、2实时局部灯。GPU实际BakeJob_A用301.623秒，2张方向性Lightmap、216光照探针；另8个离线128 HDR反射立方体绑定本地区域。保存范围与宿主还原均通过。静态薄墙/顶板双面投影和doubleSidedGI；四动态机器人用BlendProbes。

Lighting_A真实6/6，但四张实景图4589b3d0仍暴露顶灯朝向/观察窗反光问题，未据自动通过推进。保留LightingDraft_A原场景/贴图和A版反射。顶灯翻转、玻璃关闭环境反射/高光（简化低反射透射，不是真实折射）、曝光.4→.55。BakeJob_B真实39.624秒，2张Lightmap/216探针、8个B版离线反射，0错误、layoutRestored=true；不是重新执行A或覆盖旧证据。

Lighting_B启动被external_changes_dirty拒绝，无测试Job。只刷新、不再次请求编译后，Lighting_C实际6/6、0跳过、18.716秒。工程5项验证资产/HDR/Forward/SSAO、实际72块地面Lightmap索引、216探针、8持久反射/真实烘焙收据和灯光预算；另1项输出四张真实1920×1080 PlayMode离屏图，无检查补光。

同相机禁用动画/颗粒的图像开关对照，线性RGB平均绝对差：AO .0017732，离线反射 .0024894，总烘焙贡献 .0390954。该烘焙对照没有隔离纯间接分量，不声称逐区溢色已定量验收。5dc5b655四视图已由代理查看：入口/办公可读，仓区偏冷、办公偏暖，观察窗可看清内部；顶灯灯壳仍为原模型外观，非所有灯面均有明显自发光条。最终审美接受待用户亲测。

本项只证明本机Editor工程与实际图像，不证明最低i5-8400/GTX1060 6GB/16GB、中画质1080p60FPS或推荐i5-12400/RTX3060 6GB/16GB、高画质1080p60FPS。SSR/SSS未实现；局部雾/Profiler/最终还原属于后续C2/C3。现归档C1后进入C2。
