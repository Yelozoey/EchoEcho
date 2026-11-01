EchoStation local model derivative, 2026-10-26.
Original: Qwen/Qwen2.5-0.5B-Instruct, revision 7ae557604adf67be50417f59c2c2f167def9a775.
Original license: Apache-2.0; see LICENSE.txt and original MODEL_CARD.md.
ONNX conversion source: onnx-community/Qwen2.5-0.5B-Instruct, revision 22942cb7d7ba4cc81bb4673549ca4d4614469b5e.
The community conversion is not claimed to be a Qwen-produced ONNX release.
EchoStation modifications: ONNX Runtime1.31.0 DEFAULT symmetric weight-only INT8 block32 on168 constant MatMul nodes; accuracy_level4; Gather final hidden position before lm_head. Embedding/shared output head and other unquantized weights remain floating point. Original weights retained by these transformations except quantized main matrices.
No model training, cloud inference, remote code or runtime downloader is included.
The runtime uses a2048-token project context limit despite a larger original model limit.
ORT native/managed runtime MIT and third-party notices are distributed separately under EchoStation/AI/ThirdParty/OnnxRuntime/Licenses.
