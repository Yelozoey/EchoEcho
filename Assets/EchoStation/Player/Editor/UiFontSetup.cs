using System;
using EchoStation.Rendering.Editor;
using UnityEditor;

namespace EchoStation.Player.Editor
{
    public static class UiFontSetup
    {
        public const string FontPath="Assets/EchoStation/ThirdParty/Noto/Resources/NotoSansCJKsc-Regular.otf";
        [MenuItem("Tools/EchoStation/Stage 1/P04/Configure Bundled CJK Font")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();AssetDatabase.ImportAsset(FontPath,ImportAssetOptions.ForceSynchronousImport);var importer=AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;if(!importer)throw new InvalidOperationException("Bundled font importer unavailable.");importer.fontTextureCase=FontTextureCase.Dynamic;importer.includeFontData=true;importer.fontSize=22;importer.SaveAndReimport();
        }
    }
}
