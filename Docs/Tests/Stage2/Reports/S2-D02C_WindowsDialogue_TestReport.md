# S2 D02C Windows对话界面测试报告

2026-10-25最终通过。Windows11 10.0.26300、i7-12650H、RTX4060Laptop，Unity6000.6.7f1，实际1920×1080窗口、D3D11、ES_Gameplay_ForwardHDR、内部HDR/SDR、显示HDR关闭、UseLLM=false。目标最低/推荐硬件和Windows10未在此实测，沿用总文档基准，不据开发机UI验证声称60FPS达标。

## 构建和实际过程

独立产物 `Builds/Tests/S2-D02/EchoStationDialogue.exe`，使用原S1 Demo场景与CLI启动的UI诊断，不改原场景、不覆盖S1包。最终Build Succeeded、0错误、2条既有ServicesCore/Windows.Forms警告，31.5234407秒。首构建及新API弃用警告保留PlayerBuild-First，已改自有查找API，不为消除服务提示连接云服务。

以 `--s2-dialogue-report <隔离目录>/Report.json` 显示1080p测试窗口，真实Windows Player执行：T打开；空输入拒绝；InputSystem的8个中文Text事件只出现8字“调查储藏室可疑点”；Enter只提交一次，实际两个气泡；与真实J日志/Tab控制双向互斥；实际鼠标命中发送按钮再提交一次；110条追加后只保100条并复用100行；Esc关闭，鼠标保持释放，原BaseInput覆盖及诊断输入设备/设置恢复。**Report.passed=true，进程退出码0。**

## 图像和证据

- [最终回执](../Evidence/S2-D02/WindowsUI02/Report.json)、[真实退出码](../Evidence/S2-D02/WindowsUI02/Exit.json)、Player.log、Process.json。
- [对话截图](../Evidence/S2-D02/WindowsUI02/Dialogue.png) 和 [长历史截图](../Evidence/S2-D02/WindowsUI02/History.png)，均1920×1080，已逐张查看：中文无方框、气泡与输入栏/发送按钮可读，历史受视口裁剪，未出现越界遮挡。底层S1 HUD仍属于此诊断场景，不是S2完整HUD交付。
- C-FinalProtected：35项原模板/管线/小屋/导航/烘焙/S1包哈希全部不变；后置SceneSafety/RenderSafety允许、原场景干净、Gameplay绑定保持、Console零错误。

首窗口WindowsUI01在startup阶段NullReference，未执行文字输入，F085记录并保留。复测新增诊断确认startupEventSystem=true、startupInputModule=false；等待当前模块就绪后完整通过，支持首帧模块激活时序的根因。Player.log保留既有未启用Panini剥离提示（F019）和引擎退出统计，不把它们隐藏或误称全部后处理失效。

覆盖范围为Windows真实运行中的InputSystem字符/按键/鼠标事件及uGUI，不是直接赋值InputField.text；没有声称覆盖Windows原生拼音候选窗口和所有硬件键盘。此步骤用受控Echo文字检查UI，尚未接D03/D04命令/执行适配，不能当作完整脚本对话或S2玩法已完成。
