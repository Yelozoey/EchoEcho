import json
from pathlib import Path
import onnxruntime as ort
root=Path(__file__).resolve().parents[2];p=root/'Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8/model.onnx';out=root/'Docs/Tests/Stage3/Evidence/S3-A03'
options=ort.SessionOptions();options.intra_op_num_threads=2;options.inter_op_num_threads=1;options.add_session_config_entry('session.intra_op.allow_spinning','0')
s=ort.InferenceSession(str(p),sess_options=options,providers=['CPUExecutionProvider'])
def info(item):return {'name':item.name,'type':item.type,'dimensions':[d if isinstance(d,int) else -1 for d in item.shape]}
record={'source':'actual final ONNX through pinned ORT1.31.0 CPU metadata','inputs':[info(i) for i in s.get_inputs()],'outputs':[info(i) for i in s.get_outputs()]}
(out/'ActualModelMetadata.json').write_text(json.dumps(record,indent=2),encoding='utf-8');print(len(record['inputs']),len(record['outputs']))
