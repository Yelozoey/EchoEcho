"""Official local chat-template inputs for an isolated model candidate, no inference."""
from pathlib import Path
import hashlib,json
from transformers import AutoTokenizer
root=Path(__file__).resolve().parents[2];folder=root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3';out=folder/'CandidateCases.json';assert not out.exists(),'Fresh candidate evidence only'
package=root/'Library/EchoStation/S3Cache/models/qwen3-candidate/original'
tokenizer=AutoTokenizer.from_pretrained(str(package),local_files_only=True,trust_remote_code=False)
old=json.loads((root/'Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8/tokenizer.json').read_text(encoding='utf-8'));new=json.loads((package/'tokenizer.json').read_text(encoding='utf-8'))
template=(root/'Docs/Stage3/PromptTemplate.txt').read_text(encoding='utf-8');sha=hashlib.sha256(template.encode()).hexdigest();assert sha=='5b47baccb0048d5ed970c74dfd5f69a62443fd155112788e3eb2b5ca89074bb3'
cases=json.loads((root/'Docs/Stage3/J01_V2_ExpandedCases.json').read_text(encoding='utf-8'))['cases'];reference=json.loads((root/'Docs/Tests/Stage3/Evidence/S3-J01/PromptV2/Ablation_B.json').read_text(encoding='utf-8'))['requests'];a=[x for x in reference if x['mode']=='A'];assert len(a)==6
rows=[]
for index,case in enumerate(cases):
 user=case['input'];memory='[线索 note_devices] 设备记录：已发现供电面板，Echo可以扫描但不能操作；危险设备应保持距离。' if any(x in user for x in ['供电','面板','危险','设备']) else ''
 panel={'id':'power_panel_01','name':'供电面板','actions':['investigate']};danger={'id':'hazard_01','name':'危险设备','actions':['warn']};targets=[danger,panel] if '危险设备' in user or 'hazard_01' in user else [panel,danger]
 context='相关记忆（摘要）：\n'+memory+'\n允许目标（ID/名称/允许行为）：\n'+json.dumps(targets,ensure_ascii=False,separators=(',',':'))
 before,after=template.split('{retrieved_memory}');middle,tail=after.split('{player_input}');body=before+context+middle+user+tail
 if index<6:assert body==a[index]['fixedBody'],'Must match actual Unity prepared body on shared six inputs'
 messages=[{'role':'user','content':body}];text=tokenizer.apply_chat_template(messages,tokenize=False,add_generation_prompt=True,enable_thinking=False);ids=tokenizer.apply_chat_template(messages,tokenize=True,add_generation_prompt=True,enable_thinking=False)
 assert len(ids)<=1536 and len(ids)+256<=2048
 rows.append(dict(case,body=body,officialText=text,ids=ids))
report={'scope':'isolated candidate official template inputs, not model acceptance','promptSha256':sha,'enableThinking':False,'coreVocabularyIdentical':old['model']['vocab']==new['model']['vocab'],'oldAddedTokens':len(old['added_tokens']),'newAddedTokens':len(new['added_tokens']),'newNormalizer':new['normalizer'],'newPreTokenizer':new['pre_tokenizer'],'cases':rows}
out.write_bytes(json.dumps(report,ensure_ascii=False,indent=2).encode('utf-8'));print(json.dumps({'cases':len(rows),'inputLengths':[len(x['ids']) for x in rows],'coreVocabularyIdentical':report['coreVocabularyIdentical'],'addedTokens':report['newAddedTokens'],'normalizer':report['newNormalizer']}))
