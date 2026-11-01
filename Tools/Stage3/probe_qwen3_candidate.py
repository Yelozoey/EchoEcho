"""Isolated ORT candidate diagnostics; importing this module uses only the stdlib.

SHA checks bracket a run, without an immutable lease: changes restored between
those checks are not excluded. No Unity/product acceptance is claimed.
"""
from pathlib import Path
import argparse
import gc
import hashlib
import json
import math
import os
import re
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
BINDING_VERSION = 1
TOKENIZER_FILES = ('tokenizer.json', 'tokenizer_config.json', 'config.json', 'generation_config.json')
HASH_FIELDS = ('sourceModelSha256', 'optimizedModelSha256', 'candidateCasesSha256',
               'transformationSha256', 'downloadMetadataSha256', 'officialDownloadMetadataSha256')
NAMES = ['logits'] + [f'present.{i}.{kind}' for i in range(28) for kind in ('key', 'value')]
BINDING_POLICY = ('Start/end SHA256 checks of the named files; no immutable file lease. '
                  'Changes restored between the checks are not excluded.')


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha256_file(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read_json(path):
    return json.loads(Path(path).read_text(encoding='utf-8'))


def valid_sha(value):
    return type(value) is str and re.fullmatch(r'[a-f0-9]{64}', value) is not None


def validate_bindings(bindings):
    require(type(bindings) is dict, 'Missing binding object')
    require(set(bindings) == set(HASH_FIELDS) | {'officialTokenizerSha256'}, 'Incomplete binding keys')
    require(all(valid_sha(bindings[key]) for key in HASH_FIELDS), 'Invalid binding SHA256')
    tokenizer = bindings['officialTokenizerSha256']
    require(type(tokenizer) is dict and set(tokenizer) == set(TOKENIZER_FILES), 'Incomplete official tokenizer binding')
    require(all(valid_sha(value) for value in tokenizer.values()), 'Invalid official tokenizer SHA256')


def capture_bindings(root=ROOT):
    """Hash actual files and check their download/transform records, without native imports."""
    root = Path(root)
    evidence = root / 'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3'
    candidate = root / 'Library/EchoStation/S3Cache/models/qwen3-candidate'
    source = candidate / 'onnx/onnx/model_q4.onnx'
    optimized = candidate / 'onnx/onnx/model_q4_last.onnx'
    transformation_path = evidence / 'LastHeadTransformation.json'
    download_path = evidence / 'Downloads_ranges.json'
    official_download_path = evidence / 'Downloads_small.json'
    transformation, download = read_json(transformation_path), read_json(download_path)
    official_downloads = read_json(official_download_path)
    source_sha, optimized_sha = sha256_file(source), sha256_file(optimized)
    require(download.get('verified') is True, 'Source download is not verified')
    require(download.get('repo') == 'onnx-community/Qwen3-0.6B-ONNX', 'Wrong source repository')
    require(download.get('sha256') == source_sha == transformation.get('sourceSha256'), 'Source model SHA differs')
    require(transformation.get('sha256') == optimized_sha, 'Optimized model SHA differs')
    require(transformation.get('weightsUnchanged') is True, 'No unchanged-weight record')
    require(download.get('bytes') == source.stat().st_size, 'Source model size differs')
    require(transformation.get('bytes') == optimized.stat().st_size, 'Optimized model size differs')
    require(type(official_downloads) is list, 'Official download records must be a list')
    official, revisions = {}, set()
    for filename in TOKENIZER_FILES:
        path = candidate / 'original' / filename
        relative = path.relative_to(root).as_posix()
        records = [item for item in official_downloads
                   if item.get('repo') == 'Qwen/Qwen3-0.6B' and item.get('path') == relative]
        require(len(records) == 1, f'Missing or duplicate official record: {filename}')
        record, actual = records[0], sha256_file(path)
        require(record.get('sha256') == actual, f'Official file SHA differs: {filename}')
        require(record.get('bytes') == path.stat().st_size, f'Official file size differs: {filename}')
        revision = record.get('revision')
        require(type(revision) is str and re.fullmatch(r'[a-f0-9]{40}', revision), 'Invalid official revision')
        revisions.add(revision)
        official[filename] = actual
    require(len(revisions) == 1, 'Official tokenizer files have different revisions')
    bindings = {
        'sourceModelSha256': source_sha,
        'optimizedModelSha256': optimized_sha,
        'candidateCasesSha256': sha256_file(evidence / 'CandidateCases.json'),
        'officialTokenizerSha256': official,
        'transformationSha256': sha256_file(transformation_path),
        'downloadMetadataSha256': sha256_file(download_path),
        'officialDownloadMetadataSha256': sha256_file(official_download_path),
    }
    validate_bindings(bindings)
    return bindings


def validate_equivalence_report(report, actual_bindings):
    """Reject legacy, stale and partial reports; never infer success from a filename."""
    validate_bindings(actual_bindings)
    require(type(report) is dict, 'Equivalence report must be an object')
    require(report.get('mode') == 'equivalence', 'Wrong equivalence report mode')
    require(report.get('completed') is True and report.get('passed') is True, 'Equivalence did not complete and pass')
    require(type(report.get('bindingVersion')) is int and report['bindingVersion'] == BINDING_VERSION,
            'Unsupported or missing binding version')
    require(report.get('bindingsStable') is True, 'Equivalence bindings were not stable')
    validate_bindings(report.get('bindingsStart'))
    validate_bindings(report.get('bindingsEnd'))
    require(report['bindingsStart'] == report['bindingsEnd'] == actual_bindings, 'Equivalence bindings differ from actual files')
    comparisons = report.get('comparisons')
    require(type(comparisons) is list and len(comparisons) == 2, 'Equivalence needs both numerical steps')
    for index, comparison in enumerate(comparisons):
        require(type(comparison) is dict and type(comparison.get('step')) is int
                and comparison['step'] == index, 'Wrong numerical step')
        for key in ('logitsMaxAbs', 'kvMaxAbs'):
            value = comparison.get(key)
            require(type(value) in (int, float) and math.isfinite(value) and value >= 0, 'Invalid numerical difference')
        require(comparison['logitsMaxAbs'] <= 0.0001 and comparison['kvMaxAbs'] == 0, 'Numerical equivalence failed')


def error_data(error):
    """Detach native-holding traceback references on Owner; transfer strings only."""
    data = {'error': str(error), 'errorType': type(error).__name__}
    error.__traceback__ = error.__cause__ = error.__context__ = None
    return data


def save_report(path, report):
    Path(path).write_bytes(json.dumps(report, ensure_ascii=False, indent=2).encode('utf-8'))


def native_diagnostics(args, root, report, save):
    # Import only on Owner after the metadata gate; pure tests do not import these.
    import numpy as np
    import onnxruntime as ort
    from transformers import AutoTokenizer
    from validate_intent import validate

    folder = root / 'Library/EchoStation/S3Cache/models/qwen3-candidate/onnx/onnx'
    original = root / 'Library/EchoStation/S3Cache/models/qwen3-candidate/original'
    cases = read_json(root / 'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3/CandidateCases.json')['cases']
    require(type(cases) is list and cases, 'Candidate cases are empty')
    for case in cases:
        ids = case.get('ids')
        require(type(ids) is list and 1 <= len(ids) <= 1536
                and all(type(token) is int and 0 <= token < 151936 for token in ids), 'Invalid candidate input IDs')
    ort.disable_telemetry_events()

    def create_session(filename):
        value = None
        try:
            options = ort.SessionOptions()
            options.intra_op_num_threads, options.inter_op_num_threads = 2, 1
            options.execution_mode = ort.ExecutionMode.ORT_SEQUENTIAL
            options.add_session_config_entry('session.intra_op.allow_spinning', '0')
            options.add_session_config_entry('session.inter_op.allow_spinning', '0')
            value = ort.InferenceSession(str(folder / filename), sess_options=options, providers=['CPUExecutionProvider'])
            require(len(value.get_inputs()) == 59 and len(value.get_outputs()) == 57, 'Wrong model tensor count')
            expected = {'input_ids': 'tensor(int64)', 'attention_mask': 'tensor(int64)', 'position_ids': 'tensor(int64)',
                        **{f'past_key_values.{i}.{kind}': 'tensor(float)' for i in range(28) for kind in ('key', 'value')}}
            require({item.name: item.type for item in value.get_inputs()} == expected, 'Wrong model input contract')
            require(set(item.name for item in value.get_outputs()) == set(NAMES), 'Wrong model output names')
            return value
        except BaseException:
            value = None
            raise

    def empty():
        return {f'past_key_values.{i}.{kind}': np.zeros((1, 8, 0, 128), np.float32)
                for i in range(28) for kind in ('key', 'value')}

    def step(model, ids, past, consumed):
        total = consumed + len(ids)
        feed = {'input_ids': np.asarray([ids], np.int64), 'attention_mask': np.ones((1, total), np.int64),
                'position_ids': np.arange(consumed, total, dtype=np.int64)[None, :], **past}
        values = None
        try:
            values = model.run(NAMES, feed)
            require(values[0].ndim == 3 and values[0].shape[0] == 1
                    and values[0].shape[-1] == 151936 and np.isfinite(values[0]).all(), 'Invalid logits')
            for value in values[1:]:
                require(value.dtype == np.float32 and value.shape == (1, 8, total, 128)
                        and np.isfinite(value).all(), 'Invalid KV output')
            return values[0][0, -1], {name.replace('present.', 'past_key_values.'): value
                                      for name, value in zip(NAMES[1:], values[1:])}, values
        except BaseException:
            values = None
            raise
        finally:
            feed.clear()

    model = tokenizer = logits = values = None
    past, stored = {}, []
    try:
        if args.mode == 'equivalence':
            require(len(cases[0]['ids']) >= 64, 'Equivalence requires at least 64 input tokens')
            for filename in ('model_q4.onnx', 'model_q4_last.onnx'):
                try:
                    model, past, length, results = create_session(filename), empty(), 0, []
                    for ids in (cases[0]['ids'][:64], [123]):
                        logits, past, values = step(model, ids, past, length)
                        length += len(ids)
                        results.append([logits.copy()] + [past[name].copy() for name in past])
                        values = logits = None
                    stored.append(results)
                finally:
                    past.clear()
                    values = logits = model = None
                    gc.collect()
            report['comparisons'] = []
            for index in range(2):
                delta = [float(np.max(np.abs(a - b))) for a, b in zip(stored[0][index], stored[1][index])]
                report['comparisons'].append({'step': index, 'logitsMaxAbs': delta[0], 'kvMaxAbs': max(delta[1:])})
                save()
                require(delta[0] <= 0.0001 and max(delta[1:]) == 0, 'Gather transformation differs from original')
            report['passed'] = True
            return

        tokenizer = AutoTokenizer.from_pretrained(str(original), local_files_only=True, trust_remote_code=False)
        assets = read_json(original / 'tokenizer.json')
        defined = set(assets['model']['vocab'].values()) | {item['id'] for item in assets['added_tokens']}
        special = {item['id'] for item in assets['added_tokens'] if item['special']}
        valid = np.asarray([i in defined and (i not in special or i in (151643, 151645)) for i in range(151936)])
        model = create_session('model_q4_last.onnx')
        words = ('follow', 'investigate', 'wait', 'warn', 'answer', 'refuse')
        first = {word: tokenizer.encode(word, add_special_tokens=False)[0] for word in words}
        for case in cases:
            row = {key: value for key, value in case.items() if key not in ('body', 'officialText', 'ids')}
            row.update(inputTokens=len(case['ids']), tokens=[], raw='', eos=False,
                       strictJson=False, semanticCorrect=False, stopReason='error')
            report['cases'].append(row)
            started, past, generated, eos = time.perf_counter(), empty(), [], False
            try:
                if args.mode == 'intent':
                    ids = case['ids'] + tokenizer.encode('{"intent":"', add_special_tokens=False)
                    require(len(ids) <= 1536, 'Teacher-forced prefix exceeds input budget')
                    logits, past, values = step(model, ids, past, 0)
                    if time.perf_counter() - started >= 20:
                        raise TimeoutError('Native returned after logical budget')
                    row['wordFirstScores'] = {word: float(logits[token]) for word, token in first.items()}
                    top = np.argsort(logits)[-20:][::-1]
                    row['top20'] = [{'id': int(token), 'text': tokenizer.decode([int(token)], skip_special_tokens=False),
                                     'score': float(logits[token])} for token in top]
                    row['bestWordFirstScore'] = max(words, key=lambda word: logits[first[word]])
                    row['stopReason'] = 'teacher_forced_complete'
                else:
                    ids, consumed = case['ids'], 0
                    row['stopReason'] = 'token_budget'
                    for _ in range(256):
                        if time.perf_counter() - started >= 20:
                            raise TimeoutError('20-second logical budget')
                        logits, past, values = step(model, ids, past, consumed)
                        consumed += len(ids)
                        if time.perf_counter() - started >= 20:
                            raise TimeoutError('Native returned after logical budget')
                        token = int(np.argmax(np.where(valid, logits, -np.inf)))
                        generated.append(token)
                        if token in (151643, 151645):
                            eos, row['stopReason'] = True, 'eos'
                            break
                        ids = [token]
                    raw = tokenizer.decode(generated[:-1] if eos else generated, skip_special_tokens=False,
                                           clean_up_tokenization_spaces=False)
                    okay, parsed = validate(raw)
                    if time.perf_counter() - started >= 20:
                        raise TimeoutError('Request finished after logical budget')
                    row.update(raw=raw, strictJson=eos and okay)
                    if okay:
                        row['actual'] = parsed['intent']
                        row['semanticCorrect'] = (eos and parsed['intent'] == case['expected'].lower()
                                                  and (case.get('target') is None or parsed.get('targetName', parsed.get('dangerTarget')) == case['target']))
            except Exception as error:
                row.update(error_data(error))
                row['stopReason'] = 'timeout' if type(error) is TimeoutError else 'error'
                row['strictJson'] = row['semanticCorrect'] = False
            finally:
                # Partial output is diagnostic, never a completion repair. Exact IDs
                # survive even if tokenizer decoding replaces unfinished UTF-8.
                row['tokens'], row['eos'] = list(generated), eos
                if args.mode == 'raw':
                    try:
                        row['raw'] = tokenizer.decode(generated[:-1] if eos else generated, skip_special_tokens=False,
                                                      clean_up_tokenization_spaces=False)
                    except Exception as decode_error:
                        row['partialDecodeError'] = error_data(decode_error)
                    row['rawComplete'] = eos and row['stopReason'] == 'eos'
                    row['rawDecoding'] = 'Exact IDs retained; tokenizer decoding may replace unfinished UTF-8 on partial output.'
                past.clear()
                values = logits = None
                row['seconds'] = time.perf_counter() - started
                save()
                print(json.dumps({key: value for key, value in row.items()
                                  if key not in ('tokens', 'top20', 'wordFirstScores')}, ensure_ascii=True), flush=True)
                time.sleep(.3)
        if args.mode == 'raw':
            report['strictJson'] = sum(row.get('strictJson', False) for row in report['cases'])
            report['semanticCorrect'] = sum(row.get('semanticCorrect', False) for row in report['cases'])
    finally:
        past.clear()
        values = logits = model = tokenizer = None
        stored.clear()
        gc.collect()
        report['nativeReferenceCleanupThread'] = threading.get_ident()
        report['nativeReferenceCleanupScope'] = 'Python references cleared on Owner; no native leak or allocator-release claim.'


def owner_run(args, root, output, errors):
    report = {'scope': 'isolated candidate numerical/raw generation diagnosis only; not Unity, T01, safety or performance acceptance',
              'mode': args.mode, 'mainThread': threading.main_thread().ident, 'ownerThread': threading.get_ident(),
              'pid': os.getpid(), 'threads': 2, 'cases': [], 'completed': False, 'bindingVersion': BINDING_VERSION,
              'bindingPolicy': BINDING_POLICY, 'bindingsStable': False}
    def save():
        save_report(output, report)
    save()
    try:
        report['bindingsStart'] = capture_bindings(root)
        save()
        if args.mode != 'equivalence':
            equivalence_path = output.parent / f'{args.equivalence_tag}.json'
            validate_equivalence_report(read_json(equivalence_path), report['bindingsStart'])
            report['equivalenceTag'] = args.equivalence_tag
            report['equivalenceReportSha256'] = sha256_file(equivalence_path)
        native_diagnostics(args, root, report, save)
        report['bindingsEnd'] = capture_bindings(root)
        report['bindingsStable'] = report['bindingsStart'] == report['bindingsEnd']
        require(report['bindingsStable'], 'Input/model files changed across the diagnostic run')
        report['completed'] = True
        save()
        print(json.dumps({key: value for key, value in report.items() if key != 'cases'}, ensure_ascii=True), flush=True)
    except BaseException as error:
        data = error_data(error)
        report.update(data, completed=False, passed=False)
        errors.append(dict(data))
        try:
            report['bindingsEnd'] = capture_bindings(root)
            report['bindingsStable'] = report.get('bindingsStart') == report['bindingsEnd']
        except BaseException as binding_error:
            report['endBindingError'] = error_data(binding_error)
        save()
    finally:
        # Tracebacks are detached before the final Owner collection; main receives
        # JSON-compatible error strings, never native objects or exception frames.
        gc.collect()
        report['ownerFinalCollectionThread'] = threading.get_ident()
        save()


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mode', choices=('equivalence', 'intent', 'raw'), required=True)
    parser.add_argument('--tag', required=True)
    parser.add_argument('--equivalence-tag', help='Explicit bound equivalence report required for intent/raw')
    args = parser.parse_args(argv)
    for value in (args.tag, args.equivalence_tag):
        if value is not None and not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9_-]{0,95}', value):
            parser.error('Evidence tags must be simple names, not paths')
    if args.mode != 'equivalence' and not args.equivalence_tag:
        parser.error('--equivalence-tag is required for intent/raw; legacy Equivalence_A is not a gate')
    if args.mode == 'equivalence' and args.equivalence_tag:
        parser.error('--equivalence-tag is only used for intent/raw')
    output = ROOT / f'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3/{args.tag}.json'
    if output.exists():
        parser.error('Fresh evidence tag required; existing evidence will not be overwritten')
    errors = []
    def checked_owner():
        try:
            owner_run(args, ROOT, output, errors)
        except BaseException as error:
            # Includes failure to write the first/last report: an unhandled
            # Python thread exception must not make this command exit zero.
            errors.append(error_data(error))
        finally:
            gc.collect()
    worker = threading.Thread(target=checked_owner, name='S3-Qwen3-Candidate-Owner')
    worker.start()
    worker.join()
    if errors:
        print(json.dumps({'success': False, 'errors': errors}, ensure_ascii=True), flush=True)
        return 1
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
