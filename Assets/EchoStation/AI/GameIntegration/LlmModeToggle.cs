using System;
using EchoStation.Player;
using EchoStation.Station;
using UnityEngine;
using UnityEngine.UI;

namespace EchoStation.AI.GameIntegration
{
 [DisallowMultipleComponent]
 public sealed class LlmModeToggle:MonoBehaviour
 {
  private StationControls controls;private S3DialogueDriver driver;private Button button;private Text caption;private string shown;
  public Button Button=>button;public Text Caption=>caption;
  public void Configure(StationControls panel,S3DialogueDriver source)
  {
   if(!Application.isPlaying||!panel||!panel.Canvas||!source)throw new ArgumentException("Ready station controls and S3 driver required");
   if(button){if(controls!=panel||driver!=source)throw new InvalidOperationException("Mode toggle already has an owner");Refresh();return;}
   controls=panel;driver=source;var parent=controls.Canvas.transform.Find("Shade/Panel");if(!parent)throw new InvalidOperationException("Station controls panel layout missing");
   var go=new GameObject("S3_AI_Mode",typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(830,-24);rect.sizeDelta=new Vector2(340,42);
   var image=go.AddComponent<Image>();image.color=new Color(.08f,.14f,.19f);button=go.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(Toggle);
   var text=new GameObject("Caption",typeof(RectTransform));text.layer=5;text.transform.SetParent(go.transform,false);var textRect=text.GetComponent<RectTransform>();textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=textRect.offsetMax=Vector2.zero;
   caption=text.AddComponent<Text>();caption.font=RuntimeUiFont.Get();caption.fontSize=18;caption.color=new Color(.82f,.9f,1);caption.alignment=TextAnchor.MiddleCenter;caption.supportRichText=false;caption.raycastTarget=false;Refresh();
  }
  private void Toggle(){if(!isActiveAndEnabled||!controls||!controls.IsOpen||!driver||!driver.Ready||!driver.isActiveAndEnabled)return;driver.SetEnabled(!driver.Enabled);Refresh();}
  private void LateUpdate()=>Refresh();
  private void Refresh()
  {
   if(!button)return;button.gameObject.SetActive(isActiveAndEnabled);button.interactable=driver&&driver.Ready&&driver.isActiveAndEnabled;
   var next=driver&&driver.Enabled?"本地 AI：开启":"本地 AI：关闭（脚本模式）";if(next!=shown){shown=next;caption.text=next;}
  }
  private void OnEnable()=>Refresh();private void OnDisable(){if(button)button.gameObject.SetActive(false);}
  private void OnDestroy(){if(button){button.onClick.RemoveListener(Toggle);Destroy(button.gameObject);}}
 }
}
