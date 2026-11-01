# R4-R01a 专用Forward HDR与实时阴影预算

2026-10-30，Render_B实际4/4、4.686秒。三个专用管线均普通Forward／内部HDR、独立复制的Renderer及三内嵌Feature、SRP Batcher／实时额外光；实际场景绑定、SDR Camera、Low/Medium/High切换真正生效，作用域关闭恢复原default／quality／AA。11组DistanceShadowCaster远关近开，动态光阴影tier高／中／低与禁用还原通过。1920×1080 HDR离屏实际手电开关图像有贡献，D3D11，原图与数值Flash_GUID.json保留。

Render_A3/4失败与图片保留，手电5在新PBR表面的贡献低于0.001门槛，按F236只调新版20后完整重测，没有放宽阈值。此项没有证明最终静态烘焙、画质表现或CSM四级GPU索引新场景图像通过；后者历史S1证据只作复用依据，本项新证据是实际新管线与caster/tier行为。

硬件基准Windows10/11x64、1080p：最低i5-8400／GTX1060 6GB／16GB中60FPS，推荐i5-12400／RTX3060 6GB／16GB高60FPS；本次Editor功能与图像不替代目标机性能测量。
