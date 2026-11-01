EchoStation Qwen3 local model package, 2026-10-27.
Upstream model: Qwen/Qwen3-0.6B, revision c1899de289a04d12100db370d81485cdf75e47ca.
Upstream license: Apache-2.0, preserved in LICENSE.txt; original model card in MODEL_CARD.md.
Upstream source: https://huggingface.co/Qwen/Qwen3-0.6B/tree/c1899de289a04d12100db370d81485cdf75e47ca
Community INT4 ONNX export: onnx-community/Qwen3-0.6B-ONNX, revision 58ca03a5e7b354b480b1a82b34bde8dc7b854cc0.
Export source: https://huggingface.co/onnx-community/Qwen3-0.6B-ONNX/tree/58ca03a5e7b354b480b1a82b34bde8dc7b854cc0
This community ONNX export is not claimed to be an official Qwen-produced ONNX file.
EchoStation change: Gather the final hidden position before lm_head; all initializers remain unchanged relative to the cached upstream INT4 graph. No training or additional weight quantization was performed.
Original INT4 graph SHA256: d43d836fc5e240df9013733ccd214972c5d21bd9ec47e574e4f1e359cf90aed0
Packaged graph SHA256: 3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303
Prefill and one cached continuation numerical equivalence were measured against the original INT4 graph; this is not an FP32-model quality claim. Details are in PROVENANCE.json and project test records.
Runtime policy: local offline CPU inference, non-thinking greedy, project context2048/input1536/generated256. No runtime downloader or cloud inference is included.
ONNX Runtime has separate MIT/third-party notices in the project runtime dependency; the final S3 build must retain those notices.
