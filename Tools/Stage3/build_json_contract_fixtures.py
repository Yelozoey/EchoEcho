from pathlib import Path
import json
root=Path(__file__).resolve().parents[2]
text={'type':'string','minLength':1,'maxLength':280,'pattern':r'\S','x-maxUtf16Length':280}
target={'type':'string','minLength':1,'maxLength':64,'pattern':'^[a-z][a-z0-9_]*$'}
branches=[]
for intent in ['follow','investigate','wait','warn','answer','refuse']:
 props={'intent':{'const':intent}}
 if intent=='investigate':props['targetName']=target
 if intent=='warn':props['dangerTarget']=target
 if intent not in ['follow','wait']:props['content']=text
 branches.append({'type':'object','properties':props,'required':list(props),'additionalProperties':False})
schema={'$schema':'https://json-schema.org/draft/2020-12/schema','title':'EchoStation S3 structured intent','description':'Single strict JSON object, maximum4096 UTF8 bytes. Duplicate keys and invalid Unicode rejected before schema. C# UTF16 content length<=280 additionally enforced by validate_intent.py. No network/schema fetch.','oneOf':branches}
(root/'Docs/Stage3/intent.schema.json').write_text(json.dumps(schema,ensure_ascii=False,indent=2),encoding='utf-8')
cases=[]
def add(name,raw,valid):cases.append({'name':name,'raw':raw,'valid':valid})
def obj(intent,**fields):return json.dumps(dict(intent=intent,**fields),ensure_ascii=False,separators=(',',':'))
for intent in ['follow','wait']:add(intent,obj(intent),True)
add('investigate',obj('investigate',targetName='power_panel_01',content='我前去扫描。'),True)
add('warn',obj('warn',dangerTarget='hazard_01',content='设备存在危险。'),True)
for intent in ['answer','refuse']:add(intent,obj(intent,content='记录已归档。'),True)
add('field_order','{"content":"记录。","targetName":"panel_01","intent":"investigate"}',True)
add('json_whitespace',' \t\r\n'+obj('follow')+'\n\t ',True)
add('date_string',obj('answer',content='2026-10-26'),True)
add('escaped_surrogate_pair','{"intent":"answer","content":"\\ud83d\\ude80"}',True)
add('escaped_intent','{"intent":"\\u0066ollow"}',True)
add('escaped_newline',obj('answer',content='第一行\n第二行'),True)
add('content_280',obj('answer',content='中'*280),True)
add('astral_140',obj('answer',content='🚀'*140),True)
add('id_64',obj('investigate',targetName='a'+'7'*63,content='扫描。'),True)
short=obj('wait');add('raw_4096',short+' '*(4096-len(short.encode())),True)
bad=[('empty',''),('space',' \t\n'),('null','null'),('array','[]'),('string','"follow"'),('number','1'),('markdown','```json\n'+obj('follow')+'\n```'),('two_objects',obj('follow')+obj('wait')),('comment','/*x*/'+obj('follow')),('trailing_comment',obj('follow')+'//x'),('trailing_comma','{"intent":"follow",}'),('single_quote',"{'intent':'follow'}"),('unquoted','{intent:"follow"}'),('unknown_intent',obj('dance')),('case_intent',obj('Follow')),('case_key','{"Intent":"follow"}'),('duplicate','{"intent":"follow","intent":"wait"}'),('escaped_duplicate','{"intent":"follow","\\u0069ntent":"wait"}'),('extra_follow',obj('follow',content='跟随。')),('unknown_field',obj('wait',target='a')),('missing_content',obj('answer')),('null_content',obj('answer',content=None)),('numeric_content',obj('answer',content=7)),('boolean_content',obj('answer',content=True)),('object_content',obj('answer',content={})),('array_content',obj('answer',content=[])),('whitespace_content',obj('answer',content=' \t\r\n')),('content_281',obj('answer',content='a'*281)),('astral_141',obj('answer',content='🚀'*141)),('missing_target',obj('investigate',content='扫描。')),('null_target',obj('investigate',targetName=None,content='扫描。')),('bad_id',obj('investigate',targetName='Bad-ID',content='扫描。')),('id_65',obj('investigate',targetName='a'+'7'*64,content='扫描。')),('warn_wrong_target',obj('warn',targetName='a',content='危险。')),('both_targets',obj('warn',dangerTarget='a',targetName='b',content='危险。')),('extra_field',obj('answer',content='答复。',debug='x')),('truncated','{"intent":"answer","content":"abc'),('nan','{"intent":"answer","content":NaN}'),('raw_control','{"intent":"answer","content":"a\x00b"}'),('high_surrogate_escape','{"intent":"answer","content":"\\ud800"}'),('low_surrogate_escape','{"intent":"answer","content":"\\udc00"}'),('wrong_surrogate_pair','{"intent":"answer","content":"\\ud800\\u0041"}'),('raw_surrogate',obj('answer',content='\ud800')),('bom','\ufeff'+obj('follow')),('raw_4097',short+' '*(4097-len(short.encode())))]
for name,raw in bad:add(name,raw,False)
for case in cases:
 encoded=case['raw'].encode('utf-16-le','surrogatepass')
 case['utf16Units']=[int.from_bytes(encoded[i:i+2],'little') for i in range(0,len(encoded),2)]
 case['display']=ascii(case.pop('raw'))
folder=root/'Docs/Tests/Stage3/Evidence/S3-J01';folder.mkdir(parents=True,exist_ok=True)
(folder/'ParserCases.json').write_text(json.dumps(cases,ensure_ascii=True,indent=2),encoding='utf-8')
print(json.dumps({'cases':len(cases),'valid':sum(c['valid'] for c in cases),'invalid':sum(not c['valid'] for c in cases)}))
