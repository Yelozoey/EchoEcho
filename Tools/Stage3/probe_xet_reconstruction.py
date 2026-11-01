"""Read-only CAS reconstruction accounting. Never fetches any xorb/model data."""
from pathlib import Path
import argparse
import json
import os
import subprocess
import sys
import time
import urllib.request

import probe_xet_transport as transport

REPO = 'onnx-community/Qwen2.5-1.5B-Instruct'
REVISION = 'd54957e3af1be04546436e874a60fe386041b244'
NAME = 'onnx/model.onnx_data'
SIZE = 6208411648
SHA = '7111acf0e546fe192035357890402832315b4c243e56b01b47ce9358285fc82e'


def physical_summary(reconstruction):
    terms = reconstruction['terms']
    xorbs = reconstruction['xorbs']
    offset = reconstruction['offset_into_first_range']
    assert offset == 0
    unpacked = sum(term['unpacked_length'] for term in terms)
    assert unpacked == SIZE, 'Reconstruction does not describe the complete fixed file'
    physical, unique, range_count, fetch_count = 0, 0, 0, 0
    for entries in xorbs.values():
        intervals = []
        fetch_count += len(entries)
        for entry in entries:
            # Signed URL deliberately not accessed, printed, or persisted.
            for descriptor in entry['ranges']:
                start, end = descriptor['bytes']['start'], descriptor['bytes']['end']
                assert type(start) is int and type(end) is int and 0 <= start <= end
                physical += end - start + 1
                range_count += 1
                intervals.append((start, end))
        merged = []
        for start, end in sorted(intervals):
            if merged and start <= merged[-1][1] + 1:
                merged[-1] = (merged[-1][0], max(merged[-1][1], end))
            else:
                merged.append((start, end))
        unique += sum(end - start + 1 for start, end in merged)
    return {'termCount': len(terms), 'xorbCount': len(xorbs), 'fetchEntryCount': fetch_count,
            'physicalRangeCount': range_count, 'physicalRangeBytesSum': physical,
            'uniquePhysicalRangeBytesByXorb': unique, 'unpackedTermBytes': unpacked,
            'physicalToUnpackedRatio': physical / unpacked,
            'uniquePhysicalToUnpackedRatio': unique / unpacked,
            'termChunkCountIncludingReuse': sum(x['range']['end'] - x['range']['start'] for x in terms)}


def worker(label):
    transport.save(transport.EV / f'ReconstructionWorker_{label}.json', {'pid': os.getpid(), 'parentPid': os.getppid(), 'label': label})
    from huggingface_hub.file_download import get_hf_file_metadata
    from huggingface_hub.utils._xet import refresh_xet_connection_info
    start = time.monotonic()
    metadata = get_hf_file_metadata(f'https://huggingface.co/{REPO}/resolve/{REVISION}/{NAME}', token=False, timeout=20)
    assert metadata.commit_hash == REVISION and metadata.size == SIZE and metadata.etag == SHA
    assert metadata.xet_file_data is not None, 'Fixed file has no Xet metadata'
    metadata_seconds = time.monotonic() - start
    connection = refresh_xet_connection_info(file_data=metadata.xet_file_data, headers={})
    request = urllib.request.Request(connection.endpoint.rstrip('/') + '/v2/reconstructions/' + metadata.xet_file_data.file_hash,
                                     headers={'Authorization': 'Bearer ' + connection.access_token, 'Accept': 'application/json'})
    query_start = time.monotonic()
    with urllib.request.urlopen(request, timeout=30) as response:
        assert response.status == 200
        raw = response.read(8 * 1024**2 + 1)
        assert len(raw) <= 8 * 1024**2, 'Reconstruction JSON exceeds research cap'
    summary = physical_summary(json.loads(raw))
    return {'passed': True, 'metadataOnly': True, 'noXorbDataRequested': True,
            'repo': REPO, 'revision': REVISION, 'name': NAME, 'fileBytes': SIZE, 'sha256': SHA,
            'metadataSeconds': metadata_seconds, 'reconstructionQuerySeconds': time.monotonic() - query_start,
            'metadataResponseBytes': len(raw), 'highPerformanceRequested': os.environ.get('HF_XET_HIGH_PERFORMANCE') == '1',
            'tokenOrSignedUrlsPersisted': False, 'elapsedSeconds': time.monotonic() - start, **summary}


def cleanup(process, label):
    receipt = transport.EV / f'ReconstructionWorker_{label}.json'
    candidates = [process.pid]
    if receipt.exists():
        owner = json.loads(receipt.read_bytes())
        if owner.get('label') == label and type(owner.get('pid')) is int:
            candidates.insert(0, owner['pid'])
    stopped = []
    for pid in dict.fromkeys(candidates):
        query = "$p=Get-CimInstance Win32_Process -Filter 'ProcessId = " + str(pid) + "';if($p){@{pid=$p.ProcessId;command=$p.CommandLine}|ConvertTo-Json -Compress}"
        result = subprocess.run(['powershell.exe', '-NoProfile', '-NonInteractive', '-Command', query], capture_output=True, text=True, timeout=5)
        if not result.stdout.strip():
            continue
        actual = json.loads(result.stdout)
        assert actual.get('pid') == pid and str(Path(__file__).resolve()).casefold() in actual.get('command', '').casefold()
        assert f'--label {label}' in actual['command'] and '--worker' in actual['command']
        terminated = subprocess.run(['taskkill.exe', '/PID', str(pid), '/T', '/F'], capture_output=True, timeout=10)
        stopped.append({'pid': pid, 'exitCode': terminated.returncode})
    return stopped


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--label', required=True)
    parser.add_argument('--worker', action='store_true')
    args = parser.parse_args()
    assert args.label.replace('_', '').isalnum() and len(args.label) <= 64
    if args.worker:
        print(json.dumps(worker(args.label)), flush=True)
        return
    output = transport.EV / f'Reconstruction_{args.label}.json'
    assert not output.exists(), 'Fresh research evidence required'
    env, proxy = transport.stage_environment()
    start = time.monotonic()
    record = {'passed': False, 'metadataOnly': True, 'workLimitSeconds': 120,
              'existingProxyOnlyInChild': proxy, 'activeDownloadTouched': False}
    process = subprocess.Popen([sys.executable, '-X', 'utf8', __file__, '--label', args.label, '--worker'], env=env,
                               creationflags=subprocess.CREATE_NEW_PROCESS_GROUP, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
    try:
        stdout, stderr = process.communicate(timeout=120)
        record['exitCode'] = process.returncode
        if process.returncode == 0:
            record['result'] = json.loads(stdout.strip().splitlines()[-1])
            record['passed'] = record['result']['passed']
        else:
            record['errorRedacted'] = transport.sanitized(stderr or stdout)
    except subprocess.TimeoutExpired:
        record['timedOut'] = True
        record['cleanup'] = cleanup(process, args.label)
        process.communicate(timeout=15)
    finally:
        record['elapsedSeconds'] = time.monotonic() - start
        record['ownedTransportBytes'] = transport.ensure_budget()
        transport.save(output, record)
        print(json.dumps({'output': str(output), 'passed': record['passed'], 'elapsedSeconds': record['elapsedSeconds']}), flush=True)


if __name__ == '__main__':
    main()
