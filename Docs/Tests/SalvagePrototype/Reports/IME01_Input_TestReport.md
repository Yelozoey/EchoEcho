# IME01 中文输入修复实际记录

**最终状态：本项通过并完成。** 2026-10-31，用户取消防火墙后，同一Game_B封包以Native_C新进程复测，exit0；原生微软拼音89次组合更新、12次CJK提交、60条聚焦且有效的原生上下文状态，关闭面板后NativeMode=Auto/bridge=false/context=0恢复。用户直接确认候选/发送/关闭重开“三项都正常”。人工确认与事件汇总Native_C_Acceptance.json；自动17/17保持，旧Native_B受阻回执不覆盖、不计通过。

发行503文件及GameTextInput源码SHA与已封清单逐项一致；本轮未改包、未重建。FinalWorkspace_C再次确认原S3场景干净、VC/PC/AA4、普通Forward/内部HDR/SDR/D3D11及三配置native/disk/dirty精确一致，测试进程已正常关闭。用户已完成本项本人验收，下一LLM短任务尚未启动。

覆盖限本机Windows11/微软拼音；未证明其他IME/Windows10另机/目标机性能。提交事件总5次来自整轮多次试输，不能仅按总计反推单次去重；候选Enter不误发/再次Enter单次发送由受控测试及用户直接确认共同支持。以下保留排查、失败及历史待测过程。

2026-10-31。范围仅旧回收版T通信/微软拼音，LLM/训练/飞行未启动。产品修改仅Player/Runtime/GameTextInput.cs：Windows Player真实键盘聚焦时同步引擎IME入口，关闭/换键盘恢复原模式；字符与组合仍只从InputSystem接收，不重复读取IMGUI。未修改Active Input Handling=New、包版本或全机输入法配置。

## 定位证据

同一独立Windows Probe_A中，焦点true、中文布局00000804、请求On，但context=0、imeSelected=false、组合事件0。引擎原生IME入口实际执行于frame224259，之后组合45次、CJK字符6次、32条状态具有非零上下文。用户最初回复“仍只能输入英文”时日志尚无legacy-on，不能把此回复写成原生入口已经执行后的可靠A/B结论；后续有执行收据及实际组合/汉字事件。原始Native_A保留，不用注入汉字冒充微软拼音。

## 独立测试

修复前Baseline_A原有文字4/4通过，仍有原生故障，说明旧覆盖缺口。Fix_A编译门禁拒绝（F320，无Job）；Fix_B11项8过3清理失败（F321，原始回执保留）。修复夹具后Fix_C实际 **11/11**，10.923852秒，Job428647fc69744f75a124b54aed126c0f：命令启用/结果、虚拟键盘不触发原生桥、关闭还原、重开、组合Enter/Esc/提交隔离，以及原有文字4项/对话面板4项。每个请求group均有实际fullName，不以编译就绪当测试通过。

证据根：Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/IME01。新回收版/终端受影响回归、独立修复Windows包与原状态恢复仍在执行；本报告当前不是最终交付/用户验收完成声明。

受影响回归Regression_A实际 **6/6**，14.0370638秒，Job9446ecb727b6478a860b9a20e176fca6：NextPanelTests三项和TerminalIntegrationTests三项，原脚本路由/偏好/模态/存档与终端独立E操作保持；与Fix_C合计17项，不含先前Baseline_A的重复计数。新包标签Game_B，正在构建，尚未做原生修复后验收。

Game_B已构建：42.0548933秒、0错误/11警告，原始messages完整保留，包括既有Mesh预烘焙、Services未关联、验证代码弃用/FindFirst弃用和Windows.Forms提示，不为消提示升级包或关联云。发行程序Builds/LLMShortTasks/IME01/Game_B/EchoStationIME.exe；新输出模型副本1,902,692,739B剥离，构建scope四配置native/disk/dirty全部精确恢复。两轮峰值21.184/21.432GiB，均小于22GiB。

Native_B启动受Windows防火墙权限弹窗阻挡，已请用户手动取消；代理没有操作安全权限。其后原生候选/发送/关闭重开仍待实测，不能把弹窗期间IME关闭状态算失败或通过。原S3场景/VC管线/PC/AA4及三配置native/disk/dirty已精确恢复，FinalWorkspace.passed=true；旧文件最终SHA核验在执行。

Native_B等待600秒后由测试外层终止本轮自有进程，exit1，Timeout/Process原样保留。此轮受阻，不计修复通过。Game_B候选封包503文件/266,088,231B（约253.76MiB），无模型权重或Validation副本，发行SHA清单DeliveryManifest_Game_B.json。等待用户在同包实际T面板确认微软拼音候选/提交/重开；尚未进入下一短任务。

## 最终收尾与当前验收边界

Protection_After实际12808项旧文件SHA核验通过，仅授权产品文件Assets/EchoStation/Player/Runtime/GameTextInput.cs发生变化，unexpected=[]，原模型/旧发行包不变。FinalWorkspace/FinalWorkspaceReceipt/Scope_restore_A证明原场景S3、VC普通Forward/内部HDR/SDR、PC/AA4及Graphics/Quality/EditorSettings完整native/disk/dirty精确恢复；FinalConsole0。回退AA同步只接受记录中的临时GUID/派生AA0组合，完整门禁未放宽。没有在恢复后的VC白名单外运行测试。

既定资源根最终19.662965GiB/20，Game_B封包SHA/source SHA已保存。首次Native_A诊断exit0，Native_B修复后原生受阻exit1，不混为“Windows已验证通过”。结论为修复候选已交付、自动17项通过、本人原生IME复测待完成；不宣称目标机性能、所有输入法兼容或LLM能力已达标。本轮到此等待本人测试。
