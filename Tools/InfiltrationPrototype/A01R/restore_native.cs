EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
const string folder="Docs/Tests/InfiltrationPrototype/Evidence/A01R";
var baseline=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(folder+"/NativeBefore_A.json"));
string temporaryGuid=UnityEditor.AssetDatabase.AssetPathToGUID("Assets/EchoStation/Rendering/Settings/ES_Gameplay_ForwardHDR.asset");
var objects=new System.Collections.Generic.Dictionary<string,UnityEngine.Object>();
foreach(var entry in baseline.Properties())
{
    var obj=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(entry.Name).Single();objects.Add(entry.Name,obj);
    var expected=Newtonsoft.Json.Linq.JObject.Parse((string)entry.Value["json"]);
    var current=Newtonsoft.Json.Linq.JObject.Parse(UnityEditor.EditorJsonUtility.ToJson(obj));
    var permitted=(Newtonsoft.Json.Linq.JObject)expected.DeepClone();string originalGuid=null;
    if(entry.Name.EndsWith("/GraphicsSettings.asset"))
    {
        var token=current["GraphicsSettings"]["m_CustomRenderPipeline"];originalGuid=(string)expected["GraphicsSettings"]["m_CustomRenderPipeline"]["guid"];
        if((string)token["guid"]!=originalGuid&&(string)token["guid"]!=temporaryGuid)throw new System.InvalidOperationException("Unknown default pipeline");
        permitted["GraphicsSettings"]["m_CustomRenderPipeline"]=token.DeepClone();
    }
    if(entry.Name.EndsWith("/QualitySettings.asset"))
    {
        if(UnityEngine.QualitySettings.GetQualityLevel()!=1)throw new System.InvalidOperationException("Preserve changed quality selection");
        var pc=current["QualitySettings"]["m_QualitySettings"][1];var original=expected["QualitySettings"]["m_QualitySettings"][1];
        originalGuid=(string)original["customRenderPipeline"]["guid"];
        if((string)pc["name"]!="PC"||((string)pc["customRenderPipeline"]["guid"]!=originalGuid&&(string)pc["customRenderPipeline"]["guid"]!=temporaryGuid))throw new System.InvalidOperationException("Unknown PC binding");
        int aa=(int)pc["antiAliasing"];if(aa!=(int)original["antiAliasing"]&&aa!=0)throw new System.InvalidOperationException("Unknown PC AA change");
        permitted["QualitySettings"]["m_QualitySettings"][1]["customRenderPipeline"]=pc["customRenderPipeline"].DeepClone();
        permitted["QualitySettings"]["m_QualitySettings"][1]["antiAliasing"]=aa;
    }
    if(!Newtonsoft.Json.Linq.JToken.DeepEquals(current,permitted))throw new System.InvalidOperationException("Unrelated native setting changes: "+entry.Name);
    string originalText=System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String((string)entry.Value["disk"]));
    var variants=new System.Collections.Generic.HashSet<string>{originalText};
    if(originalGuid!=null)variants.Add(originalText.Replace(originalGuid,temporaryGuid));
    if(entry.Name.EndsWith("/QualitySettings.asset"))
    {
        foreach(string value in variants.ToArray())
        {
            int start=value.IndexOf("    name: PC",System.StringComparison.Ordinal);
            int end=value.IndexOf("\n  - serializedVersion:",start,System.StringComparison.Ordinal);if(end<0)end=value.Length;
            if(start<0)throw new System.InvalidOperationException("Original PC YAML missing");
            string block=value.Substring(start,end-start);
            string expectedAA="    antiAliasing: "+(int)expected["QualitySettings"]["m_QualitySettings"][1]["antiAliasing"];
            if(block.Split(new[]{expectedAA},System.StringSplitOptions.None).Length!=2)throw new System.InvalidOperationException("Ambiguous PC AA field");
            variants.Add(value.Substring(0,start)+block.Replace(expectedAA,"    antiAliasing: 0")+value.Substring(end));
        }
    }
    string disk=System.Text.Encoding.UTF8.GetString(System.IO.File.ReadAllBytes(entry.Name));
    if(!variants.Contains(disk))throw new System.InvalidOperationException("Unrelated disk setting changes: "+entry.Name);
}
foreach(var entry in baseline.Properties())
{
    var obj=objects[entry.Name];UnityEditor.EditorJsonUtility.FromJsonOverwrite((string)entry.Value["json"],obj);
    byte[] original=System.Convert.FromBase64String((string)entry.Value["disk"]);
    if(!System.IO.File.ReadAllBytes(entry.Name).SequenceEqual(original))System.IO.File.WriteAllBytes(entry.Name,original);
    if((bool)entry.Value["dirty"])UnityEditor.EditorUtility.SetDirty(obj);else UnityEditor.EditorUtility.ClearDirty(obj);
}
var results=new Newtonsoft.Json.Linq.JArray();
foreach(var entry in baseline.Properties())
{
    var obj=objects[entry.Name];results.Add(new Newtonsoft.Json.Linq.JObject{{"path",entry.Name},{"nativeExact",UnityEditor.EditorJsonUtility.ToJson(obj)==(string)entry.Value["json"]},{"diskExact",System.IO.File.ReadAllBytes(entry.Name).SequenceEqual(System.Convert.FromBase64String((string)entry.Value["disk"]))},{"dirtyExact",UnityEditor.EditorUtility.IsDirty(obj)==(bool)entry.Value["dirty"]}});
}
if(results.Any(r=>!(bool)r["nativeExact"]||!(bool)r["diskExact"]||!(bool)r["dirtyExact"]))throw new System.InvalidOperationException("Restoration did not verify");
UnityEditor.SceneView.RepaintAll();UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
return new {restored=true,rows=results};
