# Barracuda 3.0.2：当前量化Qwen模型 No-Go

2026-10-26 09:02。Unity6000.6.7f1实际安装、编译成功后，在干净的S2已保存场景、非播放状态执行ONNXModelConverter(false,false,false).Convert，对象是完整786156820字节的已固定SHA的model_q4.onnx，不是替代小图。

实际抛出Unity.Barracuda.ONNX.OnnxImportException：`Unknown type Trilu encountered while parsing layer /model/Trilu_output_0.`。完整堆栈/耗时见Docs/Tests/Stage3/Evidence/S3-G01/BarracudaActualImport.json。模型图另有168个MatMulNBits，源码支持表未列此路径，但不能把后续节点称已被执行验证。导入已失败，因此不继续伪造后台/Windows成功；未展开为FP32、未改第三方算子或Unity版本。

依据用户明确选择B，切到ONNX Runtime CPU1.31.0，重新验证同一真实模型的实际后台、释放和Windows Player。此结论针对本次模型/导入路径，不声称穷尽所有Barracuda模型。

MCP长调用超时导致外部success=false/null，但Editor execute_code历史显示代码实际执行，最后一轮已在代码内部写出完整结果。此前误以为A02/A03尚未执行，后来历史证实已经执行过；停止重复导入。主机Process.PrivateMemorySize64在Unity返回0，不作有效内存证据，后续由外部Windows计数采样。
