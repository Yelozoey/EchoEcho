"""Third G01 candidate: official ORT blockwise weight-only INT8, source FP32 graph pinned."""
import hashlib,json,logging,time
from pathlib import Path
import onnx
from onnxruntime.quantization.matmul_nbits_quantizer import MatMulNBitsQuantizer,DefaultWeightOnlyQuantConfig
root=Path(__file__).resolve().parents[2];folder=root/'Library/EchoStation/S3Cache/models/qwen-int4/onnx';ev=root/'Docs/Tests/Stage3/Evidence/S3-G01'
output=folder/'model_weight8.onnx'
if output.exists():raise SystemExit('Preserve existing candidate')
logging.getLogger().setLevel(logging.WARNING)
started=time.monotonic();config=DefaultWeightOnlyQuantConfig(block_size=32,is_symmetric=True,accuracy_level=0,op_types_to_quantize=('MatMul',),bits=8)
print('Loading source graph; weight-only8 block32, activation FP32; embedding/head preserved',flush=True)
quant=MatMulNBitsQuantizer(str(folder/'model.onnx'),algo_config=config);quant.process();print('Quantized; serializing',flush=True)
quant.model.save_model_to_file(str(output),use_external_data_format=False)
nodes=[n for n in quant.model.model.graph.node if n.op_type=='MatMulNBits'];bits=[{a.name:onnx.helper.get_attribute_value(a) for a in n.attribute} for n in nodes]
assert len(nodes)==168 and all(a['bits']==8 for a in bits)
with output.open('rb') as f:digest=hashlib.file_digest(f,'sha256').hexdigest()
record={'source':'model.onnx / SecondCandidateDownloads.json','tool':'onnxruntime1.31.0 official MatMulNBitsQuantizer','algorithm':'DEFAULT round-to-nearest weight-only','bits':8,'blockSize':32,'symmetric':True,'accuracyLevel':0,'quantizedNodes':len(nodes),'bytes':output.stat().st_size,'sha256':digest,'seconds':time.monotonic()-started}
(ev/'Weight8Conversion.json').write_text(json.dumps(record,indent=2),encoding='utf-8');print(json.dumps(record,indent=2),flush=True)
