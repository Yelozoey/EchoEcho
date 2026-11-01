using System;
using EchoStation.AI.Execution;
using EchoStation.Echo;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.UI;

namespace EchoStation.AI.Presentation
{
 [DisallowMultipleComponent]
 public sealed class LlmFeedbackView:MonoBehaviour
 {
  private DialoguePanel panel;private EchoPresentation robot;private Text status;private Button cancel;private InferenceFeedback state=InferenceFeedback.For(FeedbackPhase.Script);private bool requested;
  public Text StatusText=>status;public Button CancelButton=>cancel;public FeedbackPhase Phase=>state.Phase;public bool CancelWasRequested=>requested;
  public event Action CancelRequested;
  public void Configure(DialoguePanel dialogue,EchoPresentation presentation)
  {
   if(!Application.isPlaying||!dialogue||!dialogue.Canvas||!presentation)throw new ArgumentException("Ready dialogue and Echo presentation required");
   if(panel==dialogue&&status&&cancel){if(robot&&robot!=presentation)robot.InferenceThinking=false;robot=presentation;Render();return;}
   Release();panel=dialogue;robot=presentation;var parent=panel.InputField.transform.parent;var font=panel.InputField.textComponent.font;
   status=Label(Rect("AI_Status",parent,new Vector2(1208,22),new Vector2(36,-68)),font,16,TextAnchor.MiddleLeft);status.color=new Color(.68f,.8f,.91f);
   var button=Rect("AI_Cancel",parent,new Vector2(150,42),new Vector2(850,-24));var image=button.gameObject.AddComponent<Image>();image.color=new Color(.08f,.14f,.19f);cancel=button.gameObject.AddComponent<Button>();cancel.targetGraphic=image;cancel.navigation=new Navigation{mode=Navigation.Mode.None};
   var caption=Rect("Caption",button,new Vector2(150,42),Vector2.zero);Label(caption,font,18,TextAnchor.MiddleCenter).text="取消当前请求";cancel.onClick.AddListener(RequestCancel);Render();
  }
  public void SetPhase(FeedbackPhase phase)
  {var next=InferenceFeedback.For(phase);if(next.Phase==state.Phase)return;state=next;requested=false;Render();}
  private void RequestCancel()
  {if(!isActiveAndEnabled||!panel||!panel.IsOpen||!state.CanCancel||requested)return;requested=true;if(cancel)cancel.interactable=false;CancelRequested?.Invoke();}
  private void Render()
  {
   if(robot)robot.InferenceThinking=isActiveAndEnabled&&state.Thinking;
   if(status){status.text=state.Message;status.gameObject.SetActive(isActiveAndEnabled);}
   if(cancel){cancel.interactable=isActiveAndEnabled&&state.CanCancel&&!requested;cancel.gameObject.SetActive(isActiveAndEnabled);}
  }
  private void OnEnable()=>Render();
  private void OnDisable(){if(robot)robot.InferenceThinking=false;if(status)status.gameObject.SetActive(false);if(cancel){cancel.interactable=false;cancel.gameObject.SetActive(false);}}
  private void OnDestroy()=>Release();
  private void Release()
  {if(robot)robot.InferenceThinking=false;if(cancel){cancel.onClick.RemoveListener(RequestCancel);Destroy(cancel.gameObject);}if(status)Destroy(status.gameObject);cancel=null;status=null;panel=null;robot=null;}
  private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
  {var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;}
  private static Text Label(RectTransform rect,Font font,int size,TextAnchor alignment)
  {var label=rect.gameObject.AddComponent<Text>();label.font=font;label.fontSize=size;label.alignment=alignment;label.supportRichText=false;label.raycastTarget=false;label.color=new Color(.82f,.9f,1);label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;return label;}
 }
}
