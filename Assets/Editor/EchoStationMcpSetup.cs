using System;
using System.IO;
using System.Threading.Tasks;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

// Initializes the local MCP bridge once after the package is imported.
[InitializeOnLoad]
public static class EchoStationMcpSetup
{
    private const string DoneKey = "EchoStation.McpSetup.Done";
    private static string MarkerPath => Path.Combine(Application.dataPath, "..", "Library", "MCPForUnity", "project-setup.done");

    static EchoStationMcpSetup()
    {
        if (!SessionState.GetBool(DoneKey, false) && !File.Exists(MarkerPath))
            EditorApplication.delayCall += Configure;
    }

    [MenuItem("Tools/EchoStation/Configure Unity MCP")]
    private static async void Configure()
    {
        try
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            var uvxPath = Path.Combine(projectPath, "Library", "MCPForUnity", "uv", "bin", "uvx.exe");
            if (!File.Exists(uvxPath))
                throw new FileNotFoundException("The project-local uvx runtime is missing.", uvxPath);

            EditorPrefs.SetString("MCPForUnity.UvxPath", uvxPath);
            EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://localhost:8080");
            EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", false);

            var config = EditorConfigurationCache.Instance;
            config.SetUseHttpTransport(true);
            config.Refresh();

            var server = MCPServiceLocator.Server;
            if (!server.IsLocalHttpServerReachable() && !server.StartLocalHttpServer(quiet: true))
                throw new InvalidOperationException("Unity MCP server could not start.");

            var deadline = DateTime.UtcNow.AddMinutes(5);
            while (!server.IsLocalHttpServerReachable())
            {
                if (DateTime.UtcNow >= deadline)
                    throw new TimeoutException("Unity MCP server startup timed out.");
                await Task.Delay(500);
            }

            if (!await MCPServiceLocator.Bridge.StartAsync())
                throw new InvalidOperationException("Unity MCP bridge could not connect.");

            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            File.WriteAllText(MarkerPath, "Unity MCP HTTP setup completed.");
            SessionState.SetBool(DoneKey, true);
            Debug.Log("[EchoStation] Unity MCP connected at http://localhost:8080/mcp");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
