using System;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Station;
using UnityEngine;
using UnityEngine.UI;

namespace EchoStation.AI.GameIntegration
{
 [DefaultExecutionOrder(-500),RequireComponent(typeof(StationSession)),DisallowMultipleComponent]
 public sealed class S3Bootstrap:MonoBehaviour
 {
  public const string ScenePath="Assets/EchoStation/AI/Scenes/ES_S3_AI_Demo.unity";
  public const string NavigationPath="Assets/EchoStation/AI/Generated/ES_S3_NavMesh.asset";
  public static string SaveDirectory=>Path.Combine(Application.persistentDataPath,"Saves","S3");
  private void Awake()
  {
   var persistence=GetComponent<StationPersistence>();if(!persistence)throw new InvalidOperationException("S3 needs the complete station persistence adapter");persistence.ConfigureDirectory(SaveDirectory,"S3");GameRuntimeOptions.UseLLM=false;
   if(!GetComponent<S3DialogueDriver>())gameObject.AddComponent<S3DialogueDriver>();
  }
  private void Start()
  {
   var lifecycle=GetComponent<StationLifecycle>();if(!lifecycle||!lifecycle.Canvas)throw new InvalidOperationException("S3 title UI is missing");var labels=lifecycle.Canvas.GetComponentsInChildren<Text>(true);
   labels.Single(t=>t.name=="SaveNotice").text="S3独立单槽 · F5保存 / F9读取\n新局和结局均不自动覆盖手动档。";
   labels.Single(t=>t.name=="Footer").text="无战斗 · 探索与环境线索 · Echo支持离线AI与脚本模式\nWASD / 鼠标 · E交互 · J日志 · T文字 · Tab切换本地AI";
  }
 }
}
