# 全场浅灰涂装版：Editor走图

主场景：`Assets/EchoStation/InfiltrationCoated/Scenes/ES_Infiltration_Coated.unity`。

按Play，再点Game。WASD移动、鼠标观察、Space跳跃、Esc释放鼠标；Tab切换三路/巡逻示意线，F8切换中庭一块地面的实时倒影。这里没有局部样板的F5–F7材质对照，也没有T对话或可完成的潜入任务。

从入口看墙板、设备箱和终端，沿蓝线看中庭，橙线进入行政区，青线绕物流/检修通道。墙面与喷漆主体为半哑光涂层；门框、结构边条、管线、风机仍有金属反光，屏幕/玻璃/布料/橡胶和机器人配色保留。四个岗位的无背板文字已加深以适配浅墙。

新主场景重新烘焙GI与8区域反射，旧金属版和局部样板保留。四守卫依旧只展示岗位/Idle，真实检查、巡逻与Echo LLM尚未接入。

工程回归与限制见[实际Editor报告](../Tests/InfiltrationPrototype/Reports/A01-F03_Editor_TestReport.md)，接手开发从仓库根目录`HANDOFF.md`开始。本轮有效GPU采样偏高，未达到60FPS验收；不要将美术完成等同完整游戏或性能完成。
