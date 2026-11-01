"""Tiny local fixture tests. No Internet, models, Unity, or large allocation."""
from pathlib import Path
import copy
import hashlib
from http.client import IncompleteRead
import importlib.util
import io
import json
import re
import sys
import tempfile
import threading
import unittest
from unittest import mock

import download_qwen15_candidate as download


class Response(io.BytesIO):
    def __init__(self, body, status=206, headers=None):
        super().__init__(body)
        self.status = status
        self.headers = headers or {}


class RangeSource:
    def __init__(self, data, fault=None):
        self.data = data
        self.fault = fault
        self.calls = []
        self.lock = threading.Lock()

    def __call__(self, request, timeout):
        match = re.fullmatch(r'bytes=(\d+)-(\d+)', request.get_header('Range'))
        if not match:
            raise AssertionError('Range was not bounded')
        start, end = map(int, match.groups())
        with self.lock:
            self.calls.append((start, end))
        headers = {'Content-Range': f'bytes {start}-{end}/{len(self.data)}',
                   'Content-Length': str(end - start + 1)}
        body, status = self.data[start:end + 1], 206
        if self.fault:
            body, status, headers = self.fault(start, end, body, status, headers)
        return Response(body, status, headers)


class DownloadGuardTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix='echo-qwen15-download-')
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.cache = self.root / 'cache'
        self.evidence = self.root / 'evidence'
        self.evidence.mkdir()
        self.data = bytes(range(251)) * 7 + b'last short chunk'
        self.repo, self.revision, self.name = 'fixture/repo', 'a' * 40, 'onnx/model.onnx_data'
        self.digest = hashlib.sha256(self.data).hexdigest()
        self.metadata = {'id': self.repo, 'sha': self.revision, 'private': False, 'gated': False,
                         'siblings': [{'rfilename': self.name, 'size': len(self.data),
                                       'lfs': {'size': len(self.data), 'sha256': self.digest}}]}
        self.count_patch = mock.patch.object(download, 'resource_bytes', return_value={'totalBytes': 0, 'parts': [], 'limitBytes': download.LIMIT_BYTES})
        self.count_patch.start()
        self.addCleanup(self.count_patch.stop)

    def run_download(self, source, workers=4, attempts=3, **kwargs):
        return download.download_ranges(self.root, self.cache, self.evidence, self.metadata, 0,
                                        opener=source, chunk_bytes=256, workers=workers, attempts=attempts,
                                        file_spec=(self.name, len(self.data), self.digest),
                                        repo=self.repo, revision=self.revision, **kwargs)

    def paths(self):
        final = self.cache / 'onnx' / self.name
        return final, final.with_name(final.name + '.part'), final.with_name(final.name + '.ranges.json')

    def test_import_has_no_network_argument_or_model_side_effects(self):
        spec = importlib.util.spec_from_file_location('isolated_download_import', download.__file__)
        module = importlib.util.module_from_spec(spec)
        with mock.patch('urllib.request.urlopen', side_effect=AssertionError('Import accessed network')), mock.patch.object(sys, 'argv', ['tool', '--weights']):
            spec.loader.exec_module(module)
        self.assertEqual(module.WORKERS, 4)

    def test_real_parallel_positional_writes_preserve_all_data_and_no_chunk_copies(self):
        source = RangeSource(self.data)
        with mock.patch('builtins.print'):
            report = self.run_download(source)
        final, part, manifest = self.paths()
        self.assertEqual(final.read_bytes(), self.data)
        self.assertFalse(part.exists())
        self.assertTrue(report['verified'])
        self.assertTrue(report['singlePreallocatedFile'])
        self.assertEqual(report['sha256'], self.digest)
        self.assertEqual(len(source.calls), (len(self.data) + 255) // 256)
        self.assertFalse(list(self.cache.rglob('*.ranges/*.part')))
        state = json.loads(manifest.read_bytes())
        self.assertEqual(len(state['completed']), report['ranges'])

    def test_valid_final_is_reverified_without_network(self):
        final, _, _ = self.paths()
        final.parent.mkdir(parents=True)
        final.write_bytes(self.data)
        report = self.run_download(mock.Mock(side_effect=AssertionError('Unexpected network')))
        self.assertTrue(report['reusedFinal'])

    def test_corrupt_final_cannot_be_reused(self):
        final, _, _ = self.paths()
        final.parent.mkdir(parents=True)
        final.write_bytes(b'X' + self.data[1:])
        with self.assertRaises(download.DownloadError):
            self.run_download(mock.Mock(side_effect=AssertionError('Unexpected network')))

    def test_http_status_range_length_and_encoding_are_all_required(self):
        good = {'Content-Range': 'bytes 4-9/100', 'Content-Length': '6'}
        for status, headers in ((200, good), (206, {**good, 'Content-Range': 'bytes 4-10/100'}),
                                (206, {**good, 'Content-Range': 'bytes 4-9/101'}),
                                (206, {**good, 'Content-Length': '7'}),
                                (206, {**good, 'Content-Encoding': 'gzip'})):
            with self.subTest(status=status, headers=headers), self.assertRaises(download.DownloadError):
                download.validate_range(status, headers, 4, 9, 100)
        download.validate_range(206, good, 4, 9, 100)

    def test_truncated_response_is_not_marked_complete_and_retries_are_bounded(self):
        def truncate(start, end, body, status, headers):
            return body[:-1], status, headers
        source = RangeSource(self.data, truncate)
        with mock.patch('builtins.print'), self.assertRaises(download.DownloadError):
            self.run_download(source)
        final, part, manifest = self.paths()
        self.assertFalse(final.exists())
        self.assertEqual(part.stat().st_size, len(self.data))
        self.assertEqual(json.loads(manifest.read_bytes())['completed'], {})
        counts = {chunk: source.calls.count(chunk) for chunk in source.calls}
        self.assertTrue(all(value <= 3 for value in counts.values()))
        self.assertTrue(list(self.evidence.glob('RangeError_*.json')))

    def test_whole_lfs_sha_is_required_after_locally_consistent_chunks(self):
        source = RangeSource(b'X' + self.data[1:])
        with mock.patch('builtins.print'), self.assertRaisesRegex(download.DownloadError, 'LFS SHA'):
            self.run_download(source)
        final, part, manifest = self.paths()
        self.assertFalse(final.exists())
        self.assertTrue(part.exists())
        self.assertGreater(len(json.loads(manifest.read_bytes())['completed']), 0)

    def create_partial(self, corrupt=False):
        final, part, manifest = self.paths()
        final.parent.mkdir(parents=True)
        part.write_bytes(self.data[:256] + bytes(len(self.data) - 256))
        binding = {'repo': self.repo, 'revision': self.revision, 'name': self.name,
                   'bytes': len(self.data), 'sha256': self.digest}
        state = {'version': 1, 'binding': binding, 'chunkBytes': 256,
                 'completed': {'0': hashlib.sha256(self.data[:256]).hexdigest()}}
        manifest.write_bytes(json.dumps(state).encode())
        if corrupt:
            with part.open('r+b') as stream:
                stream.write(b'X')
        return state

    def test_resume_rehashes_and_skips_only_completed_persisted_chunks(self):
        self.create_partial()
        source = RangeSource(self.data)
        with mock.patch('builtins.print'):
            self.run_download(source)
        self.assertNotIn((0, 255), source.calls)
        self.assertEqual(self.paths()[0].read_bytes(), self.data)

    def test_corrupted_completed_chunk_is_downloaded_again(self):
        self.create_partial(corrupt=True)
        source = RangeSource(self.data)
        with mock.patch('builtins.print'):
            self.run_download(source)
        self.assertIn((0, 255), source.calls)
        self.assertEqual(self.paths()[0].read_bytes(), self.data)

    def test_retry_requests_only_remaining_prefix_offset(self):
        first = True
        def truncate_first(start, end, body, status, headers):
            nonlocal first
            if first:
                first = False
                return body[:37], status, headers
            return body, status, headers
        source = RangeSource(self.data, truncate_first)
        with mock.patch('builtins.print'):
            self.run_download(source, workers=1)
        self.assertEqual(source.calls[:2], [(0, 255), (37, 255)])
        self.assertEqual(self.paths()[0].read_bytes(), self.data)
        self.assertEqual(json.loads(self.paths()[2].read_bytes())['partial'], {})

    def test_process_resume_uses_only_rehashed_persisted_partial_prefix(self):
        source = RangeSource(self.data, lambda start, end, body, status, headers: (body[:23], status, headers))
        with mock.patch('builtins.print'), self.assertRaises(download.DownloadError):
            self.run_download(source, workers=1)
        self.assertEqual(source.calls, [(0, 255), (23, 255), (46, 255)])
        state = json.loads(self.paths()[2].read_bytes())
        self.assertEqual(state['partial']['0']['bytes'], 69)
        self.assertEqual(state['partial']['0']['sha256'], hashlib.sha256(self.data[:69]).hexdigest())
        fresh_process_source = RangeSource(self.data)
        with mock.patch('builtins.print'):
            self.run_download(fresh_process_source, workers=1)
        self.assertEqual(fresh_process_source.calls[0], (69, 255))
        self.assertEqual(self.paths()[0].read_bytes(), self.data)

    def create_partial_prefix(self, have=71):
        self.create_partial()
        _, part, manifest = self.paths()
        state = json.loads(manifest.read_bytes())
        state['completed'] = {}
        state['partial'] = {'0': {'bytes': have, 'sha256': hashlib.sha256(self.data[:have]).hexdigest()}}
        manifest.write_bytes(json.dumps(state).encode())
        return state

    def test_corrupt_partial_prefix_restarts_from_chunk_base(self):
        self.create_partial_prefix()
        with self.paths()[1].open('r+b') as target:
            target.write(b'X')
        source = RangeSource(self.data)
        with mock.patch('builtins.print'):
            self.run_download(source, workers=1)
        self.assertEqual(source.calls[0], (0, 255))
        self.assertEqual(self.paths()[0].read_bytes(), self.data)

    def test_partial_binding_fields_overlap_and_bounds_are_rejected(self):
        original = self.create_partial_prefix()
        for mutate in (
            lambda value: value['binding'].update(revision='b' * 40),
            lambda value: value.update(chunkBytes=128),
            lambda value: value['partial']['0'].update(bytes=0),
            lambda value: value['partial']['0'].update(bytes=256),
            lambda value: value['partial']['0'].update(bytes=True),
            lambda value: value['partial']['0'].update(sha256='not a hash'),
            lambda value: value['partial']['0'].update(extra='not allowed'),
            lambda value: value['completed'].update({'0': hashlib.sha256(self.data[:256]).hexdigest()}),
        ):
            state = copy.deepcopy(original)
            mutate(state)
            self.paths()[2].write_bytes(json.dumps(state).encode())
            with self.subTest(state=state), self.assertRaises(download.DownloadError):
                self.run_download(mock.Mock(side_effect=AssertionError('Unexpected network')))

    def test_incomplete_read_partial_is_kept_within_validated_response_bounds(self):
        source = RangeSource(self.data)
        first = True
        class InterruptedResponse(Response):
            def read(self, requested=-1):
                raise IncompleteRead(self.getvalue()[:31], 225)
        def opener(request, timeout):
            nonlocal first
            response = source(request, timeout)
            if first:
                first = False
                return InterruptedResponse(response.getvalue(), response.status, response.headers)
            return response
        with mock.patch('builtins.print'):
            self.run_download(opener, workers=1)
        self.assertEqual(source.calls[:2], [(0, 255), (31, 255)])
        self.assertEqual(self.paths()[0].read_bytes(), self.data)

    def test_incomplete_read_oversize_partial_is_not_written_or_trusted(self):
        source = RangeSource(self.data)
        class OversizeResponse(Response):
            def read(self, requested=-1):
                raise IncompleteRead(b'X' * (requested + 1), 0)
        def opener(request, timeout):
            response = source(request, timeout)
            return OversizeResponse(response.getvalue(), response.status, response.headers)
        with mock.patch('builtins.print'), self.assertRaises(download.DownloadError):
            self.run_download(opener, workers=1)
        state = json.loads(self.paths()[2].read_bytes())
        self.assertEqual(state['completed'], {})
        self.assertEqual(state['partial'], {})
        with self.paths()[1].open('rb') as target:
            self.assertEqual(target.read(256), bytes(256))

    def test_changed_pin_or_orphan_partial_cannot_resume(self):
        state = self.create_partial()
        _, part, manifest = self.paths()
        state['binding']['revision'] = 'b' * 40
        manifest.write_bytes(json.dumps(state).encode())
        with self.assertRaises(download.DownloadError):
            self.run_download(mock.Mock(side_effect=AssertionError('Unexpected network')))
        manifest.unlink()
        with self.assertRaisesRegex(download.DownloadError, 'Orphan'):
            self.run_download(mock.Mock(side_effect=AssertionError('Unexpected network')))
        self.assertTrue(part.exists())

    def test_budget_reserves_outputs_and_counts_preallocated_file_once(self):
        self.assertEqual(download.check_budget(10, 20, 30, 60), 60)
        with self.assertRaises(download.DownloadError):
            download.check_budget(10, 20, 31, 60)
        for args in ((-1, 0, 0), (0, True, 0), (0, 0, 1.0)):
            with self.assertRaises(download.DownloadError):
                download.check_budget(*args)
        self.create_partial()
        seen = []
        original = download.check_budget
        def inspect(current, required, reserve, limit=download.LIMIT_BYTES):
            seen.append(required)
            return original(current, required, reserve, limit)
        with mock.patch.object(download, 'check_budget', side_effect=inspect), mock.patch('builtins.print'):
            self.run_download(RangeSource(self.data))
        self.assertEqual(seen, [0, 0])

    def test_metadata_must_match_repo_revision_and_complete_lfs_identity(self):
        for field, value in (('id', 'other/repo'), ('sha', 'b' * 40), ('private', True), ('gated', 'auto')):
            metadata = copy.deepcopy(self.metadata)
            metadata[field] = value
            with self.subTest(field=field), self.assertRaises(download.DownloadError):
                download.pinned_item(metadata, self.repo, self.revision, self.name)
        metadata = copy.deepcopy(self.metadata)
        metadata['siblings'][0]['lfs']['size'] += 1
        with self.assertRaises(download.DownloadError):
            download.pinned_item(metadata, self.repo, self.revision, self.name)

    def test_small_git_blob_is_verified_and_large_file_cannot_use_small_path(self):
        body = b'official small text fixture'
        blob = hashlib.sha1(f'blob {len(body)}\0'.encode() + body).hexdigest()
        metadata = {'id': self.repo, 'sha': self.revision, 'private': False, 'gated': False,
                    'siblings': [{'rfilename': 'LICENSE', 'size': len(body), 'blobId': blob}]}
        report = download.fetch_small(self.root, self.cache, self.evidence, 'original', self.repo,
                                      self.revision, 'LICENSE', metadata, 0,
                                      opener=lambda *args, **kwargs: Response(body, 200))
        self.assertTrue(report['verified'])
        metadata['siblings'][0]['size'] = 17 * 1024**2
        with self.assertRaises(download.DownloadError):
            download.fetch_small(self.root, self.cache, self.evidence, 'original', self.repo,
                                 self.revision, 'LICENSE', metadata, 0,
                                 opener=mock.Mock(side_effect=AssertionError('Unexpected network')))

    def test_fresh_report_and_path_traversal_guards(self):
        path = self.evidence / 'fresh.json'
        download.write_json(path, {'preserve': True}, fresh=True)
        with self.assertRaises(FileExistsError):
            download.write_json(path, {'preserve': False}, fresh=True)
        self.assertTrue(json.loads(path.read_bytes())['preserve'])
        for relative in ('../escape', '/absolute', 'C:/outside', 'x/../../outside'):
            with self.subTest(path=relative), self.assertRaises(download.DownloadError):
                download.safe_child(self.root, relative)


if __name__ == '__main__':
    unittest.main(verbosity=2)
