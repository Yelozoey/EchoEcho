# 用户手动下载，暂停后续处理

2026-10-27最新覆盖：用户已明确恢复**仅Qwen3-0.6B调优和Prompt迭代**；Qwen2.5相关下载/转换/适配继续暂停。下文是此前手动下载等待的历史记录，不阻止已授权Qwen3工作，也不表示允许处理用户正在手动下载的2.5文件。当前路线见Qwen3TuningPlan.md。

2026-10-26（Asia/Shanghai）。用户明确要求提供网址，由用户尝试下载，下载完再通知继续。立即暂停S3自动下载、转换、推理与开发；不自行恢复。S3保持11/24，J01未通过。非root下载工具任务已中断，没有取得实际source成功。

固定文件：https://huggingface.co/onnx-community/Qwen2.5-1.5B-Instruct/resolve/d54957e3af1be04546436e874a60fe386041b244/onnx/model.onnx_data?download=true

保存为：E:\Projects\Unity\EchoStation\Library\EchoStation\S3Cache\models\qwen15-candidate\onnx\onnx\model.onnx_data

预期完整大小6208411648字节，SHA256为7111acf0e546fe192035357890402832315b4c243e56b01b47ce9358285fc82e。配套model.onnx小图与官方配置/分词/许可已经取得，不需用户重复下载。

原HTTP Weights_A/B均失败，全部日志保留；B的9个已记录片段506367465字节已独立打包复核到http_progress_B.bin，SHA4dc5c0fec14ad303a342fad5836f3d0a46864e121d76f8d1c1923bd2bfe3bf2b。原6.2GB预分配.part已按绝对路径/保留备份SHA门禁移除，避免重复占用；HTTPProgressArchive_B/Cleanup_B和ResourceBudget_C可复核。S3当前统计约10.61GiB，实际用户下载完成后必须重新统计。

用户通知继续后：先核验手动下载的实际路径/完整字节/SHA及20GiB资源，再运行已独立6/6通过的量化工具（F132/F134修复、QuantToyTests_B.log）；实际量化及其外采峰值内存尚未运行。然后后台Python参考、真实Unity18条门槛，只有通过才继续正式适配。新Xet source工具正在准备时中断，最后一次反馈其测试夹具SyntaxError、未完成独立验收，不能把该工具当通过或继续自动下载。手动文件通过后无需为了重跑传输研究而下载第二份。
