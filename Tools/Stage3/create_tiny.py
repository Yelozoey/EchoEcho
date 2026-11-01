from pathlib import Path
from onnx import helper,TensorProto,save
p=Path(__file__).resolve().parents[2]/'Docs/Tests/Stage3/Evidence/S3-G01/TinyAdd.onnx'
graph=helper.make_graph([helper.make_node('Add',['x','y'],['z'])],'BackgroundArithmetic',[helper.make_tensor_value_info('x',TensorProto.FLOAT,[2])],[helper.make_tensor_value_info('z',TensorProto.FLOAT,[2])],[helper.make_tensor('y',TensorProto.FLOAT,[2],[2.,3.])])
model=helper.make_model(graph,opset_imports=[helper.make_opsetid('',13)]);model.ir_version=8;save(model,str(p));print(p)
