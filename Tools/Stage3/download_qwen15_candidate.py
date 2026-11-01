"""Pinned Qwen 1.5B candidate acquisition. Default: metadata/small files only.

No model execution, remote code, global installation, or network configuration.
The optional 6.2 GB external weight uses one preallocated file, not chunk copies.
"""
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path, PurePosixPath
import argparse
import contextlib
import hashlib
from http.client import IncompleteRead
import json
import math
import os
import re
import stat
import threading
import time
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = Path('Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen15')
CACHE = Path('Library/EchoStation/S3Cache/models/qwen15-candidate')
ORIGINAL_REPO = 'Qwen/Qwen2.5-1.5B-Instruct'
ORIGINAL_REVISION = '989aa7980e4cf806f80c7fef2b1adb7bc71aa306'
ONNX_REPO = 'onnx-community/Qwen2.5-1.5B-Instruct'
ONNX_REVISION = 'd54957e3af1be04546436e874a60fe386041b244'
GRAPH = ('onnx/model.onnx', 1057843, '8f7da5d0bb5e5b6eba0ba0e9c006fcbc8b670134405ef5e02aaaf738361e2074')
WEIGHTS = ('onnx/model.onnx_data', 6208411648, '7111acf0e546fe192035357890402832315b4c243e56b01b47ce9358285fc82e')
LIMIT_BYTES = 20 * 1024**3
DEFAULT_RESERVE = 2 * math.ceil(1.05 * 1024**3) + 64 * 1024**2
CHUNK_BYTES = 64 * 1024**2
WORKERS = 4
ATTEMPTS = 3


class DownloadError(ValueError):
    pass


def require(condition, message):
    if not condition:
        raise DownloadError(message)


def sha256_file(path, start=0, length=None):
    result = hashlib.sha256()
    with path.open('rb') as source:
        source.seek(start)
        remaining = length
        while remaining is None or remaining > 0:
            block = source.read(1024**2 if remaining is None else min(1024**2, remaining))
            if not block:
                require(remaining is None or remaining == 0, 'Unexpected end while hashing')
                break
            result.update(block)
            if remaining is not None:
                remaining -= len(block)
    return result.hexdigest()


def safe_child(root, relative):
    require(isinstance(relative, (str, Path)), 'Invalid relative path')
    text = str(relative).replace('\\', '/')
    parts = PurePosixPath(text)
    require(not parts.is_absolute() and ':' not in text and all(p not in ('..', '.') for p in parts.parts), 'Unsafe relative path')
    root = root.resolve()
    destination = root.joinpath(*parts.parts)
    require(destination.resolve().is_relative_to(root), 'Path escapes root')
    current = root
    for part in parts.parts:
        current /= part
        if current.exists() or current.is_symlink():
            info = current.lstat()
            require(not current.is_symlink() and not (getattr(info, 'st_file_attributes', 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT), 'Reparse/symlink path refused')
    return destination


def write_json(path, value, *, fresh=False):
    path.parent.mkdir(parents=True, exist_ok=True)
    data = json.dumps(value, ensure_ascii=False, indent=2).encode('utf-8')
    if fresh:
        with path.open('xb') as target:
            target.write(data)
    else:
        temporary = path.with_name(path.name + '.atomic-' + str(time.time_ns()))
        with temporary.open('xb') as target:
            target.write(data)
            target.flush()
            os.fsync(target.fileno())
        os.replace(temporary, path)


def resource_bytes(root):
    """Match resource_budget.py: current stats for every S3 root plus test sources."""
    baseline = json.loads((root / 'Docs/Tests/Stage3/Evidence/S3-I03/ResourceBudget.json').read_text(encoding='utf-8'))
    paths = [p['path'] for p in baseline['parts']] + ['Assets/EchoStation/Tests/AIEditor']
    parts = []
    seen = set()
    for relative in paths:
        path = safe_child(root, relative)
        key = str(path).casefold()
        require(key not in seen, 'Duplicate budget root')
        seen.add(key)
        total = 0
        if path.exists():
            for item in path.rglob('*'):
                if item.is_file():
                    safe_child(root, item.relative_to(root))
                    total += item.stat().st_size
        parts.append({'path': str(relative), 'bytes': total})
    return {'parts': parts, 'totalBytes': sum(x['bytes'] for x in parts), 'limitBytes': LIMIT_BYTES}


def check_budget(current, required_bytes, reserve_bytes, limit=LIMIT_BYTES):
    require(all(type(x) is int and x >= 0 for x in (current, required_bytes, reserve_bytes, limit)), 'Invalid budget values')
    projected = current + required_bytes + reserve_bytes
    require(projected <= limit, f'Authorized S3 resource budget exceeded: projected {projected}, limit {limit}')
    return projected


def pinned_item(metadata, repo, revision, name):
    require(metadata.get('sha') == revision and metadata.get('id') == repo, 'Repository/revision mismatch')
    require(metadata.get('private') is False and not metadata.get('gated'), 'Private/gated repository refused')
    matches = [x for x in metadata.get('siblings', []) if x.get('rfilename') == name]
    require(len(matches) == 1, 'Missing or duplicate file metadata')
    item = matches[0]
    size = item.get('size')
    lfs = item.get('lfs')
    require(type(size) is int and size > 0, 'Invalid metadata file size')
    if lfs:
        require(lfs.get('size') == size and re.fullmatch('[0-9a-f]{64}', lfs.get('sha256', '')), 'Invalid LFS size/SHA')
    else:
        require(re.fullmatch('[0-9a-f]{40}', item.get('blobId', '')), 'Missing Git blob SHA')
    return item


def verify_file(path, item):
    require(path.is_file() and path.stat().st_size == item['size'], 'File size differs from pinned metadata')
    actual = sha256_file(path)
    if item.get('lfs'):
        require(actual == item['lfs']['sha256'], 'Pinned LFS SHA mismatch')
    else:
        blob = hashlib.sha1()
        blob.update(f'blob {item["size"]}\0'.encode('ascii'))
        with path.open('rb') as source:
            while block := source.read(1024**2):
                blob.update(block)
        require(blob.hexdigest() == item['blobId'], 'Pinned Git blob SHA mismatch')
    return actual


def validate_range(status, headers, start, end, size):
    require(status == 206, 'Range request did not return 206')
    require(headers.get('Content-Range') == f'bytes {start}-{end}/{size}', 'Exact Content-Range not honored')
    require(headers.get('Content-Length') == str(end - start + 1), 'Range Content-Length mismatch')
    require(headers.get('Content-Encoding', 'identity') == 'identity', 'Encoded range refused')


@contextlib.contextmanager
def exclusive_lock(path):
    """OS lock is released on crash. The one-byte marker is retained for reuse."""
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('a+b') as handle:
        handle.seek(0, os.SEEK_END)
        if handle.tell() == 0:
            handle.write(b'0')
            handle.flush()
        handle.seek(0)
        if os.name == 'nt':
            import msvcrt
            msvcrt.locking(handle.fileno(), msvcrt.LK_NBLCK, 1)
        else:
            import fcntl
            fcntl.flock(handle.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
        try:
            yield
        finally:
            handle.seek(0)
            if os.name == 'nt':
                msvcrt.locking(handle.fileno(), msvcrt.LK_UNLCK, 1)
            else:
                fcntl.flock(handle.fileno(), fcntl.LOCK_UN)


def acquire_metadata(root, evidence, sub, repo, revision, opener=urllib.request.urlopen):
    path = safe_child(evidence, sub + '_metadata.json')
    if not path.exists():
        url = f'https://huggingface.co/api/models/{repo}/revision/{revision}?blobs=true'
        with opener(url, timeout=45) as response:
            require(response.status == 200, 'Metadata HTTP status differs')
            raw = response.read(8 * 1024**2 + 1)
            require(len(raw) <= 8 * 1024**2, 'Metadata response too large')
            metadata = json.loads(raw)
        require(metadata.get('sha') == revision and metadata.get('id') == repo, 'Pinned metadata response mismatch')
        require(metadata.get('private') is False and not metadata.get('gated'), 'Private/gated repository refused')
        write_json(path, metadata, fresh=True)
    metadata = json.loads(path.read_text(encoding='utf-8'))
    require(metadata.get('sha') == revision and metadata.get('id') == repo, 'Archived metadata does not match pin')
    return metadata


def fetch_small(root, cache, evidence, sub, repo, revision, name, metadata, reserve, opener=urllib.request.urlopen):
    item = pinned_item(metadata, repo, revision, name)
    require(item['size'] <= 16 * 1024**2, 'Small-file path refuses weights')
    path = safe_child(cache, Path(sub) / name)
    url = f'https://huggingface.co/{repo}/resolve/{revision}/{name}'
    if not path.exists():
        temporary = safe_child(cache, Path(sub) / (name + '.part'))
        require(not temporary.exists(), 'Unverified small-file partial exists; inspect before retry')
        check_budget(resource_bytes(root)['totalBytes'], item['size'], reserve)
        path.parent.mkdir(parents=True, exist_ok=True)
        with opener(url, timeout=45) as response, temporary.open('xb') as target:
            require(response.status == 200, 'Small-file HTTP status differs')
            written = 0
            while block := response.read(1024**2):
                written += len(block)
                require(written <= item['size'], 'Small response exceeds metadata')
                target.write(block)
            target.flush()
            os.fsync(target.fileno())
        verify_file(temporary, item)
        os.replace(temporary, path)
    digest = verify_file(path, item)
    return {'repo': repo, 'revision': revision, 'url': url, 'path': path.relative_to(root).as_posix(), 'bytes': item['size'], 'sha256': digest, 'verified': True}


def validated_resume(part, manifest, binding, chunk_bytes):
    """Only a persisted, locally rehashed completed chunk is eligible for skipping."""
    require(manifest.get('version') == 1 and manifest.get('binding') == binding and manifest.get('chunkBytes') == chunk_bytes, 'Resume manifest does not match pinned file')
    require(part.is_file() and part.stat().st_size == binding['bytes'], 'Resume preallocated file size differs')
    completed = manifest.get('completed')
    require(isinstance(completed, dict), 'Malformed completed chunks')
    count = (binding['bytes'] + chunk_bytes - 1) // chunk_bytes
    verified = {}
    for key, digest in completed.items():
        require(re.fullmatch('0|[1-9][0-9]*', key) is not None and int(key) < count, 'Invalid completed chunk index')
        require(isinstance(digest, str) and re.fullmatch('[0-9a-f]{64}', digest), 'Invalid completed chunk hash')
        start = int(key) * chunk_bytes
        length = min(chunk_bytes, binding['bytes'] - start)
        if sha256_file(part, start, length) == digest:
            verified[key] = digest
    return verified


def validated_partial_resume(part, manifest, binding, chunk_bytes):
    """A prefix is reusable only after binding, bounds, and disk SHA validation."""
    require(manifest.get('version') == 1 and manifest.get('binding') == binding and manifest.get('chunkBytes') == chunk_bytes, 'Partial manifest does not match pinned file')
    partial = manifest.get('partial', {})  # Original version-1 complete-only manifests remain valid.
    require(isinstance(partial, dict), 'Malformed partial chunks')
    completed = manifest.get('completed', {})
    count = (binding['bytes'] + chunk_bytes - 1) // chunk_bytes
    verified = {}
    for key, prefix in partial.items():
        require(isinstance(key, str) and re.fullmatch('0|[1-9][0-9]*', key) is not None and int(key) < count, 'Invalid partial chunk index')
        require(key not in completed, 'Chunk cannot be both complete and partial')
        require(isinstance(prefix, dict) and set(prefix) == {'bytes', 'sha256'}, 'Malformed partial chunk record')
        start = int(key) * chunk_bytes
        length = min(chunk_bytes, binding['bytes'] - start)
        have, digest = prefix['bytes'], prefix['sha256']
        require(type(have) is int and 0 < have < length, 'Partial prefix length is outside chunk bounds')
        require(isinstance(digest, str) and re.fullmatch('[0-9a-f]{64}', digest), 'Invalid partial prefix SHA')
        if sha256_file(part, start, have) == digest:
            verified[key] = {'bytes': have, 'sha256': digest}
    return verified


def hash_prefix(path, start, length):
    result = hashlib.sha256()
    with path.open('rb') as source:
        source.seek(start)
        remaining = length
        while remaining:
            block = source.read(min(1024**2, remaining))
            require(block, 'Unexpected end while reading verified prefix')
            result.update(block)
            remaining -= len(block)
    return result


def download_ranges(root, cache, evidence, metadata, reserve, *, opener=urllib.request.urlopen,
                    chunk_bytes=CHUNK_BYTES, workers=WORKERS, attempts=ATTEMPTS,
                    file_spec=WEIGHTS, repo=ONNX_REPO, revision=ONNX_REVISION):
    name, size, expected_sha = file_spec
    require(type(chunk_bytes) is int and chunk_bytes > 0 and 1 <= workers <= 4 and 1 <= attempts <= 3, 'Invalid bounded range configuration')
    item = pinned_item(metadata, repo, revision, name)
    require(item['size'] == size and item.get('lfs', {}).get('sha256') == expected_sha, 'Weight LFS metadata differs from reviewed pin')
    path = safe_child(cache, Path('onnx') / name)
    part = safe_child(cache, Path('onnx') / (name + '.part'))
    manifest_path = safe_child(cache, Path('onnx') / (name + '.ranges.json'))
    binding = {'repo': repo, 'revision': revision, 'name': name, 'bytes': size, 'sha256': expected_sha}
    url = f'https://huggingface.co/{repo}/resolve/{revision}/{name}'
    path.parent.mkdir(parents=True, exist_ok=True)
    with exclusive_lock(path.with_name(path.name + '.lock')):
        if path.exists():
            require(not part.exists(), 'Both final and partial files exist; inspect ownership')
            digest = verify_file(path, item)
            return {**binding, 'path': path.relative_to(root).as_posix(), 'url': url, 'verified': True, 'sha256': digest, 'reusedFinal': True}
        require(part.exists() == manifest_path.exists(), 'Orphan partial/manifest refused')
        current = resource_bytes(root)
        required = 0 if part.exists() else size
        projected = check_budget(current['totalBytes'], required, reserve)
        if part.exists():
            state = json.loads(manifest_path.read_text(encoding='utf-8'))
            completed = validated_resume(part, state, binding, chunk_bytes)
            partial = validated_partial_resume(part, state, binding, chunk_bytes)
        else:
            with part.open('xb') as target:
                target.truncate(size)
                target.flush()
                os.fsync(target.fileno())
            completed = {}
            partial = {}
            state = {'version': 1, 'binding': binding, 'chunkBytes': chunk_bytes, 'completed': completed}
        state['completed'] = completed
        state['partial'] = partial
        write_json(manifest_path, state)
        state_lock = threading.Lock()
        stopped = threading.Event()
        count = (size + chunk_bytes - 1) // chunk_bytes
        def fetch(index):
            require(not stopped.is_set(), 'Range group stopped after a peer failure')
            key = str(index)
            if key in completed:
                return index
            base, end = index * chunk_bytes, min((index + 1) * chunk_bytes, size) - 1
            length = end - base + 1
            have = partial.get(key, {}).get('bytes', 0)
            for attempt in range(attempts):
                start = base + have
                result = hash_prefix(part, base, have)
                try:
                    require(not stopped.is_set(), 'Range group stopped after a peer failure')
                    req = urllib.request.Request(url, headers={'Range': f'bytes={start}-{end}', 'Accept-Encoding': 'identity'})
                    with opener(req, timeout=45) as response:
                        validate_range(response.status, response.headers, start, end, size)
                        remaining = end - start + 1
                        with part.open('r+b', buffering=0) as target:
                            target.seek(start)
                            try:
                                while remaining:
                                    require(not stopped.is_set(), 'Range group stopped after a peer failure')
                                    read_error = None
                                    read_limit = min(1024**2, remaining)
                                    try:
                                        block = response.read(read_limit)
                                    except IncompleteRead as error:
                                        block, read_error = error.partial, error
                                    require(isinstance(block, bytes) and len(block) <= read_limit, 'Range body exceeds requested bounds')
                                    require(block, 'Truncated range response')
                                    require(target.write(block) == len(block), 'Short local file write')
                                    result.update(block)
                                    have += len(block)
                                    remaining -= len(block)
                                    if read_error is not None:
                                        raise read_error
                                require(not response.read(1), 'Range has unexpected trailing bytes')
                            finally:
                                target.flush()
                                os.fsync(target.fileno())
                        require(sha256_file(part, base, length) == result.hexdigest(), 'Written range failed disk hash')
                    with state_lock:
                        completed[key] = result.hexdigest()
                        partial.pop(key, None)
                        write_json(manifest_path, state)
                    print(f'completed range {index + 1}/{count}', flush=True)
                    return index
                except Exception as error:
                    # A failed response is never itself promoted to a completed chunk.
                    # If all expected bytes arrived before a later error, refetch the last byte.
                    reusable = min(have, length - 1)
                    if reusable:
                        digest = sha256_file(part, base, reusable)
                        if reusable == have:
                            require(digest == result.hexdigest(), 'Failed-response prefix differs from written payload')
                        with state_lock:
                            partial[key] = {'bytes': reusable, 'sha256': digest}
                            write_json(manifest_path, state)
                    have = reusable
                    write_json(evidence / f'RangeError_{time.time_ns()}_{index}.json', {'index': index, 'attempt': attempt + 1, 'start': start, 'end': end, 'receivedPrefixBytes': have, 'error': str(error)}, fresh=True)
                    if attempt + 1 == attempts or stopped.is_set():
                        stopped.set()
                        raise
            raise DownloadError('Range retry exhaustion')
        with ThreadPoolExecutor(max_workers=workers) as pool:
            futures = [pool.submit(fetch, index) for index in range(count)]
            for future in as_completed(futures):
                future.result()
        require(len(completed) == count, 'Not every chunk completed')
        actual = verify_file(part, item)
        os.replace(part, path)
        after = resource_bytes(root)
        check_budget(after['totalBytes'], 0, reserve)
        return {**binding, 'path': path.relative_to(root).as_posix(), 'url': url, 'verified': True, 'sha256': actual,
                'ranges': count, 'workers': workers, 'chunkBytes': chunk_bytes, 'singlePreallocatedFile': True,
                'budgetBefore': current, 'reserveBytes': reserve, 'budgetProjectedMax': projected, 'budgetAfter': after}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--weights', action='store_true', help='Explicitly download the reviewed 6.2 GB external weight after tool acceptance')
    parser.add_argument('--label', required=True, help='Fresh evidence label, never overwrites a report')
    args = parser.parse_args()
    require(re.fullmatch('[A-Za-z0-9_-]{1,64}', args.label), 'Invalid evidence label')
    evidence, cache = safe_child(ROOT, EVIDENCE), safe_child(ROOT, CACHE)
    output = evidence / ('Downloads_' + args.label + '.json')
    require(not output.exists(), 'Fresh output label required')
    evidence.mkdir(parents=True, exist_ok=True)
    records = []
    report = {'mode': 'weights' if args.weights else 'small-only', 'completed': False, 'verified': False,
              'reserveBytes': DEFAULT_RESERVE, 'records': records, 'startedUtc': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime())}
    try:
        check_budget(resource_bytes(ROOT)['totalBytes'], 0, DEFAULT_RESERVE)
        original = acquire_metadata(ROOT, evidence, 'original', ORIGINAL_REPO, ORIGINAL_REVISION)
        onnx = acquire_metadata(ROOT, evidence, 'onnx', ONNX_REPO, ONNX_REVISION)
        for repo, revision, sub, metadata, names in (
            (ORIGINAL_REPO, ORIGINAL_REVISION, 'original', original, ('LICENSE', 'README.md', 'config.json', 'tokenizer.json', 'tokenizer_config.json', 'generation_config.json')),
            (ONNX_REPO, ONNX_REVISION, 'onnx', onnx, ('README.md', 'config.json', GRAPH[0])),
        ):
            for name in names:
                record = fetch_small(ROOT, cache, evidence, sub, repo, revision, name, metadata, DEFAULT_RESERVE)
                if name == GRAPH[0]:
                    require(record['bytes'] == GRAPH[1] and record['sha256'] == GRAPH[2], 'FP32 graph differs from reviewed pin')
                records.append(record)
                print('ready', sub, name, record['bytes'], flush=True)
        if args.weights:
            records.append(download_ranges(ROOT, cache, evidence, onnx, DEFAULT_RESERVE))
        report['budgetAfter'] = resource_bytes(ROOT)
        check_budget(report['budgetAfter']['totalBytes'], 0, DEFAULT_RESERVE)
        report['completed'] = report['verified'] = True
    except Exception as error:
        report['error'] = {'type': type(error).__name__, 'message': str(error)}
        raise
    finally:
        report['finishedUtc'] = time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime())
        write_json(output, report, fresh=True)
        print(json.dumps({'output': str(output), 'completed': report['completed'], 'verified': report['verified']}), flush=True)


if __name__ == '__main__':
    main()
