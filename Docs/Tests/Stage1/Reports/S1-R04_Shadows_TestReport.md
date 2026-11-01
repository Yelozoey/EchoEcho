# S1-R04 实时光、CSM与距离阴影实际测试报告

2026-10-25；模块v0.1。**独立功能通过：最终Editor47/47，Windows x64 Player退出0、图像检查passed=true，六项交付审计全部通过。** 不属于目标硬件60FPS或正式美术验收。

## 环境与前置

Unity6000.6.7f1 / URP17.6普通Forward / D3D11 / 内部HDR→SDR /1920×1080。开发机Windows11 10.0.26300、i7-12650H、RTX4060 Laptop 7956MB、16122MB RAM。用户基准：i5-8400/GTX1060 6GB/16GB中画质1080p60FPS；i5-12400/RTX3060 6GB/16GB高画质1080p60FPS；这两套硬件未实际测试。

R01–03先前独立通过；本模块先A配置与距离策略13/13，再B实际渲染2/2归档，之后C持久化/Windows/回归。参见同目录R04A与R04B报告。最终47包括既有28、策略13、图像/绘制2、光源预制体/频闪4，0失败/跳过，17.973秒。

## 交付与实际测试

- Pipeline四级2048 CSM、40m、累计分割.1/.25/.5、Border.1；仅这三个输入相对R03变化，其他继承参数继续由R01漂移测试约束。
- DistanceShadowCaster：到bounds最近点30m停投，28m恢复、.1s检测，指定相机；保留enabled/receiveShadows、原TwoSided/ShadowsOnly/Off；实际PlayMode验证启停/销毁恢复和相机移动。
- ES_Flashlight、ES_ScanLight、ES_FaultLight三份Realtime软阴影预制体；高/中/中附加阴影档1024/512/512；故障灯实际强度2→.16→2，禁用恢复2。参数见模块表。
- Windows实际主光阴影差0.00082425、灯开关差0.32363090；Spot阴影差0.00008719、灯开关差0.00082675，均超过原有断言。四级GPU索引均有像素，实测密度105.15/42.69/21.45/10.75像素每米。
- Windows相机3至41m共11点；3.9/4.1、9.9/10.1、19.9/20.1均仍有实际阴影。31/39/41m不投影，对显式Off的图像差为0，同时实际显隐差.00032697/.00018318/.00016470证明物体仍绘制。
- 官方Frame Debugger On14事件含4个Shadowmap SRPBatch绘制提交，Off8事件且0阴影提交。单SRPBatch可含多个底层draw，详细draw数当前不可用，不将4解释为4个draw。
- SHA保护12份模板/前模块资产全部不变；共享Renderer不改，量化中临时SSAO禁用最终恢复。原始模板始终保留。

## 可复测入口与证据

Unity菜单`Tools/EchoStation/Stage 1/R04`含配置、夹具、光源模板和Build Validation Player；NUnit程序集`EchoStation.Rendering.Editor.Tests`；构建后`Tools/Rendering/RunR04Player.ps1`运行隐藏D3D11程序并保存退出码，45秒超时只终止自建子进程。审计命令`python Tools/Rendering/audit_r04.py`。

原始目录`Docs/Tests/Stage1/Evidence/S1-R04/`：`A-Retest.json`、`B-Final.json`、`C-Lights.json`、`EditMode-Final.json`、`PlayerBuild.json`、`FinalAudit.json`；`Player/PlayerProbe.json`、`Exit.json`、`Player.log`及8张1080p实际PNG；`Editor/FrameDebugger-On.json`、`FrameDebugger-Off.json`。构建49.284秒，0 errors/2 warnings。

## 故障、警告与限制

F-029工具输入、F-030测试生命周期、F-031命名空间/零测试假通过风险均记录修正，保留失败证据。F-032通过官方窗口和跨Editor更新取得真实提交事件；即时详细事件数据仍不可用，作用域外enabled=false不单独证明捕获未启动，不能混用ProfilerID与运行期session GUID。所有历史尝试均保留。

两条既有构建警告：Services项目未关联与System.Windows.Forms平台依赖（F-011）；运行四条禁用DOF/Panini Shader剥离提示（F-019）。本模块无新Shader编译警告/运行异常，不为了消提示启用未用效果或云服务。

Editor附加场景测试与独立Player的部分绝对亮度不同；本次要求同环境因果开关、GPU级联及距离策略，均分别通过。未作跨环境绝对色度验收，也不猜测所有残余照明来源已定位。此夹具不是最终小屋画面，不以简场景宣称性能目标达成。

结论：R04模板/距离策略功能、实际渲染及持久化通过，允许进入R05；全S1仍未完成。
