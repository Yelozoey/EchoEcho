using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EchoStation.Rendering.Editor
{
    public static class OptimizationTestEnvironment
    {
        [Serializable]private class Record{public bool restored,enabled;public int options;public string reason;}
        [MenuItem("Tools/EchoStation/Stage 1/R05/Restore Observed Editor Test Options")]
        public static void RestoreObserved()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            // The serialized original was observed repeatedly before R05 test changes:
            // enabled=1, options=1. This recovery menu is specific to this recorded run.
            EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorSettings.enterPlayModeOptionsEnabled=true;Write(true,"Recovered the recorded pre-R05 serialized options (1/1); subsequent tests must preserve both fields.");
        }
        [MenuItem("Tools/EchoStation/Stage 1/R05/Record Editor Test Options")]
        public static void RecordCurrent()=>Write(false,"Read-only final check of actual EditorSettings API values.");
        private static void Write(bool restored,string reason)
        {
            var path=Path.Combine(OptimizationFixtureSetup.EvidencePath,restored?"EditorOptions-Recovery.json":"EditorOptions-Final.json");
            File.WriteAllText(path,JsonUtility.ToJson(new Record{restored=restored,enabled=EditorSettings.enterPlayModeOptionsEnabled,
                options=(int)EditorSettings.enterPlayModeOptions,reason=reason},true));
        }
    }
}
