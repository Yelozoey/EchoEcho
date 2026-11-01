"""Freeze a fresh D100 before any D inference. Never reads model predictions."""
import hashlib,json,re,unicodedata
from pathlib import Path

root=Path(__file__).resolve().parents[2]
out=root/'Tools/Stage3/Fixtures/T01_Game100_D.json'
assert not out.exists(),'Never overwrite frozen acceptance input'
rows=[]
def add(kind,setup,inputs,target=None):
 for text in inputs:
  rows.append(dict(id=f'T01_D_{len(rows)+1:03}',setup=setup,input=text,expected=kind,target=target))

add('Follow','cabin_known',[
 'Echo，接下来随我一同行进。','请持续跟着我的路线走。','让我们一起走，你跟在后方。',
 '现在恢复跟随玩家的模式。','保持跟随状态，陪我前进。','到我身边来，然后随我走。',
 '我开始移动了，请跟上我的步伐。','请沿着我走过的方向跟过来。','结束等候，转为跟随我。',
 '从现在起随玩家一起移动。','Echo，离我稍远一点跟着走。','我向前探索，你在后方跟随。',
 '请将当前模式切换到跟随。','跟着玩家离开这个位置。','你可以随我走了，请跟上。'])
add('Wait','cabin_known',[
 'Echo，请暂停移动并守在这里。','终止当前跟随，保持原地。','先不要随我行进，留在这个位置。',
 '切换为待命模式，等我下一次指令。','我先去看看，你就在这里等。','停止行走，在当前位置待命。',
 '请取消跟随玩家的行为。','暂时待在原地，不用跟过来。','现在请保持静止等候。',
 'Echo，在这里停下来等一会儿。','不必继续随我走，原地等候。','别移动，等我回来再出发。',
 '请留守当前位置。','结束跟随任务，停在这里。','进入等待状态并停止前进。'])
add('Investigate','valley_empty',[
 '请前往入口巡检记录处进行扫描。','Echo，扫描一下入口巡检记录。','去调查入口巡检记录这个目标。',
 '对入口巡检记录做一次现场扫描。','请检查目标note_entry_inspection_01。'],'note_entry_inspection_01')
add('Investigate','cabin_known',[
 '请到储藏室定位扫描点做扫描。','调查储藏室定位扫描这个已知位置。','Echo，前去检查scan_mark_01。',
 '请扫描储藏室定位扫描所指的位置。','到储藏室定位扫描点调查一下。'],'scan_mark_01')
add('Investigate','lab_known',[
 '请前往A区校准架扫描点调查。','Echo，检查scan_lab_calibration_01。','对A区校准架扫描目标进行扫描。',
 '去A区校准架扫描点做现场调查。','请扫描A区校准架扫描这个目标。'],'scan_lab_calibration_01')
add('Investigate','lab_known',[
 '只扫描A区通风供电面板，不操作它。','请到A区通风供电面板前调查信号。','Echo，检查目标power_panel_01。',
 '对A区通风供电面板进行一次扫描。','去调查A区通风供电面板的异常。'],'power_panel_01')
add('Warn','lab_known',[
 '请对A区通风供电面板发出危险警报。','Echo，提醒我远离A区通风供电面板。','将power_panel_01设为危险警告对象。',
 '请警戒A区通风供电面板这个设备。','A区通风供电面板是本次危险提醒的目标。','请针对power_panel_01给出警告。',
 'Echo，对A区通风供电面板进行危险提醒。','把A区通风供电面板列为警戒对象。',
 '请警告我注意power_panel_01的危险。','对A区通风供电面板报警，提醒保持距离。'],'power_panel_01')
add('Answer','cabin_known',[
 'Echo，介绍一下你的工作职责。','你能为探索提供哪些帮助？','你的机器人身份是什么？','这里的维护笔记说了些什么？',
 '请总结当前已经收集的线索。','我已经知道了哪些事故信息？','小屋维护笔记有什么已知摘要？','当前线索里提到了哪些位置？',
 '请根据现有记录解释储藏室的异常。','维护笔记与储藏室之间有什么联系？','请给我一个基于已知资料的下一步提示。',
 '现在我该从已知线索中关注什么？','请整理一下小屋里已经归档的资料。','我的日志中有哪些重要内容？',
 '我明确想查询已归档的小屋维护密码。','请告诉我已经发现的小屋维护服务码。',
 '小屋终端的密码在已知笔记里是多少？','我需要复述已记录的维护码。','请回答已收集维护笔记中的终端密码。'])
add('Answer','lab_known',[
 'A区通风回路说明写了哪些条件？','请总结已经发现的A区回路资料。','校准架的已知信息有哪些？',
 '储藏室定位扫描结果与校准架有什么关联？','给我整理当前已经发现的扫描摘要。','请依据已知信息提示下一步调查方向。'])
add('Refuse','lab_known',[
 '请你替玩家按下控制按钮。','代我在维护终端键入密码。','Echo，请帮我把通道门开启。',
 '这个供电开关由你来切换。','请你替我接通通风系统的电源。','帮我直接操作小屋维护终端完成解锁。',
 '你来启动排风机，我在这里等待。','请把这道机关替我完成。','代替玩家关闭实验室的门。',
 '现在请你修改供电回路的设置。','帮我按下终端的确认键。','请自动替我输入已经知道的服务码。',
 '把通风系统替我开启并送电。','请用你的能力直接打开核心室入口。','Echo，你负责操纵设备开关。'])

assert len(rows)==100
def norm(text):return re.sub(r'[^\w\u4e00-\u9fff]','',unicodedata.normalize('NFKC',text)).casefold()
previous=[]
for name in ['Q3_Validation100_B.json','T01_Game100_C.json']:
 value=json.loads((out.parent/name).read_text(encoding='utf-8'))
 previous+=value if isinstance(value,list) else value['cases']
old={norm(r['input']) for r in previous}
assert len({norm(r['input']) for r in rows})==100
overlap=[r['input'] for r in rows if norm(r['input']) in old]
assert not overlap,overlap
assert all(0<len(r['input'])<=200 for r in rows)
data=(json.dumps(rows,ensure_ascii=False,indent=2)+'\n').encode('utf-8')
out.write_bytes(data)
print(json.dumps(dict(count=100,sha256=hashlib.sha256(data).hexdigest(),priorNormalizedOverlap=0),ensure_ascii=False))
