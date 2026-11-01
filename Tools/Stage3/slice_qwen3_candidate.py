"""Isolated equivalent last-position head transformation; verify numerically before use."""
from pathlib import Path
import collections,hashlib,json
import onnx
root=Path(__file__).resolve().parents[2];folder=root/'Library/EchoStation/S3Cache/models/qwen3-candidate/onnx/onnx';ev=root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3';path=folder/'model_q4_last.onnx'
assert not path.exists() and not (ev/'LastHeadTransformation.json').exists(),'Never overwrite candidate evidence'
download=json.loads((ev/'Downloads_ranges.json').read_text(encoding='utf-8'));source=folder/'model_q4.onnx'
with source.open('rb') as stream:assert hashlib.file_digest(stream,'sha256').hexdigest()==download['sha256']
budget=json.loads((root/'Docs/Tests/Stage3/Evidence/S3-I03/ResourceBudget.json').read_text(encoding='utf-8'));current=sum(p.stat().st_size for x in budget['parts'] for p in (root/x['path']).rglob('*') if p.is_file());assert current+source.stat().st_size+1024**2<20*1024**3
model=onnx.load(str(source));checker_errors={}
def check_with_explicit_ort_legacy_schema(graph,label):
 try:onnx.checker.check_model(graph,full_check=False);return
 except onnx.checker.ValidationError as error:
  checker_errors[label]=str(error)
  (ev/f'Checker_{label}.json').write_text(json.dumps({'error':str(error),'scope':'standard ONNX checker, not ORT runtime'},indent=2),encoding='utf-8')
  assert 'No Op registered for SimplifiedLayerNormalization with domain_version of 14' in str(error),'Unexpected validation failure requires review'
 legacy=[n for n in graph.graph.node if n.op_type=='SimplifiedLayerNormalization' and n.domain==''];assert legacy and any(x.domain=='com.microsoft' for x in graph.opset_import)
 # Checker-only alias: validate every remaining standard node. Never save the alias.
 # ORT registers this legacy contrib schema in the default domain; actual ORT runs
 # remain mandatory. See Microsoft's contrib_ops/cpu/layer_norm.cc.
 try:
  for node in legacy:node.domain='com.microsoft'
  onnx.checker.check_model(graph,full_check=False,check_custom_domain=False)
 finally:
  for node in legacy:node.domain=''
check_with_explicit_ort_legacy_schema(model,'original')
weights={t.name:(t.data_type,list(t.dims),hashlib.sha256(t.raw_data).hexdigest()) for t in model.graph.initializer};heads=[n for n in model.graph.node if 'logits' in n.output];assert len(heads)==1 and heads[0].op_type in ['MatMul','MatMulNBits'],'Unexpected output head requires review'
head=heads[0];old=head.input[0];name='echo_qwen3_last_hidden';index='echo_qwen3_last_index';assert index not in weights
model.graph.initializer.append(onnx.helper.make_tensor(index,onnx.TensorProto.INT64,[1],[-1]));position=next(i for i,n in enumerate(model.graph.node) if n.name==head.name);model.graph.node.insert(position,onnx.helper.make_node('Gather',[old,index],[name],axis=1,name='EchoQwen3LastHiddenOnly'));[n for n in model.graph.node if 'logits' in n.output][0].input[0]=name
for output in model.graph.output:
 if output.name=='logits':output.type.tensor_type.shape.dim[1].ClearField('dim_param');output.type.tensor_type.shape.dim[1].dim_value=1
assert weights=={t.name:(t.data_type,list(t.dims),hashlib.sha256(t.raw_data).hexdigest()) for t in model.graph.initializer if t.name!=index}
check_with_explicit_ort_legacy_schema(model,'optimized');assert checker_errors.get('original')==checker_errors.get('optimized'),'Standard checker failure changed';partial=path.with_suffix('.onnx.part');onnx.save(model,str(partial));partial.replace(path)
with path.open('rb') as stream:digest=hashlib.file_digest(stream,'sha256').hexdigest()
record={'scope':'candidate transform only; numerical equivalence pending','sourceSha256':download['sha256'],'sha256':digest,'bytes':path.stat().st_size,'originalHeadInput':old,'headType':head.op_type,'weightsUnchanged':True,'checkerLegacyLimitation':checker_errors,'remainingStandardNodesCheckedWithTemporaryAlias':True,'savedLegacyDomainsUnchanged':all(n.domain=='' for n in model.graph.node if n.op_type=='SimplifiedLayerNormalization'),'operators':dict(collections.Counter(n.op_type for n in model.graph.node)),'quantNodes':[{'name':n.name,'op':n.op_type,'attributes':{a.name:onnx.helper.get_attribute_value(a) for a in n.attribute}} for n in model.graph.node if n.op_type=='MatMulNBits']}
(ev/'LastHeadTransformation.json').write_text(json.dumps(record,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in record.items() if k!='quantNodes'}),flush=True)
