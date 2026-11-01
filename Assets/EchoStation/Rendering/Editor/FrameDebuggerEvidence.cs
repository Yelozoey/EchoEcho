using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EchoStation.Rendering.Editor
{
    // Test tooling only: inspect the actual installed internal API before binding it.
    public static class FrameDebuggerEvidence
    {
        [Serializable] public class Event
        {
            public int index, draws;
            public string type, label, objectName, shader, pass, lightMode;
            public bool detailsValid;
        }
        [Serializable] public class CaptureResult { public Event[] events; public int count, shadowDraws, shadowSubmissionEvents, connectedProfiler, remote; public bool locallySupported, enabled; }
        public static IEnumerator Capture(Action render, string path, Action<CaptureResult> completed)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var utility = assembly.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility", true);
            var dataType = assembly.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerEventData", true);
            var debugger = typeof(UnityEngine.Rendering.RenderPipeline).Assembly.GetType("UnityEngine.Rendering.FrameDebugger");
            if (debugger != null && (bool)debugger.GetProperty("enabled").GetValue(null))
                throw new InvalidOperationException("User Frame Debugger is already enabled; preserving its state.");
            var set = utility.GetMethod("SetEnabled", flags); var enter = utility.GetMethod("EnterCapturingScope", flags);
            var leave = utility.GetMethod("LeaveCapturingScope", flags);
            var remote = (int)utility.GetMethod("GetRemotePlayerGUID", flags).Invoke(null, null);
            var paused = EditorApplication.isPaused;
            var windowType=assembly.GetType("UnityEditor.FrameDebuggerWindow",true);
            var oldWindow=Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w=>w.GetType()==windowType);
            EditorWindow window=null;
            try
            {
                window=(EditorWindow)windowType.GetMethod("OpenWindow",flags).Invoke(null,null);
                windowType.GetMethod("EnableFrameDebugger",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(window,null);
                // Native capture activation/repaint is deferred across editor updates.
                for(var warmup=0;warmup<6;warmup++) { window.Repaint(); EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
                enter.Invoke(null, null);
                try { render(); } finally { leave.Invoke(null, null); }
                var events = (Array)utility.GetMethod("GetFrameEvents", flags).Invoke(null, null);
                var result = new CaptureResult { count = events.Length, events = new Event[events.Length],
                    connectedProfiler=UnityEditorInternal.ProfilerDriver.connectedProfiler, remote=(int)utility.GetMethod("GetRemotePlayerGUID",flags).Invoke(null,null),
                    locallySupported=(bool)utility.GetProperty("locallySupported",flags).GetValue(null),
                    enabled=debugger!=null && (bool)debugger.GetProperty("enabled").GetValue(null) };
                for (var i = 0; i < events.Length; i++)
                {
                    var entry = events.GetValue(i); var type = entry.GetType();
                    var obj = type.GetField("m_Obj").GetValue(entry) as UnityEngine.Object;
                    var data = Activator.CreateInstance(dataType);
                    var valid = (bool)utility.GetMethod("GetFrameEventData", flags).Invoke(null, new[] { (object)i, data });
                    var row = new Event { index = i, type = type.GetField("m_Type").GetValue(entry).ToString(),
                        label = (string)utility.GetMethod("GetFrameEventInfoName", flags).Invoke(null, new object[] { i }),
                        objectName = obj ? obj.name : "", detailsValid = valid };
                    // These are real Frame Debugger draw/batch submissions. A SRP batch
                    // may contain multiple draws; do not present its count as that draw count.
                    if (new[] { "SRPBatch", "Mesh", "InstancedMesh", "DynamicBatch", "StaticBatch", "DynamicGeometry" }.Contains(row.type) &&
                        (row.label.Contains("/MainLightShadow/") || row.label.Contains("Shadowmap"))) result.shadowSubmissionEvents++;
                    if (valid)
                    {
                        row.shader = (string)dataType.GetField("m_RealShaderName").GetValue(data);
                        row.pass = (string)dataType.GetField("m_PassName").GetValue(data);
                        row.lightMode = (string)dataType.GetField("m_PassLightMode").GetValue(data);
                        row.draws = (int)dataType.GetField("m_DrawCallCount").GetValue(data);
                        if (row.lightMode == "ShadowCaster" || row.pass == "ShadowCaster") result.shadowDraws += row.draws;
                    }
                    result.events[i] = row;
                }
                File.WriteAllText(path, JsonUtility.ToJson(result, true)); completed(result);
            }
            finally
            {
                set.Invoke(null, new object[] { false, remote });
                if(window && !oldWindow) window.Close();
                EditorApplication.isPaused = paused;
            }
        }
        [MenuItem("Tools/EchoStation/Stage 1/R04/Inspect Frame Debugger API")]
        public static void Inspect()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => {
                try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
            }).Where(t => t.Name is "FrameDebuggerUtility" or "FrameDebuggerEvent" or "FrameDebuggerEventData" or "ProfilerDriver");
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            Directory.CreateDirectory(RealtimeShadowSetup.EvidencePath);
            File.WriteAllText(Path.Combine(RealtimeShadowSetup.EvidencePath, "FrameDebuggerAPI.txt"), string.Join("\n", types.Select(t =>
                t.Assembly.GetName().Name + ":" + t.FullName + "\n" + string.Join("\n", t.GetMembers(flags).Where(m => m.MemberType != MemberTypes.Constructor).Select(m => m.ToString())))));
            File.WriteAllText(Path.Combine(RealtimeShadowSetup.EvidencePath,"ProfilerConnections.txt"),
                "connected="+UnityEditorInternal.ProfilerDriver.connectedProfiler+"\n"+string.Join("\n",UnityEditorInternal.ProfilerDriver.GetAvailableProfilers().Select(id=>
                    id+":"+UnityEditorInternal.ProfilerDriver.GetConnectionIdentifier(id))));
        }
    }
}
