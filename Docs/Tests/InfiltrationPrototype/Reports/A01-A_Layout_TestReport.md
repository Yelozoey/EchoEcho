# A01-A 空间与连通实际报告

2026-11-01；Unity6000.6.7f1、D3D11、普通Forward、内部HDR/SDR。已确认A01范围见Docs/InfiltrationPrototype/A01_ExecutionPlan.md。当前仅空间与通行，不是潜入/守卫AI或视觉验收。

独立新场景Assets/EchoStation/InfiltrationLevel/Scenes/ES_Infiltration_Routes.unity，36×32m、72块实际源模型地面、六区，常规4.5m/中央6m，三个入口与侧向连接、后侧回廊。复用A00实际墙/地/门框网格，在新目录派生材质；旧源不修改。

Layout_A实际9项8通过，固定18帧持W的时长假设失败，其他几何/导航/实际控制器测试通过。F-345将输入夹具改持键0.5实际游戏秒、>1.2m要求，没有降低位移阈值或改旧移动代码。新鲜DLL/Console与SceneSafety/RenderSafety后，**Layout_B独立9/9，0跳过，7.852秒**。

实际面积：地面平面并集1152m²，扣除实际墙体/门框碰撞包围占地后保守下界1100.632m²，为旧96m²的11.465倍，≥864m²。门框空跨以整个包围矩形扣除会低估，不将其说成逐三角形精确面积。导航地面面积另算901.729m²，不混用这两个口径；后续B摆放后必须重新验算和烘焙导航。

通过用例：实际72地块/六区/包络与净面积；门框站立高度射线通畅、四边界射线受阻；三条不同途经区域路线分别NavMesh完整；两个闭合回环分别完整；真实CharacterController沿每条导航拐点走完三路和双回环（只在每路起点定位，中途只转向与Move，不跨障碍传送）；另在实际PlayMode用InputAction合成W/鼠标/Esc/点击验证绑定。编辑模式的控制器路径仿真与Play输入分账，未称为真人完整走图。

证据：Evidence/A01/Layout_A/B及各门禁、Created_A、Area_*、ControllerRoutes_*。旧失败保留。测试中的输入设置克隆还原、临时音频监听器/运行后台/时间和帧率均有TearDown恢复；原全局管线作用域留待A01全部结束精确还原。Protection_Before14,591文件捕获完成；最终SHA留待C，不提前判通过。

本模块门禁通过并归档，现可进入A01-B。尚未完成道具/四岗位、光影/Profiler与用户接受；不宣称目标硬件1080p60FPS。
