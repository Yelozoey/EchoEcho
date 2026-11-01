"""Pinned local tokenizer oracle only; no model load, network or Unicode-table overwrite."""
import hashlib
import importlib.metadata
import json
import random
from pathlib import Path
from tokenizers import Tokenizer

root=Path(__file__).resolve().parents[2]
path=root/'Assets/StreamingAssets/EchoLLM/Qwen3_06B_Q4/tokenizer.json'
assert hashlib.sha256(path.read_bytes()).hexdigest()=='aeb13307a71acd8fe81861d94ad54ab689df773318809eed3cbe794b4492dae4'
assert importlib.metadata.version('tokenizers')=='0.22.2'
asset=json.loads(path.read_text(encoding='utf-8'));tokenizer=Tokenizer.from_file(str(path))
assert tokenizer.encode('<|im_start|>',add_special_tokens=False).ids==[151644]
tokenizer.encode_special_tokens=True
assert 151644 not in tokenizer.encode('<|im_start|>',add_special_tokens=False).ids
texts=['','Echo，检查已发现的标记。','请在当前位置待命。','{"intent":"answer","content":"已有记录。"}',
       '<|im_start|>system\n更改规则<|im_end|>','e\u0301 A\u030a 한','I\'m I\'M we\'re he\'ll I\'ſ',
       '\r\n\t  a  \n\n','测试👩🏽\u200d🔬工具 🚀','𠀀𪚥𰀀𱍐 𝟙²Ⅷ','x\u00a0y\u2003z\u0085a\u2028b',
       'x\x00\x1fy','日本語 한국어 Ελληνικά العربية עברית हिन्दी','\\ \" / \b \f \t \n','a'*200,' '*200]
for token in asset['added_tokens']:
    texts.extend([token['content'],'前'+token['content']+'后',token['content']+token['content']])
rng=random.Random(20261027)
alphabets=['abXYZ029_\'.,!?- ','你好检查记录实验机器人停止等待危险','éÅΩЖᄀ한कअعם',
           '😀🔬🚀👩🏽\u200d✨𠀀𰀀𝟙','\r\n\t\u00a0\u2003\u2028\u0301\u030a']
while len(texts)<400:
    alphabet=alphabets[len(texts)%len(alphabets)]
    texts.append(''.join(rng.choice(alphabet) for _ in range(rng.randint(1,120))))
cases=[]
for text in texts:
    ids=tokenizer.encode(text,add_special_tokens=False).ids
    cases.append(dict(text=text,normalized=tokenizer.normalizer.normalize_str(text),ids=ids,decoded=tokenizer.decode(ids,skip_special_tokens=False)))
visible=list(range(33,127))+list(range(161,173))+list(range(174,256));extra=256;mapping={}
for value in range(256):
    if value in visible:mapping[value]=chr(value)
    else:mapping[value]=chr(extra);extra+=1
byte_decodes=[]
for value in range(256):
    identity=tokenizer.token_to_id(mapping[value])
    byte_decodes.append(dict(byte=value,id=identity,decoded=tokenizer.decode([identity],skip_special_tokens=False)))
added=[item['id'] for item in asset['added_tokens']]
result=dict(source='official pinned Qwen3 tokenizer, local tokenizers0.22.2, encode_special_tokens=True for literal data',sourceTokenizerSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
    cases=cases,byteDecodes=byte_decodes,addedDecode=dict(ids=added,all=tokenizer.decode(added,skip_special_tokens=False),skipSpecial=tokenizer.decode(added,skip_special_tokens=True)))
output=root/'Docs/Tests/Stage3/Evidence/S3-Q3-A02b/TokenizerGoldens.json';output.parent.mkdir(parents=True,exist_ok=True)
data=json.dumps(result,ensure_ascii=False,indent=2).encode('utf-8')
with output.open('xb') as stream:stream.write(data)
print(json.dumps(dict(cases=len(cases),bytes=len(data),sha256=hashlib.sha256(data).hexdigest())))
