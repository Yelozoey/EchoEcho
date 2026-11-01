using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Tests.Fixtures
{
    public sealed class R03PlayerProbe : MonoBehaviour
    {
        public UniversalRendererData validationRenderer;
        [Serializable] private class Result
        {
            public bool passed;
            public int screenWidth, screenHeight;
            public string error;
            public R03LightingProbe.Result lighting;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BackgroundForTestOnly()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--r03-report") >= 0) Application.runInBackground = true;
        }

        private IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); var flag = Array.IndexOf(args, "--r03-report");
            if (flag < 0 || flag + 1 >= args.Length) yield break;
            var path = Path.GetFullPath(args[flag + 1]); var directory = Path.GetDirectoryName(path); Directory.CreateDirectory(directory);
            yield return null; yield return null; yield return null;
            var report = new Result { screenWidth = Screen.width, screenHeight = Screen.height };
            R03LightingProbe probe = null; IEnumerator run = null;
            try { probe = new R03LightingProbe(gameObject.scene, validationRenderer); report.lighting = probe.Report; run = probe.Run(directory); }
            catch (Exception exception) { report.error = exception.ToString(); }
            if (run != null)
            {
                while (true)
                {
                    bool more;
                    try { more = run.MoveNext(); }
                    catch (Exception exception) { report.error = exception.ToString(); break; }
                    if (!more) break;
                    yield return run.Current;
                }
            }
            probe?.Dispose();
            report.passed = string.IsNullOrEmpty(report.error) && report.lighting != null && report.lighting.passed &&
                report.screenWidth == 1920 && report.screenHeight == 1080;
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            if (Array.IndexOf(args, "--r03-exit") >= 0) Application.Quit(report.passed ? 0 : 1);
        }
    }
}
