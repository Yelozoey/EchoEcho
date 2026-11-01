"""Emit only the selected Qwen3 v5 text; preserve the old default template."""
import hashlib
from pathlib import Path

root=Path(__file__).resolve().parents[2]
source=(root/'Docs/Stage3/Prompts/Q3_V5_V2Contrast.txt').read_bytes()
sha=hashlib.sha256(source).hexdigest()
assert sha=='f3524a85ad232bd4077da4237280e778d5b6cc3bc0fcc95d8c069480973bcbe1' and b'\r' not in source
text=source.decode('utf-8');assert text.count('{retrieved_memory}')==text.count('{player_input}')==1
output=root/'Assets/EchoStation/AI/Context/Qwen3PromptText.generated.cs'
content='// Generated from the accepted Qwen3 v5 candidate, canonical UTF8/LF.\nnamespace EchoStation.AI.Context\n{\n public static partial class Qwen3PromptTemplate\n {\n  public const string Sha256="'+sha+'";\n  public const string Text=@"'+text.replace('"','""')+'";\n }\n}\n'
with output.open('xb') as stream:stream.write(content.encode('utf-8'))
print('Qwen3 template SHA',sha)
