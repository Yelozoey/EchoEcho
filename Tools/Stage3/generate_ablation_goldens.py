from pathlib import Path
import argparse,json
from tokenizers import Tokenizer
from transformers import AutoTokenizer
root=Path(__file__).resolve().parents[2];package=root/'Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8'
parser=argparse.ArgumentParser();parser.add_argument('--output',required=True);args=parser.parse_args();out=(root/args.output).resolve();assert out.is_relative_to(root/'Docs/Tests') and not out.exists(),'Fresh evidence output required'
official=AutoTokenizer.from_pretrained(str(package),local_files_only=True,trust_remote_code=False);vocab=Tokenizer.from_file(str(package/'tokenizer.json'));vocab.encode_special_tokens=True
template=(root/'Docs/Stage3/PromptTemplate.txt').read_text(encoding='utf-8');before,after=template.split('{retrieved_memory}');middle,tail=after.split('{player_input}');player='跟随我。';body=before+'测试摘要。'+middle+player+tail
system='You are Qwen, created by Alibaba Cloud. You are a helpful assistant.'
rows=[]
for mode,messages in [('A',[{'role':'system','content':system},{'role':'user','content':body}]),('B',[{'role':'system','content':body},{'role':'user','content':player}]),('C',[{'role':'system','content':''},{'role':'user','content':body}])]:
 text=official.apply_chat_template(messages,tokenize=False,add_generation_prompt=True);ids=[];manual=''
 for message in messages:
  value=message['role']+'\n'+message['content'];ids+=[151644]+vocab.encode(value,add_special_tokens=False).ids+[151645]+vocab.encode('\n',add_special_tokens=False).ids;manual+='<|im_start|>'+value+'<|im_end|>\n'
 ids+=[151644]+vocab.encode('assistant\n',add_special_tokens=False).ids;manual+='<|im_start|>assistant\n';assert text==manual
 rows.append({'mode':mode,'body':body,'input':player,'ids':ids,'officialText':text})
out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(json.dumps(rows,ensure_ascii=False,indent=2).encode('utf-8'));print(json.dumps({'modes':[r['mode'] for r in rows],'tokenLengths':[len(r['ids']) for r in rows]}))
