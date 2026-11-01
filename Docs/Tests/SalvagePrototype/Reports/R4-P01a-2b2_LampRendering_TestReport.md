# P01a-2b2 实际灯具GPU呈现

2026-10-30，LampRender_A2/2、9.010秒，module_tests/Console0。普通Forward内部HDR/SDR，实际1920×1080渲染：开关灯具图差、HDR峰值与原生FrameDebugger实例绘制均达到门槛，数值见Evidence/R4-P01a-2b2/Render_GUID.json。每子网格每事件为一个draw、每事件至少两个实例，非组件数推算；36m距离与视锥剔除通过。

实测开关图差.006475965、线性HDR峰值3.333984；4个原生InstancedMesh事件每个2实例/1draw（捕获期间有重复显式提交），不能把4事件说成产品每帧固定4draw。单次Submit两材质槽2批，实际批数按可见区域测量。

1000次稳态矩阵/剔除计算0B托管分配，6实例，无Collider/Animator/ParticleSystem。灯具为纯发光表面及探针受光框架，不新增实时灯；原型夹具的Sun只是测试照明，不是产品新增灯。此项用直接资产验证GPU呈现，下一b3仍须实际场景Bundle/相机绑定/共享最后释放测试，不能把夹具当流送产品完成。
