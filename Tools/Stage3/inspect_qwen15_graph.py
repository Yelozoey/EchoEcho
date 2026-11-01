"""Metadata-only official opset migration/head rewrite of the real descriptor."""
from pathlib import Path
import hashlib,json
import onnx
from quantize_qwen15_candidate import prepare_graph,SOURCE_GRAPH_SHA,SOURCE_GRAPH_BYTES

root=Path(__file__).resolve().parents[2]
source=root/'Library/EchoStation/S3Cache/models/qwen15-candidate/onnx/onnx/model.onnx'
output=root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen15/DescriptorMigration_A.json'
assert not output.exists() and source.stat().st_size==SOURCE_GRAPH_BYTES
assert hashlib.sha256(source.read_bytes()).hexdigest()==SOURCE_GRAPH_SHA
model=onnx.load(source,load_external_data=False)
before={t.name:hashlib.sha256(t.SerializeToString()).hexdigest() for t in model.graph.initializer}
reduce_before=sum(n.op_type=='ReduceMean' and any(a.name=='axes' for a in n.attribute) for n in model.graph.node)
transform=prepare_graph(model,lazy_head=True,strict_candidate=True)
current={t.name:t for t in model.graph.initializer}
assert all(name in current and hashlib.sha256(current[name].SerializeToString()).hexdigest()==sha for name,sha in before.items())
reduce_after=sum(n.op_type=='ReduceMean' and len(n.input)==2 and all(a.name!='axes' for a in n.attribute) for n in model.graph.node)
assert reduce_before==reduce_after==57
assert transform['sourceOpset']==14 and transform['targetOpset']==21 and transform['opsetMigrated']
assert list(current['echo_qwen15_head_weight_fp32'].dims)==[1536,151936] and not current['echo_qwen15_head_weight_fp32'].raw_data
assert not current['model.embed_tokens.weight'].raw_data
record={'scope':'actual source descriptor metadata migration only; no external weight load, full checker or native model acceptance',
        'passed':True,'sourceGraphSha256':SOURCE_GRAPH_SHA,'originalInitializerDescriptorsUnchanged':len(before),
        'reduceMeanAttributesMigrated':reduce_after,'opsets':[{o.domain:o.version} for o in model.opset_import],
        'transform':transform,'externalWeightsLoaded':False}
output.write_bytes(json.dumps(record,indent=2).encode('utf-8'))
print(json.dumps({'passed':True,'initializerDescriptorsUnchanged':len(before),'reduceMeanMigrated':reduce_after}))
