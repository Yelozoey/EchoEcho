"""Offline validator for the shipped fixed six-intent schema; no dependencies/network."""
from pathlib import Path
import argparse,json,sys
ROOT=Path(__file__).resolve().parents[2]
SPACE=set('\t\n\v\f\r \x85\xa0\u1680\u2028\u2029\u202f\u205f\u3000')|{chr(i) for i in range(0x2000,0x200b)}
FIELDS={'follow':{'intent'},'wait':{'intent'},'investigate':{'intent','targetName','content'},'warn':{'intent','dangerTarget','content'},'answer':{'intent','content'},'refuse':{'intent','content'}}
def pairs(items):
 out={}
 for key,value in items:
  if key in out:raise ValueError('duplicate key')
  out[key]=value
 return out
def invalid_constant(value):raise ValueError('non-JSON constant')
def valid_id(value):return type(value) is str and 1<=len(value)<=64 and 'a'<=value[0]<='z' and all('a'<=c<='z' or '0'<=c<='9' or c=='_' for c in value[1:])
def case_text(case):return b''.join(int(n).to_bytes(2,'little') for n in case['utf16Units']).decode('utf-16-le','surrogatepass')
def validate(raw):
 try:
  if type(raw) is not str or not raw or len(raw.encode('utf-8','strict'))>4096:return False,None
  data=json.loads(raw,object_pairs_hook=pairs,parse_constant=invalid_constant)
  if type(data) is not dict:return False,None
  for key,value in data.items():
   key.encode('utf-8','strict')
   if type(value) is str:value.encode('utf-8','strict')
  intent=data.get('intent')
  if type(intent) is not str or intent not in FIELDS or set(data)!=FIELDS[intent]:return False,None
  if intent in ['investigate','warn'] and not valid_id(data['targetName' if intent=='investigate' else 'dangerTarget']):return False,None
  if intent not in ['follow','wait']:
   content=data['content']
   if type(content) is not str or not content or not any(c not in SPACE for c in content) or len(content.encode('utf-16-le','strict'))//2>280:return False,None
  return True,data
 except (ValueError,TypeError,UnicodeError,RecursionError):return False,None
def main():
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('path',nargs='?');parser.add_argument('--self-test',action='store_true');parser.add_argument('--report');args=parser.parse_args()
 if args.self_test:
  cases=json.loads((ROOT/'Docs/Tests/Stage3/Evidence/S3-J01/ParserCases.json').read_text(encoding='utf-8'));results=[{'name':c['name'],'expected':c['valid'],'actual':validate(case_text(c))[0]} for c in cases];out={'total':len(results),'passed':sum(r['expected']==r['actual'] for r in results),'results':results};out['success']=out['passed']==out['total']
 else:
  if not args.path:parser.error('supply JSON file or --self-test')
  try:okay,data=validate(Path(args.path).read_text(encoding='utf-8'))
  except (OSError,UnicodeError):okay,data=False,None
  out={'success':okay,'intent':data}
 if args.report:Path(args.report).write_text(json.dumps(out,ensure_ascii=True,indent=2),encoding='utf-8')
 print(json.dumps({k:v for k,v in out.items() if k!='results'},ensure_ascii=True));return 0 if out['success'] else 2
if __name__=='__main__':sys.exit(main())
