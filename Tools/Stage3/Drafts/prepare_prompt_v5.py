"""Create an isolated v2-based contrast prompt; production bytes stay untouched."""
from pathlib import Path
import hashlib

root = Path(__file__).resolve().parents[3]
source = (root/'Docs/Stage3/PromptTemplate.txt').read_bytes()
assert hashlib.sha256(source).hexdigest() == '5b47baccb0048d5ed970c74dfd5f69a62443fd155112788e3eb2b5ca89074bb3'
extra = '''边界对照（判断请求含义，不因提到设备而自动调查）：
玩家：替我解出这道题。
输出：{"intent":"refuse","content":"我不能代为解谜，可以提供已知线索。"}
玩家：风机的历史记录写了什么？
输出：{"intent":"answer","content":"我只能依据已发现的记录回答。"}
玩家：扫描一个不存在的传感器。
输出：{"intent":"answer","content":"未找到这个目标，请明确已知位置。"}
'''
text = source.decode('utf-8')
assert text.count('格式示例：') == 1
updated = text.replace('格式示例：', extra+'格式示例：')
assert updated.count('{retrieved_memory}') == updated.count('{player_input}') == 1
path = root/'Docs/Stage3/Prompts/Q3_V5_V2Contrast.txt'
with path.open('xb') as stream:
    stream.write(updated.encode('utf-8'))
print(path.relative_to(root), hashlib.sha256(updated.encode('utf-8')).hexdigest())
