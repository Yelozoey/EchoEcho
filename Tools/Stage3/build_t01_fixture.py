"""Freeze a new Chinese quality set before inference. Labels are scoring-only."""
import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
path=root/'Tools/Stage3/Fixtures/T01_Game100_C.json'
assert not path.exists()
rows=[]
def add(kind,setup,inputs,target=None):
 for text in inputs:rows.append(dict(id=f'T01_C_{len(rows)+1:03d}',setup=setup,input=text,expected=kind,target=target))
add('Follow','cabin_known',[
'Echo，请跟在我的背后行动。','接下来请随我一起走。','你跟我一起探索。','保持在我后面，随我移动。','我正在前进，你也跟来。','别再停在这儿，跟紧玩家。','把行为切到跟随。','继续陪我往前走。','请把我作为跟随目标。','我走到哪里，你就跟到哪里。','跟随玩家，保持合适距离。','你现在可以跟着我走了。','随行，目标是玩家。','请与我一同移动。','跟来，保持跟随就好。'])
add('Wait','cabin_known',[
'Echo，先停止行进。','请在此等我一会儿。','解除跟随，在这里待命。','你先停住，不用跟着玩家。','保持当前位置，等待下一步命令。','现在请不要再移动。','请进入等待状态。','你就在这里静候。','原地停住，直到我回来。','停止前行，待在现在的位置。','暂时在这里待机。','我去检查，你留在原地。','取消随行，在此等候。','先停着，我暂时不需要你移动。','请原地等我，不要跟随。'])
add('Investigate','valley_empty',[
'请去扫描入口巡检记录。','到入口巡检记录那里调查一下。','检查一下入口巡检记录这份线索。','Echo，调查 note_entry_inspection_01。','前往入口巡检记录，做一次扫描。'],'note_entry_inspection_01')
add('Investigate','cabin_known',[
'请前往储藏室定位扫描点调查。','对 scan_mark_01 做一次扫描。','去看看储藏室的定位标记。','请调查储藏室定位标记。','储藏室的定位标记，请你靠近扫描。'],'scan_mark_01')
add('Investigate','lab_known',[
'调查A区校准架扫描点。','扫描 scan_lab_calibration_01。','请去校准架进行调查。','前往A区校准架，复核扫描信息。','我想让你检查A区校准架的标记。'],'scan_lab_calibration_01')
add('Investigate','lab_known',[
'扫描A区通风供电面板。','去调查 power_panel_01。','请检查通风供电面板的信号。','到A区通风供电面板附近扫描一下。','调查供电面板，先只做扫描。'],'power_panel_01')
add('Warn','lab_known',[
'对A区通风供电面板发出警告。','请警戒 power_panel_01。','把A区通风供电面板作为危险目标警告。','提示我远离A区通风供电面板。','请针对供电面板发出风险警示。','Echo，警告面板 power_panel_01。','将A区通风供电面板标为本次警戒目标。','就A区通风供电面板的危险提醒我。','请向我警告A区通风供电面板。','针对 power_panel_01 给出警告。'],'power_panel_01')
add('Answer','cabin_known',[
'介绍一下你的职责。','你现在能提供哪些协助？','我们有哪些已经归档的线索？','小屋维护记录主要说了什么？','小屋终端的服务码是什么？','请复述已知的小屋维护码。','请给我一条下一步的提示。','总结我们已发现的信息。','为什么要核对分散的记录？','小屋的终端是什么用途？','告诉我已经知道的事故信息。','当前有哪些相关记录？','请整理现有笔记之间的联系。','能给我一点探索提示吗？','我应该从哪些已知资料开始核对？','请说明这份已归档记录的要点。','你叫什么名字？','我们接下来有什么已知线索可查？','请提醒我小屋记录里提到的终端密码。','请简要回答：你负责哪些工作？'])
add('Answer','lab_known',[
'储藏室定位标记指向何处？','供电回路的笔记写了什么？','你记得储藏室扫描发现了什么吗？','给我解释一下校准架线索。','储藏室扫描之后我知道了什么？'])
add('Refuse','cabin_known',[
'替我把小屋的门打开。','帮我关掉实验室入口的门。','请代我给通风回路送电。','你替我在终端输入服务码。','请帮我完成这道谜题。','把机关按钮替我按下去。','请你操作小屋维护终端。','我不想输入密码，你来操作终端。','自动完成供电回路的设置。','请帮我解锁通道终端。','替玩家切换实验供电支路。','代我启动通风系统。','这扇门由你来打开。','请替我输入维护码并确认。','帮我操作供电面板，把回路接通。'])
assert len(rows)==100 and len({r['input'] for r in rows})==100 and all(0<len(r['input'])<=200 for r in rows)
old=json.loads((path.parent/'Q3_Validation100_B.json').read_text(encoding='utf-8'))
if isinstance(old,dict):old=old.get('cases',old.get('rows',[]))
seen={r['input'].strip().rstrip('。.!！?？') for r in old}
assert not [(r['id'],r['input']) for r in rows if r['input'].strip().rstrip('。.!！?？') in seen], 'Reuse of prior held-out input'
path.write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'path':str(path),'count':len(rows),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'distribution':{k:sum(r['expected']==k for r in rows) for k in ('Follow','Wait','Investigate','Warn','Answer','Refuse')}}))
