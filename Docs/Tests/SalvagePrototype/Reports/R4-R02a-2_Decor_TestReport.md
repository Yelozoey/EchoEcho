# R4-R02a-2 实例装饰、LOD与Shader软带

2026-10-30，Decor_D2/2（3.797秒）；Wind_A1/1（2.912秒）。Decor_A中布局/真实六货物操作/Rover扫描7项已通过，新增自有NavMesh只重建新版；旧Nav与新场景修改前字节均留备份。

三区各12设备＋6软带，共36/18。设备204→12→4三角形三级LOD，24m起2三角形相机朝向Impostor，48m跳过，先视锥剔除；Shader摆动幅度0.02m无CPU动画。256px透明Impostor由真实Quaternius源模型正交渲染，配置、透明/不透明覆盖、归一化尺寸、边界与原始源保持均检查；已看Impostor和On_b470…实际画面。

实际开关画面差0.00617867；FrameDebugger事件21为InstancedMesh，Shader EchoStation/Next/InstancedDecor、Forward Pass，一次draw含5实例，SRP Batcher仍开。远景LOD2/Impostor选择、相机背向/远离0提交通过。UnityStats标准实例计数在本测试URP请求中持续0，原始失败A/B/C完整保留，不能当作GPU证据；F248改用原生FrameDebugger选中事件同步重渲染获取有效详情，不以enableInstancing标志替代。

独立Shader软带对照差0.000043985，CPU顶点完全相同、0动画组件，证实变形发生在Shader。测试仅改变运行时材质克隆，场景/管线/播放状态还原。

204是实测源三角形数，初稿按顶点数推断120有误并更正；不降低LOD或质量门槛。尚未墙体遮挡/最终Windows/目标硬件性能验收。
