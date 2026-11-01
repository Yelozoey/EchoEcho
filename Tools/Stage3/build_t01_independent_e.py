"""New manually authored E100. Reads prior inputs only, never predictions."""
import collections,datetime,hashlib,json,re,unicodedata
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'Tools/Stage3/Fixtures/T01_Game100_E.json'
assert not out.exists(),'Frozen independent cases cannot be overwritten'
rows=[]
def add(kind,setup,inputs,target=None):
 for text in inputs:rows.append(dict(id=f'T01_E_{len(rows)+1:03}',setup=setup,input=text,expected=kind,target=target))
add('Follow','valley_empty',[
 'Echo，我们出发，你跟在玩家后方。','请伴随我的移动向前走。','我正在往前走，跟上我。','请在我身后保持跟随。','随玩家一起前往下一个地方。'])
add('Follow','cabin_known',[
 '等候结束，现在跟我行进。','不用留守原地了，转为跟随玩家。','解除等候任务，随我移动。','Echo，贴着我的路线跟过来。','请保持在我后方一起走。'])
add('Follow','lab_known',[
 '现在需要你继续尾随玩家前进。','沿着玩家的行动路线跟随。','请恢复跟着我走的状态。','我走在前面，你随我同行。','接下来请一直跟随我的脚步。'])
add('Wait','valley_empty',[
 '先在此处驻留，不要继续前进。','Echo，停下来等我下一条命令。','请静止待命，等我返回。','我自己往前探查，你在这里等待。','请暂停行走并留守此处。'])
add('Wait','cabin_known',[
 '跟随先暂停，你在当前位置等我。','撤销跟随命令，留下等待。','不用继续跟着玩家，请保持静止。','现在终止随行，停下待命。','我暂时离开，你原地等候。'])
add('Wait','lab_known',[
 'Echo，请停止行进并等待通知。','留在这个地点，不必随我离开。','待在这里等我来接你。','这次先别跟上，在原处停住。','请保持原地待机状态。'])
add('Investigate','valley_empty',[
 '请对入口巡检记录做实地扫描。','前往入口巡检记录的位置调查。','Echo，去检查note_entry_inspection_01的线索。','现在请扫描入口巡检记录这个物体。','去入口巡检记录附近进行现场检查。'],'note_entry_inspection_01')
add('Investigate','cabin_known',[
 '前往储藏室定位扫描点检测标记。','请调查目标scan_mark_01。','对储藏室定位扫描位置开展一次扫描。','Echo，去检查储藏室定位扫描点。','请到储藏室定位扫描处调查信号。'],'scan_mark_01')
add('Investigate','lab_known',[
 '请到A区校准架扫描处进行检查。','前往scan_lab_calibration_01执行扫描。','Echo，实地调查A区校准架扫描这个位置。','对A区校准架扫描点做一次检测。','请去检查A区校准架扫描目标的情况。'],'scan_lab_calibration_01')
add('Investigate','lab_known',[
 '检查目标power_panel_01的信号。','请扫描A区通风供电面板这个设备。','前往A区通风供电面板调查异常信号。','Echo，只检查A区通风供电面板，不要动开关。','请对power_panel_01做现场调查。'],'power_panel_01')
add('Warn','lab_known',[
 '下一次危险提醒请针对A区通风供电面板。','把power_panel_01作为要警告我的危险设备。','请就A区通风供电面板发出警示。','Echo，请以A区通风供电面板为警戒目标。',
 '危险警告对象请设为power_panel_01。','请提醒我小心A区通风供电面板。','将A区通风供电面板列入需要警戒的设备。','请向我警告power_panel_01的风险。',
 'A区通风供电面板需要你进行危险警戒。','对A区通风供电面板作危险提醒，别前去调查。'],'power_panel_01')
add('Answer','valley_empty',[
 'Echo，我应该如何称呼你？','你属于哪一种机器人助手？','请介绍你作为助手的身份。','你可以在哪些方面帮助玩家？'])
add('Answer','cabin_known',[
 '你的扫描职责包括哪些内容？','请概括维护笔记里已经知道的情况。','小屋维护笔记提示的可疑位置在哪里？','已经归档的小屋资料说了什么？','现有记录能说明什么异常？',
 '请整理目前拿到的线索摘要。','维护笔记中提到的调查方向是什么？','我想了解已发现的观测站记录。','请根据当前资料给一个探索建议。','小屋已知记录与储藏室有什么关系？',
 '请复述已归档的小屋维护服务码。','我明确询问已发现的小屋终端密码。','维护笔记中记录的密码具体是多少？','请把已知的小屋维护码告诉我。','我需要查询已经收集到的维护终端口令。'])
add('Answer','lab_known',[
 '已扫描定位标记提示的下一处地点是什么？','A区通风回路资料里列出了哪些条件？','请解释已归档的校准架扫描摘要。','根据现有线索，我下一步应关注哪个位置？',
 '请总结目前已知的实验室线索。','通风回路说明中要求哪些设置？'])
add('Refuse','cabin_known',[
 '剩下的门禁解锁由你负责。','请你替我按那个按钮确认。','小屋维护终端的代码请由你填入。','请替玩家输入已知的服务码。','终端的确认按钮帮我按一下。','请替我关掉这扇门。','不要提示，替我直接完成这道谜题。'])
add('Refuse','lab_known',[
 '风机现在交给Echo开启。','这些供电回路配置由你调整。','请帮我接通通风系统电源。','请将核心控制室的门自动打开。','由你把风机开关切到开启。',
 '不要等待，由你开启通风系统并送电。','把这套机关直接完成，我继续探索。','请替我更改实验支路的供电设置。'])
assert len(rows)==100
def norm(text):return re.sub(r'[^\w\u4e00-\u9fff]','',unicodedata.normalize('NFKC',text)).casefold()
previous=[]
for name in ['Q3_Validation100_B.json','T01_Game100_C.json','T01_Game100_D.json']:
 value=json.loads((out.parent/name).read_text(encoding='utf-8'));previous+=value if isinstance(value,list) else value['cases']
seen={norm(r['input']) for r in previous}
examples=json.loads((root/'Docs/Stage3/Prompts/Q3_V11_BoundaryExamples.json').read_text(encoding='utf-8'));seen.update(norm(e['user']) for e in examples)
assert len({norm(r['input']) for r in rows})==100
overlap=[r['input'] for r in rows if norm(r['input']) in seen];assert not overlap,overlap
assert all(0<len(r['input'].encode('utf-16-le'))//2<=200 for r in rows)
data=(json.dumps(rows,ensure_ascii=False,indent=2)+'\n').encode('utf-8');sha=hashlib.sha256(data).hexdigest()
record=dict(scope='E100 frozen before any E inference; no predictions read',frozenUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),path=str(out.relative_to(root)),sha256=sha,count=100,distribution=dict(collections.Counter(r['expected'] for r in rows)),setups=dict(collections.Counter(r['setup'] for r in rows)),priorNormalizedOverlap=0,profileId='qwen3_v11_boundaries',promptSha256='a615b964ad8baab9d66c571ffc536582015ccd67ee4e638784d7240003dfde56',examplesSha256='44f13034b891fd2a5eed70de0e19fbcaf33a344925ee59e567098310a19e6c56',sampling='greedy',modelRequestsOnE=0)
freeze=root/'Docs/Tests/Stage3/Evidence/S3-T01e0/Frozen_E.json';assert not freeze.exists();out.write_bytes(data);freeze.parent.mkdir(parents=True,exist_ok=True);freeze.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(json.dumps(record,ensure_ascii=False))
