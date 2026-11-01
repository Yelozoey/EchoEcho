using System;
using System.Collections;
using System.IO;
using System.Reflection;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextFrameEvidence
    {
        public static IEnumerator Capture(Action render,string path,Action<JArray> completed)
        {
            JArray details=null;yield return FrameDebuggerEvidence.Capture(render,path,summary=>
            {
                var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;var assembly=typeof(UnityEditor.Editor).Assembly;var utility=assembly.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility",true);var type=assembly.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerEventData",true);var get=utility.GetMethod("GetFrameEventData",flags);var limit=utility.GetProperty("limit",flags);var enter=utility.GetMethod("EnterCapturingScope",flags);var leave=utility.GetMethod("LeaveCapturingScope",flags);int previous=(int)limit.GetValue(null);details=new JArray();try{foreach(var entry in summary.events){if(entry.type=="InstancedMesh"||entry.type=="SRPBatch"){limit.SetValue(null,entry.index+1);enter.Invoke(null,null);try{render();}finally{leave.Invoke(null,null);}}var data=Activator.CreateInstance(type);bool valid=(bool)get.Invoke(null,new[]{(object)entry.index,data});var row=new JObject{{"event",entry.index},{"type",entry.type},{"label",entry.label},{"object",entry.objectName},{"detailsValid",valid},{"shader",valid?(string)type.GetField("m_RealShaderName").GetValue(data):""},{"pass",valid?(string)type.GetField("m_PassName").GetValue(data):""},{"instances",valid?(int)type.GetField("m_InstanceCount").GetValue(data):0},{"draws",valid?(int)type.GetField("m_DrawCallCount").GetValue(data):0}};details.Add(row);}}finally{limit.SetValue(null,previous);}File.WriteAllText(path+".details.json",details.ToString());
            });completed(details);
        }
    }
}
