"""Source transport guards: tiny temporary files, no network/native/weight calls."""
from pathlib import Path
from types import SimpleNamespace
import contextlib
import copy
import hashlib
import io
import json
import sys
import tempfile
import unittest
from unittest import mock

import download_qwen15_candidate as guards
import probe_xet_transport as probe


class SourceGuardTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='echo-xet-source-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.meta = SimpleNamespace(commit_hash=probe.SOURCE_REVISION, size=probe.SOURCE_BYTES,
                                    etag=probe.SOURCE_SHA,
                                    xet_file_data=SimpleNamespace(file_hash='a' * 64, refresh_route='https://huggingface.co/api/models/fixture/xet-read-token/revision'))

    def test_metadata_pins_and_required_native_identity_reject_wrong_or_missing_fields(self):
        self.assertEqual(probe.validate_source_metadata(self.meta), 'a' * 64)
        for field, value in (('commit_hash', 'b' * 40), ('size', probe.SOURCE_BYTES - 1),
                             ('etag', 'b' * 64), ('xet_file_data', None)):
            bad = copy.deepcopy(self.meta)
            setattr(bad, field, value)
            with self.subTest(field=field), self.assertRaises(ValueError):
                probe.validate_source_metadata(bad)
        for field, value in (('file_hash', 'invalid'), ('refresh_route', 'http://huggingface.co/route'),
                             ('refresh_route', 'https://elsewhere.invalid/route')):
            bad = copy.deepcopy(self.meta)
            setattr(bad.xet_file_data, field, value)
            with self.subTest(field=field), self.assertRaises(ValueError):
                probe.validate_source_metadata(bad)

    def test_bad_sha_wrong_length_and_existing_final_never_promote_or_overwrite(self):
        part, final = self.root / 'source.part', self.root / 'source'
        data = b'fixture source bytes'
        part.write_bytes(data)
        for size, digest in ((len(data) + 1, hashlib.sha256(data).hexdigest()), (len(data), '0' * 64)):
            with self.subTest(size=size, sha=digest), self.assertRaises(ValueError):
                probe.promote_source(part, final, expected_bytes=size, expected_sha=digest)
            self.assertEqual(part.read_bytes(), data)
            self.assertFalse(final.exists())
        final.write_bytes(b'preserve')
        with self.assertRaises(ValueError):
            probe.promote_source(part, final, expected_bytes=len(data), expected_sha=hashlib.sha256(data).hexdigest())
        self.assertEqual(final.read_bytes(), b'preserve')

    def test_exact_full_sha_promotes_same_file_without_duplicate_bytes(self):
        part, final = self.root / 'source.part', self.root / 'source'
        data = b'full fixture data'
        part.write_bytes(data)
        digest = hashlib.sha256(data).hexdigest()
        self.assertEqual(probe.promote_source(part, final, expected_bytes=len(data), expected_sha=digest), digest)
        self.assertFalse(part.exists())
        self.assertEqual(final.read_bytes(), data)

    def test_source_paths_require_fresh_owned_destination_and_preserve_http_data(self):
        final, part = probe.source_paths(self.root)
        self.assertTrue(final.is_relative_to(self.root))
        self.assertEqual(part.name, 'model.onnx_data.xet.part')
        final.parent.mkdir(parents=True)
        old = final.with_name('model.onnx_data.part')
        for existing in (final, part, old):
            existing.write_bytes(b'preserve')
            with self.subTest(path=existing.name), self.assertRaises(ValueError):
                probe.source_paths(self.root)
            self.assertEqual(existing.read_bytes(), b'preserve')
            existing.unlink()
        with mock.patch.object(probe, 'SOURCE_FOLDER', Path('../escape')), self.assertRaises(ValueError):
            probe.source_paths(self.root)

    def test_budget_reserves_unwritten_source_and_outputs_without_counting_preallocation_twice(self):
        _, part = probe.source_paths(self.root)
        with mock.patch.object(guards, 'resource_bytes', return_value={'totalBytes': 100}):
            first = probe.source_budget(self.root, part)
            self.assertEqual(first['projectedBytes'], 100 + probe.SOURCE_BYTES + guards.DEFAULT_RESERVE)
            part.parent.mkdir(parents=True)
            part.write_bytes(bytes(20))
            second = probe.source_budget(self.root, part)
            self.assertEqual(second['remainingSourceReservedBytes'], probe.SOURCE_BYTES - 20)
        with mock.patch.object(guards, 'resource_bytes', return_value={'totalBytes': guards.LIMIT_BYTES - probe.SOURCE_BYTES - guards.DEFAULT_RESERVE + 1}):
            part.unlink()
            with self.assertRaises(ValueError):
                probe.source_budget(self.root, part)

    def test_source_cli_selects_only_native_source_and_process_environment_is_isolated(self):
        evidence = self.root / 'evidence'
        argv = ['probe', '--stage', 'source', '--label', 'Fixture_A', '--max-seconds', '1200', '--worker']
        with (mock.patch.object(probe, 'EV', evidence), mock.patch.object(sys, 'argv', argv),
             mock.patch.object(probe, 'worker_source', return_value={'passed': True}) as source,
             mock.patch.object(probe, 'worker_benchmark', side_effect=AssertionError('HTTP/tiny fallback')),
             mock.patch.object(probe, 'worker_package', side_effect=AssertionError('Package fallback')),
             contextlib.redirect_stdout(io.StringIO())):
            probe.main()
        source.assert_called_once_with('Fixture_A')
        before = dict(probe.os.environ)
        env, _ = probe.stage_environment()
        self.assertEqual(dict(probe.os.environ), before)
        self.assertEqual(env['HF_XET_HIGH_PERFORMANCE'], '1')
        self.assertEqual(env['HF_XET_RECONSTRUCTION_DOWNLOAD_BUFFER_PERFILE_SIZE'], '64mb')
        self.assertEqual(env['HF_HUB_DISABLE_IMPLICIT_TOKEN'], '1')
        self.assertNotIn('HF_TOKEN', env)
        self.assertTrue(Path(env['HF_XET_CACHE']).is_relative_to(probe.CACHE))
        with mock.patch.object(sys, 'argv', ['probe', '--stage', 'source', '--label', 'Bad', '--max-seconds', '1201']), self.assertRaises(ValueError):
            probe.main()


if __name__ == '__main__':
    unittest.main(verbosity=2)
