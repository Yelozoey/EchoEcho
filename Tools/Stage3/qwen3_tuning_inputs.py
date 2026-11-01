"""Pure bounded input preparation. Expected labels are never rendered to the model."""
import collections
import json
import re

COUNTS = dict(Follow=15, Wait=15, Investigate=20, Warn=10, Answer=25, Refuse=15)

def utf16_length(value):
    return len(value.encode('utf-16-le', errors='strict')) // 2

def context(row):
    summary = row['summary']
    assert isinstance(summary, str) and 0 < utf16_length(summary) <= 120
    assert len(summary.encode('utf-8')) <= 1200
    targets = row['targets']
    assert isinstance(targets, list) and len(targets) <= 8
    seen = set()
    rendered = []
    for target in targets:
        identity, name, actions = target['id'], target['name'], target['actions']
        assert isinstance(identity, str) and re.fullmatch(r'[a-z][a-z0-9_]{0,63}', identity)
        assert identity not in seen
        seen.add(identity)
        assert isinstance(name, str) and 0 < utf16_length(name) <= 80
        assert isinstance(actions, list) and actions and len(set(actions)) == len(actions)
        assert set(actions) <= {'investigate', 'warn'}
        rendered.append(dict(id=identity, name=name, actions=list(actions)))
    return '相关记忆（摘要）：\n[线索 fixture_summary] '+summary+'\n允许目标（ID/名称/允许行为）：\n'+json.dumps(rendered, ensure_ascii=False, separators=(',', ':'))

def messages(prompt, row, role='user'):
    assert role in ('user', 'system')
    assert '\r' not in prompt and prompt.count('{retrieved_memory}') == prompt.count('{player_input}') == 1
    user = row['input']
    assert isinstance(user, str) and 0 < utf16_length(user) <= 200
    # Single-pass construction: data containing placeholder text is never substituted again.
    start, rest = prompt.split('{retrieved_memory}')
    between, end = rest.split('{player_input}')
    body = start+context(row)+between+user+end
    result = [dict(role=role, content=body)]
    if role == 'system': result.append(dict(role='user', content=user))
    return body, result

def validate_suite(rows, old_inputs, prompt):
    assert len(rows) == 100
    assert collections.Counter(row['expected'] for row in rows) == collections.Counter(COUNTS)
    inputs = [row['input'] for row in rows]
    assert len(set(inputs)) == 100 and not set(inputs).intersection(old_inputs)
    assert all(sentence not in prompt for sentence in inputs)
    for row in rows:
        assert re.fullmatch(r'[a-z][a-z0-9_]{0,63}', row['zone'])
        messages(prompt, row)
        if row['expected'] in ('Investigate', 'Warn'):
            action = 'investigate' if row['expected'] == 'Investigate' else 'warn'
            assert sum(target['id'] == row['target'] and action in target['actions'] for target in row['targets']) == 1
        else: assert row.get('target') is None

def prepare_case(prompt, row, tokenizer, role='user'):
    body, chat = messages(prompt, row, role)
    official = tokenizer.apply_chat_template(chat, tokenize=False, add_generation_prompt=True, enable_thinking=False)
    ids = tokenizer.apply_chat_template(chat, tokenize=True, add_generation_prompt=True, enable_thinking=False)
    assert 0 < len(ids) <= 1536 and len(ids)+256 <= 2048
    # Keep labels beside the input for reporting; never include them in chat or grammar.
    return dict(row, body=body, officialText=official, ids=ids)
