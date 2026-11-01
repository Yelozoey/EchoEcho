"""Pure metadata tests: tiny temporary files, no ORT/model/Unity/network calls."""
from pathlib import Path
import contextlib
import copy
import importlib.util
import io
import json
import sys
import tempfile
import unittest
from unittest import mock

import probe_qwen3_candidate as probe


class CandidateProbeGuardTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix='echo-candidate-guards-')
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.evidence = self.root / 'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3'
        self.candidate = self.root / 'Library/EchoStation/S3Cache/models/qwen3-candidate'
        self.evidence.mkdir(parents=True)
        self.source = self.candidate / 'onnx/onnx/model_q4.onnx'
        self.optimized = self.source.with_name('model_q4_last.onnx')
        self.source.parent.mkdir(parents=True)
        self.source.write_bytes(b'tiny source, not an ONNX model')
        self.optimized.write_bytes(b'tiny variant, not an ONNX model')
        self.write_json('CandidateCases.json', {'cases': [{'input': 'fixture', 'ids': list(range(64))}]})
        self.write_json('Downloads_ranges.json', {
            'verified': True, 'repo': 'onnx-community/Qwen3-0.6B-ONNX',
            'bytes': self.source.stat().st_size, 'sha256': probe.sha256_file(self.source),
        })
        self.write_json('LastHeadTransformation.json', {
            'sourceSha256': probe.sha256_file(self.source), 'sha256': probe.sha256_file(self.optimized),
            'bytes': self.optimized.stat().st_size, 'weightsUnchanged': True,
        })
        records = []
        for name in probe.TOKENIZER_FILES:
            path = self.candidate / 'original' / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(json.dumps({'fixture': name}).encode('utf-8'))
            records.append({'repo': 'Qwen/Qwen3-0.6B', 'revision': '1' * 40,
                            'path': path.relative_to(self.root).as_posix(),
                            'bytes': path.stat().st_size, 'sha256': probe.sha256_file(path)})
        self.write_json('Downloads_small.json', records)
        self.bindings = probe.capture_bindings(self.root)

    def write_json(self, filename, value):
        (self.evidence / filename).write_bytes(json.dumps(value).encode('utf-8'))

    def good_report(self):
        return {'mode': 'equivalence', 'completed': True, 'passed': True,
                'bindingVersion': 1, 'bindingsStable': True,
                'bindingsStart': copy.deepcopy(self.bindings), 'bindingsEnd': copy.deepcopy(self.bindings),
                'comparisons': [{'step': 0, 'logitsMaxAbs': 0.0, 'kvMaxAbs': 0.0},
                                {'step': 1, 'logitsMaxAbs': 0.0001, 'kvMaxAbs': 0.0}]}

    def test_import_has_no_native_dependency_or_argument_side_effect(self):
        spec = importlib.util.spec_from_file_location('candidate_pure_import_test', probe.__file__)
        module = importlib.util.module_from_spec(spec)
        original_import = __import__
        def blocked_import(name, *args, **kwargs):
            if name.split('.')[0] in ('numpy', 'onnxruntime', 'transformers'):
                raise AssertionError('Pure import attempted a native/model dependency')
            return original_import(name, *args, **kwargs)
        with mock.patch('builtins.__import__', side_effect=blocked_import), mock.patch.object(sys, 'argv', ['tool', '--invalid']):
            spec.loader.exec_module(module)
        self.assertEqual(module.BINDING_VERSION, 1)

    def test_complete_current_bindings_are_accepted(self):
        probe.validate_equivalence_report(self.good_report(), probe.capture_bindings(self.root))

    def test_legacy_equivalence_is_rejected(self):
        with self.assertRaises(ValueError):
            probe.validate_equivalence_report({'mode': 'equivalence', 'completed': True, 'passed': True}, self.bindings)

    def test_missing_binding_or_status_field_is_rejected(self):
        for field in ('mode', 'completed', 'passed', 'bindingVersion', 'bindingsStable', 'bindingsStart', 'bindingsEnd', 'comparisons'):
            with self.subTest(field=field):
                report = self.good_report()
                del report[field]
                with self.assertRaises(ValueError):
                    probe.validate_equivalence_report(report, self.bindings)

    def test_wrong_mode_status_or_version_is_rejected(self):
        for field, value in (('mode', 'raw'), ('completed', False), ('completed', 1), ('passed', False),
                             ('bindingsStable', False), ('bindingVersion', 2), ('bindingVersion', True)):
            with self.subTest(field=field, value=value):
                report = self.good_report()
                report[field] = value
                with self.assertRaises(ValueError):
                    probe.validate_equivalence_report(report, self.bindings)

    def test_every_hash_is_bound_to_actual_files(self):
        for field in probe.HASH_FIELDS:
            with self.subTest(field=field):
                report = self.good_report()
                report['bindingsStart'][field] = report['bindingsEnd'][field] = '0' * 64
                with self.assertRaises(ValueError):
                    probe.validate_equivalence_report(report, self.bindings)
        for filename in probe.TOKENIZER_FILES:
            with self.subTest(filename=filename):
                report = self.good_report()
                for boundary in ('bindingsStart', 'bindingsEnd'):
                    report[boundary]['officialTokenizerSha256'][filename] = '0' * 64
                with self.assertRaises(ValueError):
                    probe.validate_equivalence_report(report, self.bindings)

    def test_incomplete_and_start_end_different_bindings_are_rejected(self):
        for field in probe.HASH_FIELDS + ('officialTokenizerSha256',):
            with self.subTest(field=field):
                report = self.good_report()
                del report['bindingsStart'][field]
                with self.assertRaises(ValueError):
                    probe.validate_equivalence_report(report, self.bindings)
        report = self.good_report()
        report['bindingsEnd']['candidateCasesSha256'] = '0' * 64
        with self.assertRaises(ValueError):
            probe.validate_equivalence_report(report, self.bindings)

    def test_source_or_variant_tamper_is_rejected(self):
        for path in (self.source, self.optimized):
            with self.subTest(path=path.name):
                old = path.read_bytes()
                path.write_bytes(bytes([old[0] ^ 1]) + old[1:])
                with self.assertRaises(ValueError):
                    probe.capture_bindings(self.root)
                path.write_bytes(old)

    def test_official_tokenizer_tamper_is_rejected(self):
        for filename in probe.TOKENIZER_FILES:
            with self.subTest(filename=filename):
                path = self.candidate / 'original' / filename
                old = path.read_bytes()
                path.write_bytes(old + b' ')
                with self.assertRaises(ValueError):
                    probe.capture_bindings(self.root)
                path.write_bytes(old)

    def test_case_or_metadata_change_invalidates_previous_equivalence(self):
        for filename in ('CandidateCases.json', 'LastHeadTransformation.json', 'Downloads_ranges.json', 'Downloads_small.json'):
            with self.subTest(filename=filename):
                path = self.evidence / filename
                old = path.read_bytes()
                path.write_bytes(old + b'\n')
                actual = probe.capture_bindings(self.root)
                with self.assertRaises(ValueError):
                    probe.validate_equivalence_report(self.good_report(), actual)
                path.write_bytes(old)

    def test_missing_or_failing_numerical_steps_are_rejected(self):
        bad_values = ([], [{'step': 0, 'logitsMaxAbs': 0, 'kvMaxAbs': 0}],
                      [{'step': 0, 'logitsMaxAbs': 0.00010001, 'kvMaxAbs': 0}, {'step': 1, 'logitsMaxAbs': 0, 'kvMaxAbs': 0}],
                      [{'step': 0, 'logitsMaxAbs': 0, 'kvMaxAbs': 0}, {'step': 1, 'logitsMaxAbs': 0, 'kvMaxAbs': 1e-12}],
                      [{'step': 0, 'logitsMaxAbs': float('nan'), 'kvMaxAbs': 0}, {'step': 1, 'logitsMaxAbs': 0, 'kvMaxAbs': 0}])
        for comparisons in bad_values:
            with self.subTest(comparisons=comparisons):
                report = self.good_report()
                report['comparisons'] = comparisons
                with self.assertRaises(ValueError):
                    probe.validate_equivalence_report(report, self.bindings)

    def test_errors_transfer_strings_without_traceback_references(self):
        try:
            try:
                raise ValueError('inner')
            except ValueError as cause:
                raise RuntimeError('outer') from cause
        except RuntimeError as error:
            data = probe.error_data(error)
            self.assertIsNone(error.__traceback__)
            self.assertIsNone(error.__cause__)
            self.assertIsNone(error.__context__)
            self.assertEqual(data, {'error': 'outer', 'errorType': 'RuntimeError'})
            json.dumps(data)

    def test_cli_requires_explicit_equivalence_and_preserves_existing_evidence(self):
        self.write_json('Existing_A.json', {'historical': True})
        for arguments in (['--mode', 'raw', '--tag', 'Fresh'],
                          ['--mode', 'equivalence', '--tag', 'Existing_A'],
                          ['--mode', 'raw', '--tag', '../escape', '--equivalence-tag', 'Equivalence_B']):
            with self.subTest(arguments=arguments), mock.patch.object(probe, 'ROOT', self.root), \
                    mock.patch.object(probe, 'owner_run') as owner, contextlib.redirect_stderr(io.StringIO()):
                with self.assertRaises(SystemExit) as error:
                    probe.main(arguments)
                self.assertEqual(error.exception.code, 2)
                owner.assert_not_called()
        self.assertEqual(probe.read_json(self.evidence / 'Existing_A.json'), {'historical': True})


if __name__ == '__main__':
    unittest.main(verbosity=2)
