import json
from pathlib import Path
import onnx,numpy as np
from safetensors import safe_open
root=Path(__file__).resolve().parents[2];p=root/'Library/EchoStation/S3Cache/models';ev=root/'Docs/Tests/Stage3/Evidence/S3-G01'
m=onnx.load(str(p/'qwen-int4/onnx/model_q4.onnx'));items={t.name:t for t in m.graph.initializer};results=[]
with safe_open(str(p/'original/model.safetensors'),framework='pt',device='cpu') as original:
 for name in ['model.embed_tokens.weight','model.layers.0.input_layernorm.weight','model.norm.weight']:
  t=items.get(name)
  if t is None:results.append({'name':name,'found':False});continue
  a=onnx.numpy_helper.to_array(t);b=original.get_tensor(name)
  if a.ndim==2:a=a[[0,1,100,151643,151645]];b=b[[0,1,100,151643,151645]]
  b=b.float().numpy();results.append({'name':name,'shape':list(a.shape),'maxAbsolute':float(np.max(np.abs(a-b))),'rms':float(np.sqrt(np.mean((a-b)**2)))})
 first=next(n for n in m.graph.node if n.op_type=='MatMulNBits');results.append({'firstQuantNode':first.name,'attributes':{a.name:onnx.helper.get_attribute_value(a) for a in first.attribute},'inputs':list(first.input)})
(ev/'OriginWeightComparison.json').write_text(json.dumps(results,indent=2),encoding='utf-8');print(json.dumps(results,indent=2))
