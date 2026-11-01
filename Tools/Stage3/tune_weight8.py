"""Same third candidate weights; request supported CPU accuracy4 kernel and record unchanged tensors."""
import hashlib,json
from pathlib import Path
import onnx
root=Path(__file__).resolve().parents[2];folder=root/'Library/EchoStation/S3Cache/models/qwen-int4/onnx';ev=root/'Docs/Tests/Stage3/Evidence/S3-G01'
m=onnx.load(str(folder/'model_weight8.onnx'));before={t.name:hashlib.sha256(t.raw_data).hexdigest() for t in m.graph.initializer};count=0
for node in m.graph.node:
 if node.op_type!='MatMulNBits':continue
 for attr in list(node.attribute):
  if attr.name=='accuracy_level':node.attribute.remove(attr)
 node.attribute.append(onnx.helper.make_attribute('accuracy_level',4));count+=1
assert count==168 and before=={t.name:hashlib.sha256(t.raw_data).hexdigest() for t in m.graph.initializer}
p=folder/'model_weight8_acc4.onnx';onnx.save(m,str(p))
with p.open('rb') as f:digest=hashlib.file_digest(f,'sha256').hexdigest()
r={'tensorsUnchanged':True,'changed':'168 accuracy_level attributes from default0 to4','bytes':p.stat().st_size,'sha256':digest,'note':'Same candidate, CPU kernel tuning; new numeric/performance checks required.'}
(ev/'Weight8KernelTuning.json').write_text(json.dumps(r,indent=2),encoding='utf-8');print(json.dumps(r,indent=2))
