# G01 实施细节（运行前记录）

2026-10-26。固定Qwen原始revision与HF onnx-community转换revision，见DownloadedAssets.json。原始Qwen是模型来源；ONNX转换来自HF社区，不称作Qwen官方ONNX。

- 初始CPU2计算线程、inter-op1、关闭线程自旋、顺序执行，明确CPUProvider；不自动选GPU。
- G01只做最小真实prefill/三token及生命周期，不把Spike算作正式生成器。tokenizer仅用隔离Python官方库产生参考，后续A02才开发游戏内实现。
- 原始未量化模型与INT4的logits允许量化误差；在同一teacher-forced输入上对照，预先采用cosine≥0.98、RMS误差≤1.0、top10重叠≥5作为Spike信号，另原样记录各自greedy前缀。若量化改变空白token，不猜改输出，保留差异；最终中文意图质量仍由T01判断。
- 相同INT4模型在Python ORT与Unity/Windows ORT上，最后位置logits最大绝对误差≤0.002、三步greedy token必须完全一致。若不同架构/优化导致接近并列差异，先报告具体值，不自动放宽门槛。
- 上下文长边界用1536输入tokens；此模型原图对所有位置输出logits，因此可能有较高prefill峰值，先测再决定是否在A03做等价的最终隐藏状态裁剪优化。
- Barracuda通过PackageManager添加3.0.2。添加命令返回success=false/null，但manifest与包缓存已经更新；不得因回执失败盲目重复Add。等待完整编译/域重载后才调用真实导入，详见故障F112。

技术验证计时从08:44左右开始，最多90分钟、3候选。依赖下载、Unity编译状态与实际模型异常分别留证，不把连接尚未恢复误判为模型不兼容。
