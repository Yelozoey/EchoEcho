import copy
import json
from pathlib import Path
import unittest
from transformers import AutoTokenizer
from qwen3_tuning_inputs import context, messages, prepare_case, utf16_length, validate_suite

ROOT = Path(__file__).resolve().parents[2]

class InputTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows = json.loads((ROOT/'Tools/Stage3/Fixtures/Q3_Validation100_B.json').read_text(encoding='utf-8'))['cases']
        cls.prompt = (ROOT/'Docs/Stage3/Prompts/Q3_V5_V2Contrast.txt').read_text(encoding='utf-8')
        old = json.loads((ROOT/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3/CandidateCases.json').read_text(encoding='utf-8'))
        cls.old = {row['input'] for row in old['cases']}
        cls.tokenizer = AutoTokenizer.from_pretrained(str(ROOT/'Library/EchoStation/S3Cache/models/qwen3-candidate/original'), local_files_only=True, trust_remote_code=False)

    def test_all_100_are_new_bounded_official_non_thinking_inputs(self):
        validate_suite(self.rows, self.old, self.prompt)
        for row in self.rows:
            prepared = prepare_case(self.prompt, row, self.tokenizer)
            self.assertEqual(prepared['ids'], self.tokenizer.encode(prepared['officialText'], add_special_tokens=False))
            self.assertTrue(prepared['officialText'].endswith('<think>\n\n</think>\n\n'))
            self.assertEqual(len(row['targets']), 5)

    def test_counterfactual_expected_labels_and_targets_cannot_change_input(self):
        row = self.rows[30]
        changed = dict(row, expected='DO_NOT_LEAK_THIS_LABEL', target='EXPECTED_ONLY')
        before = prepare_case(self.prompt, row, self.tokenizer)
        after = prepare_case(self.prompt, changed, self.tokenizer)
        for key in ('body','officialText','ids'): self.assertEqual(before[key], after[key])
        self.assertNotIn('DO_NOT_LEAK_THIS_LABEL', after['body'])
        self.assertNotIn('EXPECTED_ONLY', after['body'])
        for target in row['targets']: self.assertIn(target['id'], after['body'])

    def test_duplicate_old_or_prompt_example_sentences_rejected(self):
        rows = copy.deepcopy(self.rows); rows[0]['input'] = rows[1]['input']
        with self.assertRaises(AssertionError): validate_suite(rows, self.old, self.prompt)
        rows = copy.deepcopy(self.rows); rows[0]['input'] = next(iter(self.old))
        with self.assertRaises(AssertionError): validate_suite(rows, self.old, self.prompt)
        with self.assertRaises(AssertionError): validate_suite(self.rows, self.old, self.prompt+self.rows[0]['input'])

    def test_wrong_target_permission_and_label_counts_rejected(self):
        rows = copy.deepcopy(self.rows); rows[30]['target'] = 'fixture_cable_04'
        with self.assertRaises(AssertionError): validate_suite(rows, self.old, self.prompt)
        rows = copy.deepcopy(self.rows); rows[0]['expected'] = 'Wait'
        with self.assertRaises(AssertionError): validate_suite(rows, self.old, self.prompt)

    def test_duplicate_or_more_than_eight_targets_rejected(self):
        row = copy.deepcopy(self.rows[0]); row['targets'].append(row['targets'][0])
        with self.assertRaises(AssertionError): context(row)
        row['targets'] = [dict(id='target_'+str(i),name='测试',actions=['investigate']) for i in range(9)]
        with self.assertRaises(AssertionError): context(row)

    def test_utf16_and_invalid_surrogates_fail_closed(self):
        row = copy.deepcopy(self.rows[0]); row['input'] = '😀'*100
        messages(self.prompt, row)
        self.assertEqual(utf16_length(row['input']), 200)
        row['input'] += 'a'
        with self.assertRaises(AssertionError): messages(self.prompt, row)
        row['input'] = '\ud800'
        with self.assertRaises(UnicodeEncodeError): messages(self.prompt, row)

    def test_data_placeholders_not_recursively_substituted(self):
        row = copy.deepcopy(self.rows[0]); row['summary'] = '摘要含{player_input}'; row['input'] = '输入含{retrieved_memory}'
        body, _ = messages(self.prompt, row)
        self.assertIn('摘要含{player_input}', body)
        self.assertIn('输入含{retrieved_memory}', body)

    def test_unsupported_role_action_and_prompt_budget_rejected(self):
        with self.assertRaises(AssertionError): messages(self.prompt, self.rows[0], 'assistant')
        row = copy.deepcopy(self.rows[0]); row['targets'][0]['actions'] = ['open_door']
        with self.assertRaises(AssertionError): context(row)
        with self.assertRaises(AssertionError): prepare_case('说明'*2000+self.prompt, self.rows[0], self.tokenizer)

if __name__ == '__main__': unittest.main(verbosity=2)
