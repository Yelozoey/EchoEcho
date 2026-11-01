"""Read actual graph/quantization and produce small, real tokenizer fixtures for G01 only."""
import collections, hashlib, importlib.metadata, json, sys
from pathlib import Path
import onnx
from transformers import AutoTokenizer
root=Path(__file__).resolve().parents[2]
cache=root/'Library/EchoStation/S3Cache/models'
evidence=root/'Docs/Tests/Stage3/Evidence/S3-G01'
path=cache/'qwen-int4/onnx/model_q4.onnx'
m=onnx.load(str(path),load_external_data=False)
ops=collections.Counter((n.domain or 'ai.onnx')+':'+n.op_type for n in m.graph.node)
types=collections.Counter(onnx.TensorProto.DataType.Name(t.data_type) for t in m.graph.initializer)
quant=[{'name':n.name,'op':n.op_type,'domain':n.domain,'attributes':{a.name:onnx.helper.get_attribute_value(a) for a in n.attribute},'weights':list(n.input[1:])} for n in m.graph.node if 'MatMulNBits' in n.op_type]
def info(v):
 t=v.type.tensor_type
 return {'name':v.name,'dtype':onnx.TensorProto.DataType.Name(t.elem_type),'shape':[d.dim_param or d.dim_value for d in t.shape.dim]}
audit={'modelSha256':hashlib.file_digest(path.open('rb'),'sha256').hexdigest(),'modelBytes':path.stat().st_size,'irVersion':m.ir_version,'opsets':{x.domain:x.version for x in m.opset_import},'operators':dict(ops),'initializerTypes':dict(types),'packed4bitNodes':quant,'inputs':[info(x) for x in m.graph.input],'outputs':[info(x) for x in m.graph.output],'versions':{p:importlib.metadata.version(p) for p in ['onnx','onnxruntime','transformers','tokenizers','torch']}}
(evidence/'GraphAudit.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2),encoding='utf-8')
del m
tok=AutoTokenizer.from_pretrained(str(cache/'original'),local_files_only=True,trust_remote_code=False)
converted=AutoTokenizer.from_pretrained(str(cache/'qwen-int4'),local_files_only=True,trust_remote_code=False)
plan=(root/'Docs/DevelopmentPlan.md').read_text(encoding='utf-8')
start=plan.index('你是Echo，废弃观测站的轮式机器人助手。')
prompt=plan[start:plan.index('```',start)].rstrip('\n').replace('{retrieved_memory}','可用目标：power_panel_01 / 供电面板').replace('{player_input}','请去调查供电面板。')
text=tok.apply_chat_template([{'role':'user','content':prompt}],tokenize=False,add_generation_prompt=True)
ids=tok.encode(text,add_special_tokens=False)
assert ids==converted.encode(text,add_special_tokens=False),'Original and ONNX tokenizers diverge'
fixture={'text':text,'prompt':prompt,'inputIds':ids,'inputTokens':len(ids),'reference':'original pinned official tokenizer; IDs compared to pinned conversion','eos':tok.eos_token_id,'stoppingIds':[151643,151645]}
(evidence/'PrefillFixture.json').write_text(json.dumps(fixture,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'operators':dict(ops),'types':dict(types),'q4Nodes':len(quant),'inputs':audit['inputs'][:5],'outputs':audit['outputs'][:3],'tokens':len(ids),'versions':audit['versions']},ensure_ascii=False,indent=2))
