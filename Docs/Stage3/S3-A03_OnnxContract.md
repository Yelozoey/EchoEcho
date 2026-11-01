# S3-A03 ONNX输入/输出/KV合同

2026-10-26，前置A02已归档。先审查已安装ORT1.31.0实际API，正式后端纯C#，不持有Unity对象；仅Owner线程创建/运行/释放，显式禁止提供的Unity主线程ID。关闭ORT遥测，CPU2/intra、1/inter、顺序执行、禁自旋，沿用G01数值门槛。

正式合同：51输入（int64 input_ids/attention_mask/position_ids +24层key/value float32），49输出（float32 logits[1,1,151936]+48个present[1,2,序列长,64]）。prefill1–1536，初始past长度0；mask全1、position0起；每步追加1，完整KV长度不能超过2048。参数维度/dtype/name/count和实际输出有限值均校验，未知/重复/坏形状拒绝，故障不能继续旧缓存。

使用OrtValue保留上一轮原生present作为下一轮past；新Run完成后才释放旧输出，避免每token复制48份KV和全部词表。logits只提供Owner期内只读Span；不把Tensor交给主线程或跨请求复用。A04才加完整生成/EOS，I01才加正式单Owner调度器，本模块不提前整合。

独立测试按子步：纯合同无效IDs/边界/形状；真实固定189tokens连续三步，对照Python每位置logits≤0.002及token前缀；真实1/短/1536输入和位置/KV长度；缓存reset/五轮创建释放；NaN/坏dtype/超长/错误Owner拒绝；真实包Lease与后台所有权。所有实际Run不在主线程，S1/S2保持。
