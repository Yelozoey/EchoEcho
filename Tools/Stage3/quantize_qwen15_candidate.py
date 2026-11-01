"""Isolated Qwen2.5-1.5B conversion; never downloads or installs a model.

Only the official ORT DEFAULT/RTN QOperator quantizer creates quantized weights.
External FP32 tensors are loaded per target, not as one 6.2GB protobuf. The
original tied embedding is independently quantized for Gather and transposed
MatMul layouts. Required position caches remain unchanged. This tool does not
assert semantic quality, Unity compatibility, runtime memory or performance.
"""
from pathlib import Path
import argparse
import hashlib
import json
import threading
import time

SOURCE_REVISION = 'd54957e3af1be04546436e874a60fe386041b244'
SOURCE_GRAPH_BYTES = 1057843
SOURCE_GRAPH_SHA = '8f7da5d0bb5e5b6eba0ba0e9c006fcbc8b670134405ef5e02aaaf738361e2074'
SOURCE_DATA_BYTES = 6208411648
SOURCE_DATA_SHA = '7111acf0e546fe192035357890402832315b4c243e56b01b47ce9358285fc82e'
HEAD_NAME = '/lm_head/MatMul'
EMBED_NAME = 'model.embed_tokens.weight'
LAST_INDEX = 'echo_qwen15_last_index'
LAST_HIDDEN = 'echo_qwen15_last_hidden'
HEAD_WEIGHT = 'echo_qwen15_head_weight_fp32'
MAX_PACKAGE_BYTES = 1 << 30
MAX_RESOURCES_BYTES = 20 << 30


def sha256_file(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def fingerprint(tensor):
    # upb returns a Python bytes object; read this large field only once.
    raw = tensor.raw_data
    return {'type': tensor.data_type, 'shape': list(tensor.dims),
            'sha256': hashlib.sha256(raw if raw else tensor.SerializeToString()).hexdigest()}


def prepare_graph(model, *, lazy_head=False, strict_candidate=False):
    """Official schema migration, then head edits; lazy mode defers its933MB copy."""
    import onnx
    from onnx import helper, numpy_helper
    default_versions = [o.version for o in model.opset_import if o.domain in ('', 'ai.onnx')]
    if len(default_versions) != 1 or default_versions[0] not in (14, 21):
        raise ValueError('Only reviewed source opset14 or already migrated21 is supported')
    source_opset = default_versions[0]
    if source_opset != 21:
        # Run before materializing the huge lazy head. Simply changing the version
        # number leaves opset14 ReduceMean.axes attributes invalid in opset21.
        migrated = onnx.version_converter.convert_version(model, 21)
        model.CopyFrom(migrated)
    initializers = {t.name: t for t in model.graph.initializer}
    heads = [n for n in model.graph.node if n.name == HEAD_NAME]
    if len(heads) != 1 or heads[0].op_type != 'MatMul' or list(heads[0].output) != ['logits']:
        raise ValueError('Exactly the reviewed MatMul logits head is required')
    head = heads[0]
    transpose = [n for n in model.graph.node if head.input[1] in n.output]
    if len(transpose) != 1 or transpose[0].op_type != 'Transpose':
        raise ValueError('Expected the tied embedding transpose before head')
    transpose = transpose[0]
    permutation = next((list(helper.get_attribute_value(a)) for a in transpose.attribute if a.name == 'perm'), [1, 0])
    if permutation != [1, 0] or len(transpose.input) != 1:
        raise ValueError('Only the reviewed 2D head transpose is supported')
    source = initializers.get(transpose.input[0])
    if source is None or source.data_type != onnx.TensorProto.FLOAT or len(source.dims) != 2:
        raise ValueError('An FP32 tied embedding initializer is required')
    if strict_candidate and (source.name != EMBED_NAME or list(source.dims) != [151936, 1536]):
        raise ValueError('Candidate embedding shape or identity differs')
    embedding_gathers = [n for n in model.graph.node if n.op_type == 'Gather' and n.input[0] == source.name]
    if len(embedding_gathers) != 1 or not embedding_gathers[0].name:
        raise ValueError('Exactly one named embedding Gather is required')
    transpose_users = [n for n in model.graph.node if transpose.output[0] in n.input]
    if len(transpose_users) != 1 or transpose_users[0].name != HEAD_NAME:
        raise ValueError('Transpose has unreviewed additional consumers')
    reserved = {t.name for t in model.graph.initializer} | {s for n in model.graph.node for s in list(n.input) + list(n.output)}
    if reserved & {HEAD_WEIGHT, LAST_INDEX, LAST_HIDDEN}:
        raise ValueError('Transformation names already exist; refusing repeated conversion')
    output = next((o for o in model.graph.output if o.name == 'logits'), None)
    if output is None or len(output.type.tensor_type.shape.dim) != 3:
        raise ValueError('Expected batch/sequence/vocabulary logits')
    kv_outputs = {o.name: o.SerializeToString().hex() for o in model.graph.output if o.name != 'logits'}
    if lazy_head:
        new_weight = onnx.TensorProto(name=HEAD_WEIGHT, data_type=onnx.TensorProto.FLOAT,
                                      dims=[source.dims[1], source.dims[0]])
    else:
        new_weight = numpy_helper.from_array(numpy_helper.to_array(source).T, HEAD_WEIGHT)
    model.graph.initializer.extend([new_weight, helper.make_tensor(LAST_INDEX, onnx.TensorProto.INT64, [1], [-1])])
    old_hidden = head.input[0]
    rewritten = []
    for node in model.graph.node:
        if node.name == transpose.name:
            continue
        if node.name == HEAD_NAME:
            rewritten.append(helper.make_node('Gather', [old_hidden, LAST_INDEX], [LAST_HIDDEN],
                                              axis=1, name='EchoQwen15LastHiddenOnly'))
            node.input[0] = LAST_HIDDEN
            node.input[1] = HEAD_WEIGHT
        rewritten.append(node)
    model.graph.ClearField('node')
    model.graph.node.extend(rewritten)
    output.type.tensor_type.shape.dim[1].ClearField('dim_param')
    output.type.tensor_type.shape.dim[1].dim_value = 1
    return {'lastPositionGather': True, 'constantHead': True,
            'sourceOpset': source_opset, 'targetOpset': 21, 'opsetMigrated': source_opset != 21,
            'headName': HEAD_NAME, 'embeddingName': source.name,
            'embeddingGather': embedding_gathers[0].name, 'headWeight': HEAD_WEIGHT,
            'originalHidden': old_hidden, 'lazyHead': lazy_head, 'kvOutputs': kv_outputs,
            'vocabulary': int(source.dims[0]), 'hiddenSize': int(source.dims[1]),
            'change': 'last hidden Gather; explicit tied-head transpose constant; no KV or decoder edits'}


def materialize_tensor(descriptor, external_base):
    """Own the float data in a fresh arena that can actually be destroyed."""
    import onnx
    from onnx import external_data_helper
    tensor = onnx.TensorProto()
    tensor.CopyFrom(descriptor)
    if external_data_helper.uses_external_data(tensor):
        if external_base is None:
            raise ValueError('External weights need an explicit reviewed base directory')
        external_data_helper.load_external_data_for_tensor(tensor, str(external_base))
        tensor.data_location = onnx.TensorProto.DEFAULT
        tensor.ClearField('external_data')
    typed = any(len(getattr(tensor, field)) for field in
                ('float_data', 'int32_data', 'int64_data', 'double_data', 'uint64_data', 'string_data'))
    if not tensor.HasField('raw_data') and not typed:
        raise ValueError('Missing data for conversion tensor: ' + descriptor.name)
    return tensor


def quantize_target_isolated(node, descriptor, destination, official_quantizer, *,
                             external_base=None, transposed_embedding=None):
    """Discard an entire temporary protobuf arena after each official conversion.

    Clearing bytes inside the long-lived ModelProto does NOT release upb arena
    allocations. No original float raw_data is ever assigned to destination.
    """
    import onnx
    from onnx import numpy_helper
    workspace = onnx.GraphProto()
    if transposed_embedding is not None:
        embedding = materialize_tensor(transposed_embedding, external_base)
        value = numpy_helper.from_array(numpy_helper.to_array(embedding).T, descriptor.name)
        if value.data_type != descriptor.data_type or list(value.dims) != list(descriptor.dims):
            raise ValueError('Materialized head transpose differs from reviewed descriptor')
        workspace.initializer.append(value)
        del value, embedding
    else:
        value = materialize_tensor(descriptor, external_base)
        workspace.initializer.append(value)
        del value
    source = workspace.initializer[0]
    before = fingerprint(source)
    generated = official_quantizer.quantize(node, [workspace])
    expected = 'MatMulNBits' if node.op_type == 'MatMul' else 'GatherBlockQuantized'
    if len(generated) != 1 or generated[0].op_type != expected:
        raise ValueError('Official quantizer skipped a required target: ' + node.name)
    known_names = {t.name for t in destination.initializer}
    for tensor in workspace.initializer:
        if tensor.name == descriptor.name:
            continue
        if tensor.name in known_names:
            raise ValueError('Quantized initializer name collision: ' + tensor.name)
        destination.initializer.append(tensor)
        known_names.add(tensor.name)
    result = []
    for generated_node in generated:
        clone = onnx.NodeProto()
        clone.CopyFrom(generated_node)
        result.append(clone)
    # Only cloned quantized nodes, small hashes and metadata escape this frame.
    # source/workspace/official temporaries and their float arena die together.
    return result, before


def quantize_graph(model, transform, *, external_base=None, block_size=64, accuracy_level=4,
                   strict_candidate=False):
    """Official QOperator conversion using a fresh float arena per target."""
    if block_size not in (64, 128, 256) or accuracy_level not in (0, 4):
        raise ValueError('Only reviewed block64/128/256 and accuracy0/4 are allowed')
    import onnx
    from onnxruntime.quantization.matmul_nbits_quantizer import MatMulNBitsQuantizer, DefaultWeightOnlyQuantConfig
    from onnxruntime.quantization.quant_utils import QuantFormat
    initializers = {t.name: t for t in model.graph.initializer}

    targets = []
    original_nodes = list(model.graph.node)
    for node in original_nodes:
        if node.op_type == 'MatMul' and node.input[1] in initializers:
            targets.append(node)
        elif node.name == transform['embeddingGather']:
            targets.append(node)
    if len({n.name for n in targets}) != len(targets) or any(not n.name for n in targets):
        raise ValueError('Quantized target names must be unique and nonempty')
    matmul_count = sum(n.op_type == 'MatMul' for n in targets)
    if strict_candidate and (matmul_count != 197 or len(targets) != 198):
        raise ValueError('Reviewed candidate requires 197 MatMul plus one embedding Gather')
    target_names = {n.name for n in targets}
    quantized_source_names = {n.input[1] if n.op_type == 'MatMul' else n.input[0] for n in targets}
    protected = {name: initializers[name] for name in initializers.keys() - quantized_source_names}
    config = DefaultWeightOnlyQuantConfig(bits=4, block_size=block_size, is_symmetric=True,
        accuracy_level=accuracy_level, quant_format=QuantFormat.QOperator,
        op_types_to_quantize=('MatMul', 'Gather'), quant_axes=(('MatMul', 0), ('Gather', 1)))
    converter = MatMulNBitsQuantizer(model, algo_config=config)
    converter.model.set_opset_import('com.microsoft', 1)
    converter.model.set_opset_import('', 21)
    model.ir_version = max(10, model.ir_version)
    rewritten = []
    converted = []
    for node in original_nodes:
        if node.name not in target_names:
            rewritten.append(node)
            continue
        source_name = node.input[1] if node.op_type == 'MatMul' else node.input[0]
        transpose_source = initializers[transform['embeddingName']] if source_name == HEAD_WEIGHT and transform['lazyHead'] else None
        new_nodes, before = quantize_target_isolated(node, initializers[source_name], model.graph,
            converter.node_quantizer, external_base=external_base, transposed_embedding=transpose_source)
        expected = 'MatMulNBits' if node.op_type == 'MatMul' else 'GatherBlockQuantized'
        if len(new_nodes) != 1 or new_nodes[0].op_type != expected:
            raise ValueError('Official quantizer skipped a required target: ' + node.name)
        rewritten.extend(new_nodes)
        converted.append({'sourceNode': node.name, 'sourceInitializer': source_name,
                          'sourceTensor': before, 'op': expected, 'node': new_nodes[0].name})
    model.graph.ClearField('node')
    model.graph.node.extend(rewritten)
    converter.model.clean_initializers()
    remaining = {t.name: t for t in model.graph.initializer}
    for name, tensor in remaining.items():
        if name in protected:
            # Required nonquantized weights total only about34MiB. Load/copy once
            # after pruning; these are intentionally retained in the final model.
            value = materialize_tensor(protected[name], external_base)
            expected_fingerprint = fingerprint(value)
            tensor.CopyFrom(value)
            del value
            if fingerprint(tensor) != expected_fingerprint:
                raise ValueError('A required nonquantized initializer changed: ' + name)
        if tensor.data_type in (onnx.TensorProto.FLOAT, onnx.TensorProto.FLOAT16) and list(tensor.dims) in (
                [transform['vocabulary'], transform['hiddenSize']],
                [transform['hiddenSize'], transform['vocabulary']]):
            raise ValueError('Large floating embedding/head survived pruning')
    if any(name in remaining for name in quantized_source_names):
        raise ValueError('A quantized floating source initializer remains referenced')
    after_kv = {o.name: o.SerializeToString().hex() for o in model.graph.output if o.name != 'logits'}
    if after_kv != transform['kvOutputs']:
        raise ValueError('KV output contract changed')
    inventory = {'MatMulNBits': sum(n.op_type == 'MatMulNBits' for n in model.graph.node),
                 'GatherBlockQuantized': sum(n.op_type == 'GatherBlockQuantized' for n in model.graph.node)}
    if inventory != {'MatMulNBits': matmul_count, 'GatherBlockQuantized': 1}:
        raise ValueError('Final quantized node inventory differs')
    return model, {'bits': 4, 'blockSize': block_size, 'isSymmetric': True, 'accuracyLevel': accuracy_level,
                   'format': 'QOperator', 'algorithm': 'ORT DEFAULT/RTN',
                   'quantizedInventory': inventory, 'converted': converted,
                   'preservedInitializers': len(remaining.keys() & protected.keys()),
                   'floatArenaPolicy': 'fresh isolated GraphProto per quantized target; only quantized tensors copied to final graph',
                   'removedFloatingInitializers': sorted(quantized_source_names), 'kvOutputsUnchanged': True}


def resource_bytes(root):
    receipt = json.loads((root / 'Docs/Tests/Stage3/Evidence/S3-I03/ResourceBudget.json').read_text(encoding='utf-8'))
    paths = [(root / part['path']).resolve() for part in receipt['parts']]
    test_sources = (root / 'Assets/EchoStation/Tests/AIEditor').resolve()
    if test_sources not in paths:
        paths.append(test_sources)
    if any(not path.is_relative_to(root) for path in paths):
        raise ValueError('Resource inventory path escapes project')
    return sum(p.stat().st_size for path in paths for p in path.rglob('*') if p.is_file())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', required=True)
    parser.add_argument('--output', required=True)
    parser.add_argument('--evidence', required=True)
    parser.add_argument('--block-size', type=int, choices=(64, 128, 256), default=64)
    parser.add_argument('--accuracy-level', type=int, choices=(0, 4), default=4)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    source, output, evidence = [(root / value).resolve() for value in (args.source, args.output, args.evidence)]
    cache = root / 'Library/EchoStation/S3Cache'
    if not source.is_relative_to(cache) or not output.is_relative_to(cache) or not evidence.is_relative_to(root / 'Docs/Tests'):
        raise ValueError('Conversion paths are restricted to candidate cache and test evidence')
    partial = output.with_name(output.name + '.partial')
    if output.exists() or partial.exists() or evidence.exists() or source == output:
        raise FileExistsError('Preserve existing outputs and evidence')
    external = source.with_name('model.onnx_data')
    if source.stat().st_size != SOURCE_GRAPH_BYTES or sha256_file(source) != SOURCE_GRAPH_SHA:
        raise ValueError('Source graph differs from pinned descriptor')
    if external.stat().st_size != SOURCE_DATA_BYTES or sha256_file(external) != SOURCE_DATA_SHA:
        raise ValueError('Source external data differs from pinned weights')
    before_budget = resource_bytes(root)
    if before_budget + MAX_PACKAGE_BYTES + (64 << 20) > MAX_RESOURCES_BYTES:
        raise ValueError('Insufficient 20GiB resource budget for one output and evidence reserve')
    report = {'schemaVersion': 1, 'scope': 'isolated conversion only; not model/Unity/runtime acceptance',
              'sourceGraph': {'path': str(source.relative_to(root)), 'bytes': SOURCE_GRAPH_BYTES, 'sha256': SOURCE_GRAPH_SHA},
              'sourceExternal': {'path': str(external.relative_to(root)), 'bytes': SOURCE_DATA_BYTES, 'sha256': SOURCE_DATA_SHA},
              'sourceRevision': SOURCE_REVISION, 'mainThread': threading.get_native_id(), 'completed': False,
              'resourceBytesBefore': before_budget}
    started = time.monotonic()
    failure = []

    def worker():
        report['ownerThread'] = threading.get_native_id()
        try:
            import onnx
            import onnxruntime
            report['toolVersions'] = {'onnx': onnx.__version__, 'onnxruntime': onnxruntime.__version__}
            model = onnx.load(str(source), load_external_data=False)
            transform = prepare_graph(model, lazy_head=True, strict_candidate=True)
            model, details = quantize_graph(model, transform, external_base=source.parent,
                block_size=args.block_size, accuracy_level=args.accuracy_level, strict_candidate=True)
            report['transform'] = transform
            report['config'] = {k: v for k, v in details.items() if k not in ('converted',)}
            report['quantizedInventory'] = details['quantizedInventory']
            report['quantizedNodes'] = details['converted']
            onnx.checker.check_model(model)
            output.parent.mkdir(parents=True, exist_ok=True)
            # One output only. No FP32 intermediate or external-data duplicate is saved.
            with partial.open('xb') as stream:
                stream.write(model.SerializeToString())
            package_reserve = 10 << 20
            if partial.stat().st_size + package_reserve > MAX_PACKAGE_BYTES:
                raise ValueError('Actual model plus10MiB tokenizer/metadata reserve exceeds1GiB')
            if sha256_file(source) != SOURCE_GRAPH_SHA or sha256_file(external) != SOURCE_DATA_SHA:
                raise ValueError('Source binding changed during conversion')
            if resource_bytes(root) > MAX_RESOURCES_BYTES:
                raise ValueError('Actual resource budget exceeded20GiB')
            partial.rename(output)
            report['output'] = {'path': str(output.relative_to(root)), 'bytes': output.stat().st_size,
                                'sha256': sha256_file(output), 'packageReserveBytes': package_reserve}
            report['resourceBytesAfter'] = resource_bytes(root)
            report['completed'] = True
        except BaseException as error:
            report['error'] = str(error)
            report['errorType'] = type(error).__name__
            failure.append(str(error))
        finally:
            report['seconds'] = time.monotonic() - started
            try:
                evidence.parent.mkdir(parents=True, exist_ok=True)
                with evidence.open('xb') as stream:
                    stream.write(json.dumps(report, ensure_ascii=False, indent=2).encode('utf-8'))
            except Exception as error:
                report['completed'] = False
                report['error'] = 'Evidence write failed: ' + str(error)
                failure.append(report['error'])

    owner = threading.Thread(target=worker, name='Echo-Qwen15-Conversion', daemon=False)
    owner.start()
    owner.join()
    print(json.dumps({'completed': report['completed'], 'evidence': str(evidence),
                      'output': report.get('output'), 'error': report.get('error')}, ensure_ascii=False), flush=True)
    if failure:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
