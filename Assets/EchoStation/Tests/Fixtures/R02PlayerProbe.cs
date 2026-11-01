using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Tests.Fixtures
{
    // Executes only with --r02-report. Not part of the exploration game.
    public sealed class R02PlayerProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnableDiagnosticBackgroundFrames()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--r02-report") >= 0)
                Application.runInBackground = true;
        }

        [Serializable] private class FrameSample
        {
            public int block, frame;
            public uint returnedTimingCount;
            public bool effects;
            public ulong timestamp;
            public double cpuMs, gpuMs;
        }
        [Serializable] private class Result
        {
            public bool passed, frameTimingEnabled;
            public int screenWidth, screenHeight, invalidTimings, duplicateTimings;
            public string error, method = "Normal camera frames; OFF/ON/OFF/ON, 30 warm-up + 90 sampled frames per block. FrameTimingManager latest unique nonzero sample after warm-up; no explicit render request, ReadPixels or PNG encoding in timed blocks. Diagnostic only, not a target-hardware benchmark.";
            public R02ImageProbe.Result images;
            public FrameSample[] frames;
            public FrameSample[] rawTimings;
        }

        private IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "--r02-report");
            if (index < 0 || index + 1 >= args.Length) yield break;
            var path = Path.GetFullPath(args[index + 1]);
            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            yield return null; yield return null; yield return null;
            var report = new Result { screenWidth = Screen.width, screenHeight = Screen.height,
                frameTimingEnabled = FrameTimingManager.IsFeatureEnabled() };
            R02ImageProbe probe = null;
            IEnumerator run = null;
            var camera = GetComponent<Camera>();
            try
            {
                var volume = gameObject.scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Volume>()).Single();
                probe = new R02ImageProbe(camera, volume);
                report.images = probe.Report;
                run = probe.Run(directory);
            }
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
            var data = camera.GetUniversalAdditionalCameraData();
            var samples = new List<FrameSample>();
            var rawSamples = new List<FrameSample>();
            var timing = new FrameTiming[1];
            var timestamps = new HashSet<ulong>();
            for (var block = 0; block < 4; block++)
            {
                data.renderPostProcessing = block % 2 != 0;
                for (var frame = 0; frame < 120; frame++)
                {
                    FrameTimingManager.CaptureFrameTimings();
                    yield return null;
                    if (frame < 30) continue;
                    var count = FrameTimingManager.GetLatestTimings(1, timing);
                    var sample = new FrameSample { block = block, effects = data.renderPostProcessing, frame = Time.frameCount,
                        returnedTimingCount = count, timestamp = timing[0].frameStartTimestamp,
                        cpuMs = timing[0].cpuFrameTime, gpuMs = timing[0].gpuFrameTime };
                    rawSamples.Add(sample);
                    if (count == 0 || timing[0].gpuFrameTime <= 0 || timing[0].cpuFrameTime <= 0 || timing[0].frameStartTimestamp == 0)
                    { report.invalidTimings++; continue; }
                    if (!timestamps.Add(timing[0].frameStartTimestamp)) { report.duplicateTimings++; continue; }
                    samples.Add(sample);
                }
            }
            data.renderPostProcessing = true;
            report.frames = samples.ToArray();
            report.rawTimings = rawSamples.ToArray();
            report.passed = string.IsNullOrEmpty(report.error) && report.images != null && report.images.passed &&
                report.screenWidth == 1920 && report.screenHeight == 1080;
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            if (Array.IndexOf(args, "--r02-exit") >= 0) Application.Quit(report.passed ? 0 : 1);
        }
    }
}
