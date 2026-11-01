"""G01 equivalent compatibility optimization: compute only last-position vocabulary logits."""
import hashlib,json
from pathlib import Path
import onnx
root=Path(__file__).resolve().parents[2];folder=root/'Library/EchoStation/S3Cache/models/qwen-int4/onnx';ev=root/'Docs/Tests/Stage3/Evidence/S3-G01'
m=onnx.load(str(folder/'model_weight8_acc4.onnx'));original={t.name:hashlib.sha256(t.raw_data).hexdigest() for t in m.graph.initializer}
heads=[n for n in m.graph.node if 'logits' in n.output];assert len(heads)==1 and heads[0].op_type=='MatMul';head=heads[0];old=head.input[0]
name='echo_g01_last_hidden';index='echo_g01_last_index';assert all(t.name!=index for t in m.graph.initializer)
m.graph.initializer.append(onnx.helper.make_tensor(index,onnx.TensorProto.INT64,[1],[-1]));gather=onnx.helper.make_node('Gather',[old,index],[name],axis=1,name='EchoG01LastHiddenOnly')
position=next(i for i,n in enumerate(m.graph.node) if n is head or n.name==head.name);m.graph.node.insert(position,gather)
# Re-resolve after repeated-field insertion; protobuf may move/copy the node object.
heads=[n for n in m.graph.node if 'logits' in n.output];heads[0].input[0]=name
for out in m.graph.output:
 if out.name=='logits':out.type.tensor_type.shape.dim[1].ClearField('dim_param');out.type.tensor_type.shape.dim[1].dim_value=1
after={t.name:hashlib.sha256(t.raw_data).hexdigest() for t in m.graph.initializer if t.name!=index};assert original==after
onnx.checker.check_model(m,full_check=False)
path=folder/'model_weight8_last.onnx';onnx.save(m,str(path))
with path.open('rb') as f:digest=hashlib.file_digest(f,'sha256').hexdigest()
record={'change':'Gather axis1 index[-1] immediately before lm_head MatMul; KV/layers unchanged','originalHeadInput':old,'weightsUnchanged':True,'logitsShape':[1,1,151936],'sha256':digest,'bytes':path.stat().st_size,'reason':'Full1536-position vocabulary output exceeded3GiB by about76MiB; independent numeric/memory retest required'}
(ev/'LastHeadProbeTransformation.json').write_text(json.dumps(record,indent=2),encoding='utf-8');print(json.dumps(record,indent=2))
