using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextArtV2Ui
    {
        public const string Root=NextArtV2Layout.Root+"/UI",Theme=Root+"/ES_UITheme.asset",Evidence="Docs/Tests/SalvagePrototype/Evidence/Feedback02/U01";
        public static void InstallPanels()
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();using(NextSdkSaveScope.Begin(Evidence+"/Panels_A.scope.json")){var scene=EditorSceneManager.OpenScene(NextArtV2Stream.Main,OpenSceneMode.Additive);try{var player=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<NextHud>()).Single();if(player.GetComponent<NextPanelStyle>())throw new InvalidOperationException("Panel style exists");player.gameObject.AddComponent<NextPanelStyle>();EditorSceneManager.SaveScene(scene);}finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}
        }
        public static void InstallHud()
        {
            SceneSafetyGuard.RequireClean();Directory.CreateDirectory(Evidence);if(File.Exists(Theme))throw new InvalidOperationException("Keep existing theme");
            foreach(var p in Directory.GetFiles(Root+"/Kenney","*.png")){var ti=(TextureImporter)AssetImporter.GetAtPath(p.Replace('\\','/'));ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.spritePixelsPerUnit=100;ti.maxTextureSize=256;if(p.EndsWith("Panel.png"))ti.spriteBorder=new Vector4(8,8,8,20);else if(p.EndsWith("Button.png"))ti.spriteBorder=new Vector4(5,5,5,5);ti.SaveAndReimport();}
            var theme=ScriptableObject.CreateInstance<NextUiTheme>();foreach(var field in typeof(NextUiTheme).GetFields().Where(f=>f.FieldType==typeof(Sprite))){string name=field.Name=="Tab"?"TAB":field.Name;field.SetValue(theme,AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Kenney/"+name+".png"));if(field.GetValue(theme)==null)throw new InvalidOperationException("Sprite missing:"+name);}AssetDatabase.CreateAsset(theme,Theme);AssetDatabase.SaveAssetIfDirty(theme);
            var previous=SceneManager.GetActiveScene();using(NextSdkSaveScope.Begin(Evidence+"/Install_A.scope.json")){var scene=EditorSceneManager.OpenScene(NextArtV2Stream.Main,OpenSceneMode.Additive);try{var player=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMission>()).Single();if(player.GetComponent<NextHud>())throw new InvalidOperationException("HUD exists");player.gameObject.AddComponent<NextHud>().Configure(theme);EditorSceneManager.SaveScene(scene);}finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}
        }
    }
}
