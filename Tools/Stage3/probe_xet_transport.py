"""Bounded official hf-xet transport leaf. Never downloads a model weight.

Each stage owns only its child process, project-local SDK/cache, and fresh evidence.
No global env/network changes; existing proxy details and temporary tokens stay private.
"""
from pathlib import Path
import argparse
import hashlib
import importlib.metadata
import json
import math
import os
import re
import subprocess
import sys
import threading
import time
import urllib.request
from urllib.parse import urlsplit
import zipfile

ROOT = Path(__file__).resolve().parents[2]
EV = ROOT / 'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen15/XetTransport'
CACHE = ROOT / 'Library/EchoStation/S3Cache/models/qwen15-candidate/xet-transport'
VERSION = '1.7.0'
WHEEL_NAME = 'hf_xet-1.7.0-cp38-abi3-win_amd64.whl'
WHEEL_BYTES = 3822644
WHEEL_SHA = '2b7bb5727889b0f2436dbaaad8fc4c3e66b8240d992716989e0c086b4278b1bc'
WHEEL_URL = 'https://files.pythonhosted.org/packages/4d/59/b35106dfa71b6eef605dc88bd038fe99c7f86fb132a15b60d0bf2f235b2c/hf_xet-1.7.0-cp38-abi3-win_amd64.whl'
REPO = 'Qwen/Qwen3-0.6B'
REVISION = 'c1899de289a04d12100db370d81485cdf75e47ca'
FILE = 'tokenizer.json'
FILE_BYTES = 11422654
FILE_SHA = 'aeb13307a71acd8fe81861d94ad54ab689df773318809eed3cbe794b4492dae4'
LIMIT = 100 * 1024**2
SOURCE_REPO = 'onnx-community/Qwen2.5-1.5B-Instruct'
SOURCE_REVISION = 'd54957e3af1be04546436e874a60fe386041b244'
SOURCE_NAME = 'onnx/model.onnx_data'
SOURCE_BYTES = 6208411648
SOURCE_SHA = '7111acf0e546fe192035357890402832315b4c243e56b01b47ce9358285fc82e'
SOURCE_FOLDER = Path('Library/EchoStation/S3Cache/models/qwen15-candidate/onnx/onnx')


def sha(path):
    with path.open('rb') as source:
        return hashlib.file_digest(source, 'sha256').hexdigest()


def save(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('xb') as output:
        output.write(json.dumps(value, ensure_ascii=False, indent=2).encode('utf-8'))


def sanitized(value):
    text = str(value)
    proxy = urllib.request.getproxies().get('https')
    if proxy:
        text = text.replace(proxy, '[existing proxy redacted]')
    text = re.sub(r'https?://[^\s\"\'<>]+', '[URL redacted]', text)
    text = re.sub(r'\b(?:hf_[A-Za-z0-9]+|eyJ[A-Za-z0-9_.-]+)\b', '[token redacted]', text)
    text = re.sub(r'(?i)(authorization|access_token|token_info)[\s:=]+[^,\n]+', r'\1 [redacted]', text)
    return text[:12000]


def owned_size():
    parts = [CACHE, EV]
    site = ROOT / 'Library/EchoStation/S3Python/Lib/site-packages'
    parts += [site / 'hf_xet', site / f'hf_xet-{VERSION}.dist-info']
    return sum(p.stat().st_size for base in parts if base.exists() for p in base.rglob('*') if p.is_file())


def ensure_budget():
    size = owned_size()
    if size > LIMIT:
        raise ValueError('Transport leaf exceeded 100 MiB cap')
    return size


def worker_package(label):
    start = time.monotonic()
    with urllib.request.urlopen(f'https://pypi.org/pypi/hf-xet/{VERSION}/json', timeout=30) as response:
        data = json.load(response)
    files = [v for v in data['urls'] if v['filename'] == WHEEL_NAME]
    if len(files) != 1 or files[0]['size'] != WHEEL_BYTES or files[0]['digests']['sha256'] != WHEEL_SHA or files[0]['url'] != WHEEL_URL:
        raise ValueError('Pinned official PyPI wheel metadata differs')
    save(EV / f'PyPI_{label}.json', data)
    wheel = CACHE / 'wheels' / WHEEL_NAME
    wheel.parent.mkdir(parents=True, exist_ok=True)
    if not wheel.exists():
        part = wheel.with_suffix('.part')
        with urllib.request.urlopen(WHEEL_URL, timeout=30) as response, part.open('xb') as output:
            received = 0
            while block := response.read(1024**2):
                received += len(block)
                if received > WHEEL_BYTES:
                    raise ValueError('Wheel response exceeds pinned length')
                output.write(block)
        if part.stat().st_size != WHEEL_BYTES or sha(part) != WHEEL_SHA:
            raise ValueError('Wheel length/SHA mismatch')
        part.replace(wheel)
    if wheel.stat().st_size != WHEEL_BYTES or sha(wheel) != WHEEL_SHA:
        raise ValueError('Existing pinned wheel differs')
    with zipfile.ZipFile(wheel) as archive:
        names = [name for name in archive.namelist() if name.endswith('/LICENSE')]
        if len(names) != 1:
            raise ValueError('Expected one wheel LICENSE')
        license_data = archive.read(names[0])
        if b'Apache License' not in license_data or b'Version 2.0' not in license_data:
            raise ValueError('Expected Apache-2.0 license')
        (EV / f'LICENSE_{label}.txt').write_bytes(license_data)
    remaining = max(1, min(90, 110 - (time.monotonic() - start)))
    completed = subprocess.run([sys.executable, '-m', 'pip', 'install', '--no-index', '--no-deps', '--no-cache-dir', '--no-compile', '--disable-pip-version-check', '--report', str(EV / f'Pip_{label}.json'), str(wheel)], timeout=remaining, capture_output=True, text=True)
    if completed.returncode:
        raise ValueError('Project-only pip install failed: ' + sanitized(completed.stderr))
    if importlib.metadata.version('hf_xet') != VERSION:
        raise ValueError('Installed version differs')
    return {'stage': 'package', 'passed': True, 'version': VERSION, 'wheelBytes': WHEEL_BYTES,
            'wheelSha256': WHEEL_SHA, 'officialSource': 'pypi.org / files.pythonhosted.org',
            'license': 'Apache-2.0', 'sitePackages': 'Library/EchoStation/S3Python/Lib/site-packages',
            'hubVersion': importlib.metadata.version('huggingface_hub'), 'ownedBytes': ensure_budget(),
            'elapsedSeconds': time.monotonic() - start}


def worker_benchmark(label):
    # All HF variables have been set by the parent before this new Python import.
    from huggingface_hub.file_download import get_hf_file_metadata, xet_get
    url = f'https://huggingface.co/{REPO}/resolve/{REVISION}/{FILE}'
    start = time.monotonic()
    metadata = get_hf_file_metadata(url, token=False, timeout=20)
    metadata_seconds = time.monotonic() - start
    if metadata.commit_hash != REVISION or metadata.size != FILE_BYTES or metadata.etag != FILE_SHA:
        raise ValueError('Official tiny benchmark metadata differs from existing verified evidence')
    if metadata.xet_file_data is None:
        return {'stage': 'benchmark', 'passed': False, 'reason': 'Official tiny file has no Xet metadata',
                'metadataSeconds': metadata_seconds, 'ownedBytes': ensure_budget()}
    destination = CACHE / 'downloads' / f'{label}_tokenizer.json'
    if destination.exists():
        raise ValueError('Fresh benchmark output required')
    destination.parent.mkdir(parents=True, exist_ok=True)
    save(EV / f'Metadata_{label}.json', {'repo': REPO, 'revision': REVISION, 'name': FILE,
         'bytes': metadata.size, 'etag': metadata.etag, 'hasXetMetadata': True,
         'xetFileHash': metadata.xet_file_data.file_hash, 'metadataSeconds': metadata_seconds})
    transfer_start = time.monotonic()
    xet_get(incomplete_path=destination, xet_file_data=metadata.xet_file_data, headers={},
            expected_size=FILE_BYTES, displayed_filename='bounded official tokenizer transport probe')
    transfer_seconds = time.monotonic() - transfer_start
    if destination.stat().st_size != FILE_BYTES or sha(destination) != FILE_SHA:
        raise ValueError('Actual Xet tiny download size/SHA differs')
    return {'stage': 'benchmark', 'passed': True, 'transport': 'huggingface_hub.xet_get -> hf_xet.download_files',
            'repo': REPO, 'revision': REVISION, 'name': FILE, 'bytes': FILE_BYTES, 'sha256': FILE_SHA,
            'metadataSeconds': metadata_seconds, 'transferSeconds': transfer_seconds,
            'elapsedSeconds': time.monotonic() - start, 'mibPerSecond': FILE_BYTES / 1024**2 / transfer_seconds,
            'ownedBytes': ensure_budget(), 'modelWeightsDownloaded': False}


def validate_source_metadata(metadata):
    if metadata.commit_hash != SOURCE_REVISION or metadata.size != SOURCE_BYTES or metadata.etag != SOURCE_SHA:
        raise ValueError('Source HTTP commit/size/LFS identity differs from reviewed pin')
    data = metadata.xet_file_data
    if data is None or re.fullmatch('[0-9a-f]{64}', data.file_hash) is None:
        raise ValueError('Source requires a valid Xet file identity; HTTP fallback prohibited')
    route = urlsplit(data.refresh_route)
    if route.scheme != 'https' or route.hostname != 'huggingface.co':
        raise ValueError('Source Xet public credential route is not official HTTPS')
    return data.file_hash


def source_paths(root):
    import download_qwen15_candidate as guards
    final = guards.safe_child(root, SOURCE_FOLDER / 'model.onnx_data')
    part = guards.safe_child(root, SOURCE_FOLDER / 'model.onnx_data.xet.part')
    old = guards.safe_child(root, SOURCE_FOLDER / 'model.onnx_data.part')
    if final.exists() or part.exists() or old.exists():
        raise ValueError('Source requires fresh Xet target and no active/original HTTP partial; preserve existing data first')
    return final, part


def source_budget(root, part):
    import download_qwen15_candidate as guards
    stats = guards.resource_bytes(root)
    complete = part.with_name('model.onnx_data')
    have = part.stat().st_size if part.exists() else complete.stat().st_size if complete.exists() else 0
    if have > SOURCE_BYTES:
        raise ValueError('Xet source file exceeds pinned full size')
    projected = guards.check_budget(stats['totalBytes'], SOURCE_BYTES - have, guards.DEFAULT_RESERVE)
    return {'actualBytes': stats['totalBytes'], 'remainingSourceReservedBytes': SOURCE_BYTES - have,
            'quantizationAndInstallationReserveBytes': guards.DEFAULT_RESERVE,
            'projectedBytes': projected, 'limitBytes': guards.LIMIT_BYTES}


def promote_source(part, final, *, expected_bytes=SOURCE_BYTES, expected_sha=SOURCE_SHA):
    if final.exists():
        raise ValueError('Never overwrite existing complete source')
    if not part.is_file() or part.stat().st_size != expected_bytes:
        raise ValueError('Native source download length differs from reviewed pin')
    actual = sha(part)
    if actual != expected_sha:
        raise ValueError('Native source full LFS SHA differs; partial preserved, no promotion')
    with part.open('rb') as source:
        os.fsync(source.fileno())
    # Refuse a raced destination. Windows rename does not replace an existing file.
    part.rename(final)
    return actual


def atomic_progress(path, record):
    temporary = path.with_name(path.name + '.atomic')
    temporary.write_bytes(json.dumps(record, ensure_ascii=False, indent=2).encode('utf-8'))
    os.replace(temporary, path)


def worker_source(label):
    import download_qwen15_candidate as guards
    final, part = source_paths(ROOT)
    before = source_budget(ROOT, part)
    progress_path = EV / f'Progress_{label}.json'
    if progress_path.exists():
        raise ValueError('Fresh source progress label required')
    start = time.monotonic()
    progress = {'stage': 'source', 'phase': 'metadata', 'reconstructedBytes': 0,
                'expectedBytes': SOURCE_BYTES, 'elapsedSeconds': 0, 'progressValid': True}
    progress_lock, stopped = threading.Lock(), threading.Event()
    def publish():
        with progress_lock:
            progress['elapsedSeconds'] = time.monotonic() - start
            atomic_progress(progress_path, dict(progress))
    def heartbeat():
        while not stopped.wait(10):
            publish()
    def update(delta):
        with progress_lock:
            if type(delta) not in (int, float) or not math.isfinite(delta) or delta != int(delta) or delta < 0 or progress['reconstructedBytes'] + delta > SOURCE_BYTES:
                progress['progressValid'] = False
                raise ValueError('Unexpected native reconstruction progress delta')
            progress['reconstructedBytes'] += int(delta)
    publish()
    reporter = threading.Thread(target=heartbeat, name='XetSourceProgress', daemon=True)
    reporter.start()
    try:
        from huggingface_hub.file_download import get_hf_file_metadata
        from huggingface_hub.utils._xet import refresh_xet_connection_info
        from hf_xet import PyXetDownloadInfo, download_files
        metadata = get_hf_file_metadata(f'https://huggingface.co/{SOURCE_REPO}/resolve/{SOURCE_REVISION}/{SOURCE_NAME}', token=False, timeout=20)
        file_hash = validate_source_metadata(metadata)
        save(EV / f'SourceMetadata_{label}.json', {'repo': SOURCE_REPO, 'revision': SOURCE_REVISION,
             'name': SOURCE_NAME, 'bytes': SOURCE_BYTES, 'lfsSha256': SOURCE_SHA,
             'xetFileHash': file_hash, 'httpFallbackAllowed': False})
        connection = refresh_xet_connection_info(file_data=metadata.xet_file_data, headers={})
        def refresh():
            renewed = refresh_xet_connection_info(file_data=metadata.xet_file_data, headers={})
            return renewed.access_token, renewed.expiration_unix_epoch
        part.parent.mkdir(parents=True, exist_ok=True)
        with progress_lock:
            progress['phase'] = 'download'
        publish()
        download_files([PyXetDownloadInfo(destination_path=str(part), hash=file_hash, file_size=SOURCE_BYTES)],
                       endpoint=connection.endpoint, token_info=(connection.access_token, connection.expiration_unix_epoch),
                       token_refresher=refresh, progress_updater=[update])
        with progress_lock:
            progress['phase'] = 'verify-full-lfs-sha'
            if not progress['progressValid']:
                raise ValueError('Native reconstruction progress contract failed')
        publish()
        source_budget(ROOT, part)
        actual = promote_source(part, final)
        with progress_lock:
            progress['phase'] = 'completed'
        publish()
        return {'stage': 'source', 'passed': True, 'repo': SOURCE_REPO, 'revision': SOURCE_REVISION,
                'name': SOURCE_NAME, 'bytes': SOURCE_BYTES, 'sha256': actual, 'xetFileHash': file_hash,
                'transport': 'hf_xet.download_files (native Xet only, no HTTP fallback)',
                'path': final.relative_to(ROOT).as_posix(), 'budgetBefore': before,
                'elapsedSeconds': time.monotonic() - start, 'quantizedModelOrInferenceValidated': False}
    finally:
        stopped.set()
        reporter.join(timeout=2)
        publish()


def stage_environment():
    env = os.environ.copy()
    proxy = urllib.request.getproxies().get('https')
    for name in list(env):
        if name.upper().endswith('_PROXY') or name.upper() in ('HF_TOKEN', 'HUGGING_FACE_HUB_TOKEN'):
            env.pop(name)
    if proxy:
        env['HTTPS_PROXY'] = proxy
    env.update({'HF_HOME': str(CACHE / 'home'), 'HF_HUB_CACHE': str(CACHE / 'hub'),
                'HF_XET_CACHE': str(CACHE / 'xet'), 'HF_ASSETS_CACHE': str(CACHE / 'assets'),
                'HF_TOKEN_PATH': str(CACHE / 'home' / 'no-token'), 'HF_HUB_DISABLE_IMPLICIT_TOKEN': '1',
                'HF_HUB_DISABLE_TELEMETRY': '1', 'DO_NOT_TRACK': '1', 'HF_HUB_DISABLE_PROGRESS_BARS': '1',
                'HF_XET_HIGH_PERFORMANCE': '1', 'HF_XET_CHUNK_CACHE_SIZE_BYTES': '0',
                'HF_XET_NUM_CONCURRENT_RANGE_GETS': '4', 'HF_XET_LOG_LEVEL': 'off',
                'HF_XET_RECONSTRUCTION_DOWNLOAD_BUFFER_PERFILE_SIZE': '64mb',
                'HF_XET_RECONSTRUCTION_DOWNLOAD_BUFFER_SIZE': '64mb',
                'HF_XET_RECONSTRUCTION_DOWNLOAD_BUFFER_LIMIT': '264mb',
                'HF_XET_SHARD_CACHE_SIZE_LIMIT': '0',
                'RUST_LOG': 'off', 'HF_HUB_DOWNLOAD_TIMEOUT': '20', 'HF_HUB_ETAG_TIMEOUT': '20',
                'PIP_DISABLE_PIP_VERSION_CHECK': '1', 'PYTHONUTF8': '1'})
    return env, bool(proxy)


def stop_owned_tree(process, label):
    """The Windows venv launcher can own the real Python/Rust child process."""
    if os.name != 'nt':
        if process.poll() is None:
            process.kill()
        return {'method': 'owned process kill'}
    # The launched PID and worker receipt are both our own; verify fresh CIM command
    # lines before taskkill. Never enumerate or stop unrelated Python/Unity processes.
    candidates = [process.pid]
    receipt = EV / f'Worker_{label}.json'
    if receipt.exists():
        owner = json.loads(receipt.read_text(encoding='utf-8'))
        if owner.get('label') == label and type(owner.get('pid')) is int:
            candidates.insert(0, owner['pid'])
    stopped = []
    for pid in dict.fromkeys(candidates):
        script = "$p=Get-CimInstance Win32_Process -Filter 'ProcessId = " + str(pid) + "';if($null -ne $p){@{pid=$p.ProcessId;command=$p.CommandLine}|ConvertTo-Json -Compress}"
        lookup = subprocess.run(['powershell.exe', '-NoProfile', '-NonInteractive', '-Command', script], capture_output=True, text=True, timeout=5)
        if not lookup.stdout.strip():
            continue
        actual = json.loads(lookup.stdout)
        command = actual.get('command', '')
        if actual.get('pid') != pid or str(Path(__file__).resolve()).casefold() not in command.casefold() or f'--label {label}' not in command or '--worker' not in command:
            raise ValueError('Timeout PID ownership verification failed; no unrelated process stopped')
        result = subprocess.run(['taskkill.exe', '/PID', str(pid), '/T', '/F'], capture_output=True, text=True, timeout=10)
        stopped.append({'pid': pid, 'taskkillExitCode': result.returncode})
    return {'method': 'verified owned PID trees taskkill /T /F', 'stopped': stopped}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--stage', choices=('package', 'benchmark', 'source'), required=True)
    parser.add_argument('--label', required=True)
    parser.add_argument('--max-seconds', type=int)
    parser.add_argument('--worker', action='store_true')
    args = parser.parse_args()
    if not re.fullmatch('[A-Za-z0-9_-]{1,64}', args.label):
        raise ValueError('Invalid fresh label')
    maximum = args.max_seconds if args.max_seconds is not None else 1200 if args.stage == 'source' else 120
    if not 1 <= maximum <= (1200 if args.stage == 'source' else 120):
        raise ValueError('Stage work limit exceeds authorized bound')
    EV.mkdir(parents=True, exist_ok=True)
    if args.worker:
        save(EV / f'Worker_{args.label}.json', {'pid': os.getpid(), 'parentPid': os.getppid(), 'label': args.label})
        result = worker_package(args.label) if args.stage == 'package' else worker_source(args.label) if args.stage == 'source' else worker_benchmark(args.label)
        print(json.dumps(result), flush=True)
        return
    output = EV / f'{args.label}.json'
    if output.exists():
        raise ValueError('Fresh evidence label required')
    before = ensure_budget()
    env, proxy_present = stage_environment()
    start = time.monotonic()
    record = {'stage': args.stage, 'label': args.label, 'passed': False, 'stageLimitSeconds': maximum,
              'beforeOwnedBytes': before, 'diskLimitBytes': LIMIT, 'existingProxyPassedOnlyToChild': proxy_present,
              'modelWeightsDownloaded': None if args.stage == 'source' else False, 'projectOnlyEnvironment': True}
    process = None
    try:
        if args.stage == 'source':
            import download_qwen15_candidate as guards
            _, source_part = source_paths(ROOT)
            record['diskLimitBytes'] = guards.LIMIT_BYTES
            record['globalBudgetBefore'] = source_budget(ROOT, source_part)
            record['completeSourceVerified'] = False
        process = subprocess.Popen([sys.executable, '-X', 'utf8', __file__, '--stage', args.stage,
                                    '--label', args.label, '--max-seconds', str(maximum), '--worker'], env=env,
                                   creationflags=subprocess.CREATE_NEW_PROCESS_GROUP if os.name == 'nt' else 0,
                                   stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        while True:
            remaining = maximum - (time.monotonic() - start)
            if remaining <= 0:
                raise subprocess.TimeoutExpired(process.args, maximum)
            try:
                stdout, stderr = process.communicate(timeout=min(2, remaining) if args.stage == 'source' else remaining)
                break
            except subprocess.TimeoutExpired:
                if args.stage != 'source':
                    raise
                record['lastGlobalBudget'] = source_budget(ROOT, source_part)
                progress_path = EV / f'Progress_{args.label}.json'
                if progress_path.exists() and json.loads(progress_path.read_bytes()).get('progressValid') is False:
                    raise ValueError('Native source progress contract failed')
                atomic_progress(EV / f'Budget_{args.label}.json', {**record['lastGlobalBudget'], 'elapsedSeconds': time.monotonic() - start})
        record['exitCode'] = process.returncode
        lines = stdout.strip().splitlines()
        if process.returncode == 0 and lines:
            record['result'] = json.loads(lines[-1])
            record['passed'] = record['result'].get('passed') is True
            if args.stage == 'source':
                record['completeSourceVerified'] = record['passed']
        else:
            record['error'] = sanitized(stderr or stdout)
    except subprocess.TimeoutExpired as error:
        record['timedOut'] = True
        record['timeoutCleanup'] = stop_owned_tree(process, args.label)
        process.communicate(timeout=15)
        record['error'] = 'Owned child trees stopped after stage work limit; no unrelated process was touched'
        if error.stderr:
            record['childStderrRedacted'] = sanitized(error.stderr)
    except Exception as error:
        record['error'] = sanitized(type(error).__name__ + ': ' + str(error))
        if process is not None:
            record['failureCleanup'] = stop_owned_tree(process, args.label)
            process.communicate(timeout=15)
    finally:
        record['elapsedSeconds'] = time.monotonic() - start
        record['afterOwnedBytes'] = ensure_budget()
        save(output, record)
        print(json.dumps({'output': str(output), 'passed': record['passed'], 'elapsedSeconds': record['elapsedSeconds'],
                          'afterOwnedBytes': record['afterOwnedBytes']}), flush=True)


if __name__ == '__main__':
    main()
