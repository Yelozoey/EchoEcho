# S2 D02A 文字编辑与IME事件测试报告

2026-10-25通过。Unity6000.6.7f1、Windows11开发机，普通Forward/HDR到SDR/D3D11、activeInputHandler=1保持。临时空工作区与SyntheticInputScope用于PlayMode，结束已恢复原场景布局/播放选项/Gameplay管线和同一输入设置对象及flags。

独立运行Dialogue.UI.Editor.Tests/GameTextInputTests，A2-InputEditing：**4/4通过，0失败/跳过**。框架duration3.913559秒为跨模式汇总，不是工具/切换总耗时。

实际验证：5个InputSystem中文Text事件在真实uGUI输入框只出现一次；退格/箭头/Control+A选中替换正确；组合串diao期间Enter不提交，清空组合后中文提交、明确再次Enter只发一次，200字符上限；Esc事件、禁用恢复原BaseInput覆盖且不再接收文字，重新启用可重获覆盖。没有通过设置inputField.text来代替中文键入。

证据：[原始结果](../Evidence/S2-D02/A2-InputEditing.json)、A-InputHost-WorkspaceBefore/Entered/Restored。首次夹具Key方法遮蔽枚举编译错误保留A-CompilationErrors.log，F084修复后重新执行；生产输入设定和第三方源码均未改。

这是Unity InputSystem字符/IME事件与真实uGUI组件的验证，未声称已测试Windows原生拼音候选窗口或所有硬件键盘；C阶段再验证Windows运行与截图。目标硬件性能仍待最终实机，不由本测试替代。A通过归档后进入B面板模态。
