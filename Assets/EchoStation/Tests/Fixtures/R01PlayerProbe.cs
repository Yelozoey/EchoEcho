using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Tests.Fixtures
{
    // Runs only in the isolated validation scene, and only with --r01-report.
    public class R01PlayerProbe : MonoBehaviour
    {
        [Serializable] private class Result
        {
            public bool passed;
            public string error, unityVersion, os, cpu, gpu, graphicsApi, pipeline, targetFormat;
            public int memoryMb, videoMemoryMb, width, height;
            public bool internalHdr, displayHdrActive;
            public float r, g, b;
        }

        private IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            var flag = Array.IndexOf(args, "--r01-report");
            if (flag < 0 || flag + 1 >= args.Length) yield break;
            yield return null;
            yield return null;
            yield return null;
            var result = Capture();
            var path = Path.GetFullPath(args[flag + 1]);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(result, true));
            if (Array.IndexOf(args, "--r01-exit") >= 0) Application.Quit(result.passed ? 0 : 1);
        }

        private Result Capture()
        {
            var result = new Result { unityVersion = Application.unityVersion, os = SystemInfo.operatingSystem,
                cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                memoryMb = SystemInfo.systemMemorySize, videoMemoryMb = SystemInfo.graphicsMemorySize,
                width = Screen.width, height = Screen.height, displayHdrActive = HDROutputSettings.main != null && HDROutputSettings.main.active };
            var oldActive = RenderTexture.active;
            RenderTexture target = null;
            Texture2D pixel = null;
            try
            {
                var camera = GetComponent<Camera>();
                var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                result.pipeline = pipeline ? pipeline.name : "missing";
                result.internalHdr = pipeline && pipeline.supportsHDR && camera.allowHDR;
                target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                target.Create();
                result.targetFormat = target.graphicsFormat.ToString();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                pixel = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
                pixel.ReadPixels(new Rect(960, 540, 1, 1), 0, 0);
                pixel.Apply();
                var color = pixel.GetPixel(0, 0);
                result.r = color.r; result.g = color.g; result.b = color.b;
                result.passed = result.unityVersion == "6000.6.7f1" && result.graphicsApi == "Direct3D11" &&
                    result.pipeline == "ES_PC_ForwardHDR" && result.internalHdr && !result.displayHdrActive &&
                    !camera.GetUniversalAdditionalCameraData().allowHDROutput && result.width == 1920 && result.height == 1080 &&
                    Mathf.Abs(result.r - 4f) < 0.02f && Mathf.Abs(result.g - 2f) < 0.02f && Mathf.Abs(result.b - 1f) < 0.02f;
            }
            catch (Exception exception) { result.error = exception.ToString(); }
            finally
            {
                RenderTexture.active = oldActive;
                if (target) { target.Release(); Destroy(target); }
                if (pixel) Destroy(pixel);
            }
            return result;
        }
    }
}
