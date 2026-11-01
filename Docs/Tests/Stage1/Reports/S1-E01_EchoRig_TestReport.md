# S1-E01 轮式Echo灰盒预制体验收

2026-10-25，v0.1。前置W04独立通过。最终Rig-Retest.json 4/4、0失败/跳过，E01功能通过，允许E02。

ES_Echo.prefab具有根scale1、四个独立X轴轮组、Body/ProbePitch/ScanOrigin/Status挂点和单一.75×.86×.75m根BoxCollider，底部在地面，可通过1.6×2.4m门洞。实例化后轮组旋转60度、探头俯仰25度，机身/root位置不变，扫描原点随探头移动、方向变化25度。没有蒙皮、Animator、粒子或面部依赖。

动态URP Lit/BlendProbes、无Static/MaterialPropertyBlock，蓝/黄/红三个独立发光材质已保存，默认蓝。实际PlayMode正反两张1080p图像非空/无粉色，人工复查确认四轮与单探头结构；关闭所有灯、仅保留Status渲染时仍有>1的实际蓝色HDR像素，StatusEmissionDark.png。不是仅检查Emission关键词。

首轮Rig.json 3/4失败及Images-FirstRun保留；F-044记录URP按GI标志重新计算发光关键词，修复后实际图像与关键词均通过。发光表面无ContributeGI，不开启Realtime GI。原模型来源核查/授权灰盒选择及F-043访问403记录见S1-E01文档，未下载任何第三方机器人文件。

环境Unity6000.6.7f1/URP17.6普通Forward/Windows Editor D3D11；当前开发机同总文档，无独立Windows/目标硬件性能验收。此模块仅模型/挂点，不含导航、行为树、状态动画，也未提前将测试代理放进产品场景。
