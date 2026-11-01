"""Tiny native CPU tests only; no real weights, downloads, Unity or model installation."""
import copy
import json
from pathlib import Path
import tempfile
import threading
import unittest
import subprocess
import sys

import numpy as np
import onnx
from onnx import TensorProto, helper, numpy_helper
import onnxruntime as ort

import quantize_qwen15_candidate as conversion

QUANT_MAX_ABS = 0.15
QUANT_MIN_COSINE = 0.98
ARENA_MEMORY_LIMIT = 128 << 20


def memory_regression_child():
    """A six-by32MiB synthetic weight regression, isolated from test-runner RAM."""
    import ctypes as c
    from ctypes import wintypes as w
    import gc
    from onnxruntime.quantization.matmul_nbits_quantizer import DefaultWeightOnlyQuantizer, DefaultWeightOnlyQuantConfig
    from google.protobuf.internal import api_implementation
    class Counters(c.Structure):
        _fields_ = [('cb', w.DWORD), ('faults', w.DWORD)] + [(name, c.c_size_t) for name in
            ('peakws', 'ws', 'peakpaged', 'paged', 'peaknonpaged', 'nonpaged', 'pagefile', 'peakpagefile', 'private')]
    get = c.windll.psapi.GetProcessMemoryInfo
    get.argtypes = [w.HANDLE, c.POINTER(Counters), w.DWORD]
    get.restype = w.BOOL
    current = c.windll.kernel32.GetCurrentProcess
    current.restype = w.HANDLE
    def private():
        counters = Counters()
        counters.cb = c.sizeof(counters)
        if not get(current(), c.byref(counters), counters.cb):
            raise c.WinError()
        return counters.private
    payload = b'\0\0\0=' * (8 << 20)  # exactly32MiB of FP32 .03125
    control = onnx.ModelProto()
    tensor = control.graph.initializer.add(name='old-long-lived-arena', data_type=TensorProto.FLOAT, dims=[8388608])
    baseline = private()
    for _ in range(6):
        tensor.raw_data = payload
        tensor.ClearField('raw_data')
        gc.collect()
    old_growth = private() - baseline
    del tensor, control
    gc.collect()
    rows, errors = [], []
    root = Path(__file__).resolve().parents[2] / 'Library/EchoStation'
    with tempfile.TemporaryDirectory(prefix='qwen15-arena-', dir=root) as directory:
        owned = Path(directory).resolve()
        if not owned.is_relative_to(root.resolve()):
            raise ValueError('Synthetic memory fixture path escaped its root')
        (owned / 'tiny-weights.bin').write_bytes(payload)
        def worker():
            try:
                destination = onnx.GraphProto()
                quantizer = DefaultWeightOnlyQuantizer(DefaultWeightOnlyQuantConfig(
                    bits=4, block_size=64, is_symmetric=True, accuracy_level=4))
                start = private()
                for index in range(6):
                    name = 'synthetic.weight.' + str(index)
                    descriptor = onnx.TensorProto(name=name, data_type=TensorProto.FLOAT, dims=[4096, 2048],
                                                  data_location=TensorProto.EXTERNAL)
                    for key, value in (('location', 'tiny-weights.bin'), ('offset', '0'), ('length', str(len(payload)))):
                        descriptor.external_data.add(key=key, value=value)
                    node = helper.make_node('MatMul', ['unused.input', name], ['unused.output'], name='synthetic/' + str(index))
                    conversion.quantize_target_isolated(node, descriptor, destination, quantizer, external_base=owned)
                    gc.collect()
                    rows.append({'iteration': index, 'privateDelta': private() - start,
                                 'destinationInitializers': len(destination.initializer)})
                    if any(t.data_type == TensorProto.FLOAT and list(t.dims) == [4096, 2048]
                           for t in destination.initializer):
                        raise ValueError('A source float matrix escaped into the final graph')
            except BaseException as error:
                errors.append(str(error))
        owner = threading.Thread(target=worker, name='Echo-Arena-Regression-Owner')
        owner.start()
        owner.join(60)
        if owner.is_alive() or errors:
            raise AssertionError('Synthetic arena fixture failed: ' + str(errors))
    result = {'syntheticOnly': True, 'protobuf': api_implementation.Type(), 'sourceBytesPerIteration': len(payload),
              'iterations': 6, 'oldArenaGrowth': old_growth, 'isolatedRows': rows,
              'predeclaredPrivateGrowthLimit': ARENA_MEMORY_LIMIT}
    if old_growth < (160 << 20) or max(row['privateDelta'] for row in rows) > ARENA_MEMORY_LIMIT:
        raise AssertionError(json.dumps(result))
    print(json.dumps(result), flush=True)


def toy():
    rng = np.random.default_rng(261026)
    embedding = rng.integers(-7, 8, (256, 64)).astype(np.float32) / 32
    embedding[:, 0] = -8 / 32
    core = rng.integers(-7, 8, (64, 64)).astype(np.float32) / 32
    core[0, :] = -8 / 32
    tensors = [numpy_helper.from_array(embedding, conversion.EMBED_NAME),
               numpy_helper.from_array(core, 'core.weight'),
               numpy_helper.from_array(np.array([1, -1, 2, 32], np.int64), 'kv.shape'),
               numpy_helper.from_array(np.array([0], np.float32), 'retained.zero'),
               numpy_helper.from_array(np.array([0], np.int64), 'unsqueeze.axes')]
    nodes = [helper.make_node('Gather', [conversion.EMBED_NAME, 'input_ids'], ['hidden'], name='embedding/Gather', axis=0),
             helper.make_node('MatMul', ['hidden', 'core.weight'], ['core.projected'], name='core/MatMul'),
             helper.make_node('ReduceMean', ['hidden'], ['mean'], axes=[2], keepdims=1, name='opset14/ReduceMean'),
             helper.make_node('Unsqueeze', ['retained.zero', 'unsqueeze.axes'], ['broadcast.zero']),
             helper.make_node('Mul', ['mean', 'broadcast.zero'], ['mean.zero']),
             helper.make_node('Add', ['core.projected', 'mean.zero'], ['projected']),
             helper.make_node('Reshape', ['hidden', 'kv.shape'], ['new.kv']),
             helper.make_node('Transpose', ['new.kv'], ['new.kv.t'], perm=[0, 2, 1, 3]),
             helper.make_node('Concat', ['past.key', 'new.kv.t'], ['present.key'], axis=2),
             helper.make_node('Concat', ['past.value', 'new.kv.t'], ['present.value'], axis=2),
             helper.make_node('Transpose', [conversion.EMBED_NAME], ['tied.head'], perm=[1, 0], name='head/Transpose'),
             helper.make_node('MatMul', ['projected', 'tied.head'], ['logits'], name=conversion.HEAD_NAME)]
    inputs = [helper.make_tensor_value_info('input_ids', TensorProto.INT64, [1, 'sequence'])]
    inputs.extend(helper.make_tensor_value_info('past.' + s, TensorProto.FLOAT, [1, 2, 'past', 32]) for s in ('key', 'value'))
    outputs = [helper.make_tensor_value_info('logits', TensorProto.FLOAT, [1, 'sequence', 256])]
    outputs.extend(helper.make_tensor_value_info('present.' + s, TensorProto.FLOAT, [1, 2, 'total', 32]) for s in ('key', 'value'))
    model = helper.make_model(helper.make_graph(nodes, 'tiny-tied-decoder-fixture', inputs, outputs, tensors),
                              opset_imports=[helper.make_opsetid('', 14)])
    model.ir_version = 8
    onnx.checker.check_model(model)
    return model


def session(model):
    options = ort.SessionOptions()
    options.intra_op_num_threads = 2
    options.inter_op_num_threads = 1
    options.add_session_config_entry('session.intra_op.allow_spinning', '0')
    options.execution_mode = ort.ExecutionMode.ORT_SEQUENTIAL
    return ort.InferenceSession(model.SerializeToString(), sess_options=options, providers=['CPUExecutionProvider'])


class Qwen15QuantTests(unittest.TestCase):
    def background(self, operation):
        result, errors = [], []
        main = threading.get_native_id()
        def work():
            try:
                self.assertNotEqual(main, threading.get_native_id())
                result.append(operation())
            except BaseException as error:
                errors.append(error)
        owner = threading.Thread(target=work, name='Echo-Tiny-Quant-Owner')
        owner.start()
        owner.join(45)
        self.assertFalse(owner.is_alive(), 'Tiny CPU leaf did not finish')
        if errors:
            raise errors[0]
        return result[0]

    def compare(self, a, b, *, quantized=False):
        left, right = session(a), session(b)
        lp = {'past.key': np.zeros((1, 2, 0, 32), np.float32), 'past.value': np.zeros((1, 2, 0, 32), np.float32)}
        rp = {k: v.copy() for k, v in lp.items()}
        rows = []
        for ids in ([4, 18, 127], [37]):
            lo = left.run(None, {'input_ids': np.asarray([ids], np.int64), **lp})
            ro = right.run(None, {'input_ids': np.asarray([ids], np.int64), **rp})
            expected = lo[0][:, -1:, :]
            self.assertEqual(ro[0].shape, (1, 1, 256))
            maximum = float(np.max(np.abs(expected - ro[0])))
            cosine = float(np.dot(expected.ravel(), ro[0].ravel()) / (np.linalg.norm(expected) * np.linalg.norm(ro[0])))
            if quantized:
                self.assertLessEqual(maximum, QUANT_MAX_ABS)
                self.assertGreaterEqual(cosine, QUANT_MIN_COSINE)
            else:
                self.assertEqual(maximum, 0.0, 'Dyadic fixture must remain exactly equivalent before quantization')
            for j, key in enumerate(('past.key', 'past.value'), 1):
                self.assertEqual(lo[j].shape, ro[j].shape)
                if not quantized:
                    np.testing.assert_array_equal(lo[j], ro[j])
                lp[key], rp[key] = lo[j], ro[j]
            rows.append({'maxAbs': maximum, 'cosine': cosine, 'kvLength': lo[1].shape[2]})
        return rows

    def test_last_position_and_constant_head_are_exact_before_quantizing(self):
        def run():
            before = toy()
            after = copy.deepcopy(before)
            transform = conversion.prepare_graph(after)
            onnx.checker.check_model(after)
            self.assertTrue(transform['opsetMigrated'])
            mean = next(n for n in after.graph.node if n.name == 'opset14/ReduceMean')
            self.assertEqual(len(mean.input), 2)
            self.assertFalse(any(a.name == 'axes' for a in mean.attribute))
            rows = self.compare(before, after)
            self.assertEqual(transform['vocabulary'], 256)
            self.assertFalse(any(n.name == 'head/Transpose' for n in after.graph.node))
            return rows
        self.background(run)

    def test_official_qoperator_int4_cpu_two_steps_and_pruning(self):
        def run():
            source = toy()
            prepared = copy.deepcopy(source)
            transform = conversion.prepare_graph(prepared)
            result, details = conversion.quantize_graph(prepared, transform, block_size=64, accuracy_level=4)
            onnx.checker.check_model(result)
            self.assertEqual(details['quantizedInventory'], {'MatMulNBits': 2, 'GatherBlockQuantized': 1})
            self.assertTrue(details['kvOutputsUnchanged'])
            self.assertNotIn(conversion.EMBED_NAME, {t.name for t in result.graph.initializer})
            return self.compare(source, result, quantized=True)
        rows = self.background(run)
        print(json.dumps({'fixtureOnly': True, 'actualCpu': True, 'threads': 2, 'steps': rows,
                          'predeclaredMaxAbs': QUANT_MAX_ABS, 'predeclaredMinCosine': QUANT_MIN_COSINE}), flush=True)

    def test_streamed_external_source_matches_in_memory_conversion(self):
        root = Path(__file__).resolve().parents[2] / 'Library/EchoStation'
        root.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(prefix='qwen15-tiny-', dir=root) as directory:
            owned = Path(directory).resolve()
            self.assertTrue(owned.is_relative_to(root.resolve()))
            source_path = owned / 'tiny.onnx'
            onnx.save_model(toy(), source_path, save_as_external_data=True, all_tensors_to_one_file=True,
                            location='tiny.data', size_threshold=0)
            def run():
                source = onnx.load(source_path, load_external_data=False)
                self.assertTrue(all(not t.raw_data for t in source.graph.initializer))
                transform = conversion.prepare_graph(source, lazy_head=True)
                embedding = next(t for t in source.graph.initializer if t.name == conversion.EMBED_NAME)
                self.assertFalse(embedding.raw_data, 'Schema migration must not load external weights')
                self.assertTrue(onnx.external_data_helper.uses_external_data(embedding))
                result, details = conversion.quantize_graph(source, transform, external_base=owned)
                onnx.checker.check_model(result)
                reference = toy()
                self.compare(reference, result, quantized=True)
                self.assertFalse(any(onnx.external_data_helper.uses_external_data(t) for t in result.graph.initializer))
                retained = next(t for t in result.graph.initializer if t.name == 'retained.zero')
                np.testing.assert_array_equal(numpy_helper.to_array(retained), np.array([0], np.float32))
                return details
            self.background(run)

    def test_bad_head_consumers_or_repeat_conversion_are_rejected(self):
        model = toy()
        conversion.prepare_graph(model)
        with self.assertRaises(ValueError):
            conversion.prepare_graph(model)
        model = toy()
        model.graph.node.append(helper.make_node('Identity', ['tied.head'], ['unexpected']))
        with self.assertRaises(ValueError):
            conversion.prepare_graph(model)

    def test_blocks_and_accuracy_are_explicit_and_unsupported_options_rejected(self):
        for block, accuracy in ((32, 4), (63, 4), (64, 1), (128, 2)):
            with self.subTest(block=block, accuracy=accuracy):
                model = toy()
                transform = conversion.prepare_graph(model)
                with self.assertRaises(ValueError):
                    conversion.quantize_graph(model, transform, block_size=block, accuracy_level=accuracy)

    def test_isolated_float_arenas_do_not_accumulate_six32mib_sources(self):
        result = subprocess.run([sys.executable, '-X', 'utf8', str(Path(__file__).resolve()), '--memory-child'],
                                capture_output=True, text=True, timeout=90)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        report = json.loads(result.stdout.strip().splitlines()[-1])
        self.assertEqual(report['iterations'], 6)
        self.assertGreaterEqual(report['oldArenaGrowth'], 160 << 20)
        self.assertLessEqual(max(row['privateDelta'] for row in report['isolatedRows']), ARENA_MEMORY_LIMIT)
        print(json.dumps(report), flush=True)


if __name__ == '__main__':
    if sys.argv[1:] == ['--memory-child']:
        memory_regression_child()
    else:
        unittest.main(verbosity=2)
