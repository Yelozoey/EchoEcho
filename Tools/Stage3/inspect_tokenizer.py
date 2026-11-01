import json
from pathlib import Path
from transformers import AutoTokenizer
from tokenizers import Tokenizer
p=Path(__file__).resolve().parents[2]/'Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8'
x=json.loads((p/'tokenizer.json').read_text(encoding='utf8'));s=x['pre_tokenizer']['pretokenizers'][0]['pattern']['Regex']
print('repr',repr(s),'backslashes',s.count('\\'))
text='你好，Echo！abc123 e\u0301 I\u2019M\n'
raw=Tokenizer.from_file(str(p/'tokenizer.json'));fast=AutoTokenizer.from_pretrained(str(p),local_files_only=True,trust_remote_code=False)
print('rawpre',raw.pre_tokenizer.pre_tokenize_str(text));print('fastpre',fast.backend_tokenizer.pre_tokenizer.pre_tokenize_str(text));print('idsEqual',raw.encode(text,add_special_tokens=False).ids==fast.encode(text,add_special_tokens=False));print('normalizer',fast.backend_tokenizer.normalizer.__getstate__())
