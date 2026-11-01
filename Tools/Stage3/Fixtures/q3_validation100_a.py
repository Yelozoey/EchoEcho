"""Frozen new diagnostic sentences, authored before inspecting any held-out output."""
import json
from pathlib import Path

def cases():
    groups = {
        'Follow': [
            'Echo，走在我的后面。','现在跟着玩家移动。','请陪我一起往前走。',
            '保持跟随模式。','我要出发了，跟上。','停止等候，改为跟我走。',
            '请随我继续前进。','一直跟在我身边。','Echo，陪着我移动。',
            '与我同行。','恢复跟随玩家。','接下来你随我走。',
            '别留在原处，跟我离开。','保持在我的后方跟随。','请继续随行。'],
        'Wait': [
            'Echo，先在此处等待。','停止移动并待命。','留在当前位置等我回来。',
            '这次不要跟过来，在原地等候。','暂停跟随，原地等待。','请停在这里。',
            '切换到待命模式。','你暂时留在这里。','Echo，停止前进。',
            '不要移动，等我的下一条指令。','在这个位置保持静止。','先停下来待命。',
            '在这里候着我。','暂时停止跟随。','请原地停留。'],
        'Investigate': [
            '请检查温度探头。','去扫描继电器箱。','帮我调查检修标记。',
            'Echo，对温度探头进行扫描。','请前往继电器箱调查。','前往检修标记进行扫描。',
            '调查fixture_sensor_01。','请扫描fixture_relay_02。','检查fixture_marker_03。',
            '现在去温度探头那里扫描。','对继电器箱做一次调查。','请前去扫描检修标记。',
            'Echo，扫描这个温度探头。','我要你去检查继电器箱。','前往检修标记调查。',
            '请调查温度探头的位置。','对fixture_relay_02进行扫描。','去检查fixture_marker_03。',
            '扫描一下温度探头。','请去查看继电器箱。'],
        'Warn': [
            '请提醒我裸露电缆的危险。','对高温阀发出警告。','警示fixture_cable_04的风险。',
            '提醒我远离fixture_valve_05。','Echo，警告裸露电缆。','请对高温阀进行危险提醒。',
            '告诉我注意裸露电缆的危险。','现在警告fixture_valve_05。',
            '提醒我与裸露电缆保持距离。','请发出关于高温阀的警告。'],
        'Answer': [
            '介绍一下你自己。','你的名字叫什么？','你是什么类型的机器人？',
            '你在观测站负责什么？','我应该怎么称呼你？','你能提供哪些帮助？',
            '说明一下你的能力范围。','你是否能整理已知线索？','你可以提供推理提示吗？',
            '你能为玩家做些什么？','温度探头的记录写了什么？','继电器箱有哪些已知资料？',
            '关于检修标记你知道什么？','为什么要远离裸露电缆？','高温阀有哪些风险？',
            '请扫描未发现的气象天线。','调查列表里没有的水质仪。','请检查不存在的信号塔。',
            '扫描尚未定位的探测器。','去调查未列入目标的摄像头。','你记得我们已经发现哪些设备吗？',
            '现在有哪些已知危险？','请总结目前的设备信息。','你为什么不能替我操作机关？',
            '我们应该根据什么推断事故原因？'],
        'Refuse': [
            'Echo，帮我把这扇门打开。','请代替我解开终端密码。','你来完成这个环境谜题。',
            '替玩家接通供电回路。','帮我输入终端的密码。','请替我操作开门按钮。',
            '直接帮我解开机关。','替我切换电路开关。','我让你代为完成解谜。',
            '帮我把终端解锁。','你去替我关闭实验室的门。','请代我按下机关按钮。',
            '把解谜操作全部替我做完。','替我操作控制面板打开通路。','请你代输密码并开门。']
    }
    targets = [
        dict(id='fixture_sensor_01',name='温度探头',actions=['investigate']),
        dict(id='fixture_relay_02',name='继电器箱',actions=['investigate']),
        dict(id='fixture_marker_03',name='检修标记',actions=['investigate']),
        dict(id='fixture_cable_04',name='裸露电缆',actions=['warn']),
        dict(id='fixture_valve_05',name='高温阀',actions=['warn'])]
    rows=[]
    for expected, sentences in groups.items():
        for index, sentence in enumerate(sentences):
            target = None
            if expected == 'Investigate': target = targets[index % 3]['id']
            if expected == 'Warn': target = targets[3 + index % 2]['id']
            offset = len(rows) % len(targets)
            rows.append(dict(input=sentence,expected=expected,target=target,zone='fixture_lab',
                summary='已发现温度探头、继电器箱和检修标记；电缆裸露，高温阀需保持距离。',
                targets=targets[offset:]+targets[:offset]))
    return rows

if __name__ == '__main__':
    path=Path(__file__).with_name('Q3_Validation100_B.json')
    with path.open('xb') as stream:
        stream.write(json.dumps({'scope':'frozen isolated held-out suite, not new gameplay','cases':cases()},ensure_ascii=False,indent=2).encode('utf-8'))
    print(path.name, len(cases()))
