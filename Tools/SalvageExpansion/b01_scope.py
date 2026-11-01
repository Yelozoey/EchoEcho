"""Temporary existing pipeline bindings, with exact native/disk/dirty restoration."""
import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/MCP'))
from client import Client
from preflight import payload, require_idle

parser = argparse.ArgumentParser()
parser.add_argument('action', choices=['bind', 'restore'])
args = parser.parse_args()
folder = ROOT / 'Docs/Tests/SalvagePrototype/Evidence/Expansion/B01'
client = Client()
state = json.loads(client.request('resources/read', {'uri': 'mcpforunity://editor/state'})['contents'][0]['text'])['data']
require_idle(client, state)
prefix = r'''
EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
const string folder="Docs/Tests/SalvagePrototype/Evidence/Expansion/B01";
var baseline=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(folder+"/NativeBefore_A.json"));
var temporary=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/EchoStation/Rendering/Settings/ES_Gameplay_ForwardHDR.asset");
if(!temporary||UnityEngine.QualitySettings.GetQualityLevel()!=1)throw new System.InvalidOperationException("Unexpected pipeline/quality");
'''
if args.action == 'bind':
    code = prefix + r'''
if(System.IO.File.Exists(folder+"/Bindings_A.json"))throw new System.InvalidOperationException("Preserve binding receipt");
foreach(var entry in baseline.Properties()){
 var obj=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(entry.Name).Single();
 if(UnityEditor.EditorJsonUtility.ToJson(obj)!=(string)entry.Value["json"]||UnityEditor.EditorUtility.IsDirty(obj)!=(bool)entry.Value["dirty"]||!System.IO.File.ReadAllBytes(entry.Name).SequenceEqual(System.Convert.FromBase64String((string)entry.Value["disk"])))throw new System.InvalidOperationException("Native/disk/dirty changed before temporary scope: "+entry.Name);
}
UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=temporary;UnityEngine.QualitySettings.renderPipeline=temporary;
var receipt=new {pipeline=UnityEditor.AssetDatabase.GetAssetPath(temporary),quality=UnityEngine.QualitySettings.GetQualityLevel(),savedAssets=false};
System.IO.File.WriteAllText(folder+"/Bindings_A.json",Newtonsoft.Json.JsonConvert.SerializeObject(receipt));return receipt;
'''
else:
    code = prefix + r'''
if(!System.IO.File.Exists(folder+"/Bindings_A.json")||System.IO.File.Exists(folder+"/NativeRestored_A.json"))throw new System.InvalidOperationException("Missing or previously restored temporary scope");
var objects=new System.Collections.Generic.Dictionary<string,UnityEngine.Object>();
string temporaryGuid=UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(temporary));
foreach(var entry in baseline.Properties()){
 var path=entry.Name;var obj=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).Single();objects.Add(path,obj);
 var expected=Newtonsoft.Json.Linq.JObject.Parse((string)entry.Value["json"]);var current=Newtonsoft.Json.Linq.JObject.Parse(UnityEditor.EditorJsonUtility.ToJson(obj));var permitted=(Newtonsoft.Json.Linq.JObject)expected.DeepClone();
 string originalGuid=null;
 if(path.EndsWith("/GraphicsSettings.asset")){originalGuid=(string)expected["GraphicsSettings"]["m_CustomRenderPipeline"]["guid"];var token=current["GraphicsSettings"]["m_CustomRenderPipeline"];if((string)token["guid"]!=originalGuid&&(string)token["guid"]!=temporaryGuid)throw new System.InvalidOperationException("Unknown default binding");permitted["GraphicsSettings"]["m_CustomRenderPipeline"]=token.DeepClone();}
 if(path.EndsWith("/QualitySettings.asset")){originalGuid=(string)expected["QualitySettings"]["m_QualitySettings"][1]["customRenderPipeline"]["guid"];var token=current["QualitySettings"]["m_QualitySettings"][1]["customRenderPipeline"];if((string)token["guid"]!=originalGuid&&(string)token["guid"]!=temporaryGuid)throw new System.InvalidOperationException("Unknown PC binding");permitted["QualitySettings"]["m_QualitySettings"][1]["customRenderPipeline"]=token.DeepClone();}
 if(!Newtonsoft.Json.Linq.JToken.DeepEquals(current,permitted))throw new System.InvalidOperationException("Unexpected native changes: "+path);
 string expectedDisk=System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String((string)entry.Value["disk"]));string disk=System.Text.Encoding.UTF8.GetString(System.IO.File.ReadAllBytes(path));
 if(disk!=expectedDisk&&(originalGuid==null||disk!=expectedDisk.Replace(originalGuid,temporaryGuid)))throw new System.InvalidOperationException("Unexpected disk changes: "+path);
}
foreach(var entry in baseline.Properties())UnityEditor.EditorJsonUtility.FromJsonOverwrite((string)entry.Value["json"],objects[entry.Name]);
var result=new Newtonsoft.Json.Linq.JArray();
foreach(var entry in baseline.Properties()){
 var obj=objects[entry.Name];if(UnityEditor.EditorJsonUtility.ToJson(obj)!=(string)entry.Value["json"])throw new System.InvalidOperationException("Native restore mismatch");
 var disk=System.Convert.FromBase64String((string)entry.Value["disk"]);if(!System.IO.File.ReadAllBytes(entry.Name).SequenceEqual(disk))System.IO.File.WriteAllBytes(entry.Name,disk);
 if((bool)entry.Value["dirty"])UnityEditor.EditorUtility.SetDirty(obj);else UnityEditor.EditorUtility.ClearDirty(obj);
 result.Add(new Newtonsoft.Json.Linq.JObject{{"path",entry.Name},{"nativeExact",UnityEditor.EditorJsonUtility.ToJson(obj)==(string)entry.Value["json"]},{"diskExact",System.IO.File.ReadAllBytes(entry.Name).SequenceEqual(disk)},{"dirtyExact",UnityEditor.EditorUtility.IsDirty(obj)==(bool)entry.Value["dirty"]}});
}
System.IO.File.WriteAllText(folder+"/NativeRestored_A.json",result.ToString());return result;
'''
raw = client.request('tools/call', {'name': 'execute_code', 'arguments': {'action': 'execute', 'compiler': 'codedom', 'code': code}})
result = payload(raw)
(folder / ('Scope_' + args.action + '_A.json')).write_text(json.dumps(raw, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(result, ensure_ascii=False))
if not result.get('success'):
    raise SystemExit('Scope did not report success; inspect receipts before another mutation')
