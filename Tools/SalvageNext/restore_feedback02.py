"""Restore only this task's recognized temporary settings; refuse any unrelated difference."""
import sys,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MCP'))
from client import Client
from preflight import payload,require_idle
c=Client()
state=json.loads(c.request('resources/read',{'uri':'mcpforunity://editor/state'})['contents'][0]['text'])['data']
require_idle(c,state,lambda raw:None)
code=r'''
EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
if(UnityEngine.Application.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.InvalidOperationException("Editor busy");
const string receipt="Docs/Tests/SalvagePrototype/Evidence/Feedback02/P02/NativeRestored_A.json";
if(System.IO.File.Exists(receipt))throw new System.InvalidOperationException("Keep previous restore receipt");
var baseline=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Docs/Tests/SalvagePrototype/Evidence/Feedback02/NativeBefore_A.json"));
const string originalGuid="cd4c7b12c6a97cd42bbf16ac1d47589d",temporaryGuid="b6c5c421bbf36364c9edd3f812aa7c8e";
var objects=new System.Collections.Generic.Dictionary<string,UnityEngine.Object>();var before=new Newtonsoft.Json.Linq.JObject();
foreach(var property in baseline.Properties())
{
 var path=property.Name;var row=(Newtonsoft.Json.Linq.JObject)property.Value;var obj=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).Single();objects.Add(path,obj);
 string json=UnityEditor.EditorJsonUtility.ToJson(obj);var current=Newtonsoft.Json.Linq.JObject.Parse(json);var expected=Newtonsoft.Json.Linq.JObject.Parse((string)row["json"]);var permitted=(Newtonsoft.Json.Linq.JObject)expected.DeepClone();
 if(path.EndsWith("/GraphicsSettings.asset")){var token=current["GraphicsSettings"]["m_CustomRenderPipeline"];string guid=(string)token["guid"];if(guid!=originalGuid&&guid!=temporaryGuid)throw new System.InvalidOperationException("Unknown graphics binding");permitted["GraphicsSettings"]["m_CustomRenderPipeline"]=token.DeepClone();}
 else if(path.EndsWith("/QualitySettings.asset")){var token=current["QualitySettings"]["m_QualitySettings"][1];string guid=(string)token["customRenderPipeline"]["guid"];int aa=(int)token["antiAliasing"];if((guid!=originalGuid&&guid!=temporaryGuid)||(aa!=0&&aa!=4))throw new System.InvalidOperationException("Unknown quality mutation");permitted["QualitySettings"]["m_QualitySettings"][1]["customRenderPipeline"]=token["customRenderPipeline"].DeepClone();permitted["QualitySettings"]["m_QualitySettings"][1]["antiAliasing"]=aa;}
 else if(path.EndsWith("/EditorSettings.asset")){var token=current["EditorSettings"];int flags=(int)token["m_EnterPlayModeOptions"];if(flags!=0&&flags!=1)throw new System.InvalidOperationException("Unknown play options");permitted["EditorSettings"]["m_EnterPlayModeOptions"]=flags;permitted["EditorSettings"]["m_EnterPlayModeOptionsEnabled"]=token["m_EnterPlayModeOptionsEnabled"].DeepClone();}
 if(!Newtonsoft.Json.Linq.JToken.DeepEquals(current,permitted))throw new System.InvalidOperationException("Unrecognized native changes: "+path);
 byte[] original=System.Convert.FromBase64String((string)row["disk"]);string disk=System.Text.Encoding.UTF8.GetString(System.IO.File.ReadAllBytes(path));var variants=new System.Collections.Generic.HashSet<string>{System.Text.Encoding.UTF8.GetString(original)};
 if(path.EndsWith("/GraphicsSettings.asset")||path.EndsWith("/QualitySettings.asset"))foreach(var v in variants.ToArray())variants.Add(v.Replace(originalGuid,temporaryGuid));
 if(path.EndsWith("/QualitySettings.asset"))foreach(var v in variants.ToArray())variants.Add(v.Replace("    antiAliasing: 4","    antiAliasing: 0"));
 if(path.EndsWith("/EditorSettings.asset")){foreach(var v in variants.ToArray())variants.Add(v.Replace("  m_EnterPlayModeOptionsEnabled: 1","  m_EnterPlayModeOptionsEnabled: 0"));foreach(var v in variants.ToArray())variants.Add(v.Replace("  m_EnterPlayModeOptions: 1","  m_EnterPlayModeOptions: 0"));}
 if(!variants.Contains(disk))throw new System.InvalidOperationException("Unrecognized disk changes: "+path);
 before[path]=new Newtonsoft.Json.Linq.JObject{{"json",json},{"dirty",UnityEditor.EditorUtility.IsDirty(obj)},{"disk",System.Convert.ToBase64String(System.IO.File.ReadAllBytes(path))}};
}
System.IO.File.WriteAllText("Docs/Tests/SalvagePrototype/Evidence/Feedback02/P02/NativeBeforeRestore_A.json",before.ToString());
foreach(var property in baseline.Properties())UnityEditor.EditorJsonUtility.FromJsonOverwrite((string)property.Value["json"],objects[property.Name]);
var pipeline=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/EchoStation/ParkourArt/Settings/VC_ForwardHDR.asset");if(!pipeline)throw new System.InvalidOperationException("Original pipeline missing");
UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=pipeline;UnityEngine.QualitySettings.renderPipeline=pipeline;UnityEngine.QualitySettings.antiAliasing=4;
var rows=new Newtonsoft.Json.Linq.JArray();
foreach(var property in baseline.Properties())
{
 var path=property.Name;var row=property.Value;var obj=objects[path];byte[] disk=System.Convert.FromBase64String((string)row["disk"]);bool native=UnityEditor.EditorJsonUtility.ToJson(obj)==(string)row["json"];
 if(!native)throw new System.InvalidOperationException("Native restore mismatch: "+path);
 if(!System.IO.File.ReadAllBytes(path).SequenceEqual(disk))System.IO.File.WriteAllBytes(path,disk);
 if((bool)row["dirty"])UnityEditor.EditorUtility.SetDirty(obj);else UnityEditor.EditorUtility.ClearDirty(obj);
 rows.Add(new Newtonsoft.Json.Linq.JObject{{"path",path},{"nativeExact",native},{"diskExact",System.IO.File.ReadAllBytes(path).SequenceEqual(disk)},{"dirtyExact",UnityEditor.EditorUtility.IsDirty(obj)==(bool)row["dirty"]}});
}
System.IO.File.WriteAllText(receipt,rows.ToString());return rows;
'''
raw=c.request('tools/call',{'name':'execute_code','arguments':{'action':'execute','compiler':'codedom','code':code}})
result=payload(raw)
print(json.dumps(result,ensure_ascii=False))
if not result.get('success'):raise SystemExit(1)
