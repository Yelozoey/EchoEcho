# PK06 第一人称Player实际报告

2026-10-27（Asia/Shanghai）。Unity6000.6.7f1、URP17.6.0、D3D11，RTX4060 Laptop开发机。用户要求给跑酷场景添加Player进入查看。

## 实现

主场景根节点增加`11_Player_FirstPerson`：原项目PlayerInputReader与FirstPersonMotion输入/视角逻辑、本场景VectorCityFirstPerson、CharacterController；子相机Camera_FirstPerson为唯一启用MainCamera，眼高1.6m/FOV70/远裁剪280，继承HDR/SDR与颜色Volume。原Hero相机保留，禁用；Views菜单可在Player和展示视点间切换。

出生X/Z=(-22,-25.5)，根据实际地面最高碰撞点+.10确定Y（当前12.32），WASD4.5m/s、Shift8m/s、Space1.6m跳高、重力18，Esc/左击释放与重捕获，R或掉至y<6返回起跑台。原S1输入/运动源码与Prefab不修改；场景几何、材质、反射EXR均不重建。

## 实际验证

`Evidence/PK06/Player_C.json` **2/2通过**（3.8335113秒），先经过干净场景/Prefab、Console与新鲜S2 RenderSafety门禁，再显式加载拥有场景。配置检查：唯一启用主相机、出生地面12m、HDR/SDR/Volume层与远裁剪有效。

真实PlayMode合成键鼠通过InputAction和MonoBehaviour.Update驱动实际CharacterController，不把直接改位移当输入通过。实际步行约.3秒1.35084m、同时间Shift2.401669m、跳跃峰1.59891m并落地、护栏停止x=-32.615、Esc不移动/左击恢复、R及跌落复位均通过。输入设置对象身份/flags恢复断言通过。

`ActualPlayer.json`保留最终复测数值；`FirstPerson_1080.png`为隔离灯光/几何的实际1920×1080渲染。Player_C当时平均亮度.5827097，最终Player_Final_B为.5843053。完整场景回归与真实独立场景播放见下方后置记录。

历史Player_A 1/2（F176加载前激活失败）；Player_B 2/2，随后按F177修复测试宿主额外监听器和外部渲染污染，并加入Shift位移验证，Player_C重跑通过。首失败回执保留。

最终`Player_Final_B.json` **2/2通过**（5.7386154秒）：步行1.350632m、加速2.400526m、跳高1.598865m、护栏停止x=-32.615、图像均值.5843053。增加展示/Player两种相机的MainCamera切换断言。`Player_Final`为1/2：F180修复测试过早按Space，最终重新实际验证通过。

全场`SceneRegression.json` **12/13**：Player项通过；唯一失败为F178既有起跑楼体顶面12.22与旧Waypoint_00=12高度差。原楼体位移来源未证实，按本次仅添加Player的范围保留几何，不提高容差或改旧路线断言来制造13/13。出生已适配实际地面；这项旧路线不一致仍未修复。

保护口径：进入本轮时磁盘默认/PC已是VC、PC AA4（与上一轮早期S2基线不同，候选哈希精确核实），最终保留本轮开始时的配置；场景关闭的Scope后备仍为原S2/AA0。`ProtectedAfter.json`实际1584份全部相同，0改变、0丢失。

最后以主场景独立进入实际PlayMode，未运行测试夹具：`LiveReady.json`读回playing=true、paused=false、sceneCount=1、角色grounded=true，唯一主相机Camera_FirstPerson；落地位置(-22,12.250,-25.500)，与配置出生Y=12.32及CharacterController接触余量相符。D3D11/VC_ForwardHDR、相机内部HDR开启、显示HDR关闭。`FirstPerson_Live_1080.png`为此时相机的1920×1080显式RenderRequest，均值.5826993，人工查看为清晰屋顶/红色路线画面；不是GameView帧率或性能测试。截图之后Console仍0错误/0警告。最后保持PlayMode运行，并打开/聚焦Game窗口，用户点击即可开始操作。

这些结果不等于完整镜之边缘蹬墙/攀爬系统、玩家主观手感、Windows Player构建或目标硬件性能验收。
