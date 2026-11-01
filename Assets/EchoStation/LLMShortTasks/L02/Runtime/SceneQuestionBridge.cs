using System;
using System.IO;
using System.Linq;
using System.Threading;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.Echo;
using EchoStation.L01;
using EchoStation.Salvage;
using EchoStation.SalvageExpansion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace EchoStation.L02
{
    public static class SceneQuestionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;Loaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
        static void Loaded(Scene scene,LoadSceneMode mode){if(scene.path!=ExpansionDialogueBootstrap.ScenePath)return;foreach(var root in scene.GetRootGameObjects())foreach(var world in root.GetComponentsInChildren<ExpansionWorld>(true))if(!world.GetComponent<SceneQuestionBridge>())world.gameObject.AddComponent<SceneQuestionBridge>();}
    }
    [DefaultExecutionOrder(80),DisallowMultipleComponent]
    public sealed class SceneQuestionBridge:MonoBehaviour
    {
        ExpansionWorld world;ExpansionEcho echo;FacilityController facility;ExpansionPersistence persistence;ExpansionDialoguePanel panel;EchoPresentation presentation;
        SceneQuestionSession session;ILocalModelFactory factory;IMonotonicClock clock;Button toggle,cancel;Text source;RectTransform messages,status;string chosen;bool initialized,disposing,shownEnabled;RequestPhase previousPhase;Func<string,EchoTextContext,string,bool> interceptor;Func<string,bool> targetInterceptor;
        public bool AIEnabled=>session!=null&&session.Enabled;public bool Busy=>session!=null&&session.Busy;public bool CleanupComplete=>session==null||session.CleanupComplete;public string SourceText=>source?source.text:"";
        public Button ToggleButton=>toggle;public Button CancelButton=>cancel;public QuestionReply LastReply{get;private set;}public RequestPhase Phase=>session?.Phase??RequestPhase.New;
#if UNITY_EDITOR
        public void ConfigureTests(ILocalModelFactory value,IMonotonicClock timer=null){if(initialized)throw new InvalidOperationException("Configure before initialization");factory=value;clock=timer;}
#endif
        void Start()=>Initialize();
        public void Initialize()
        {
            if(initialized)return;world=GetComponent<ExpansionWorld>();echo=GetComponent<ExpansionEcho>();facility=GetComponent<FacilityController>();persistence=GetComponent<ExpansionPersistence>();panel=GetComponent<ExpansionDialoguePanel>();if(!panel)panel=gameObject.AddComponent<ExpansionDialoguePanel>();panel.Initialize();presentation=echo.Robot.GetComponent<EchoPresentation>();
            var parent=panel.InputField.transform.parent;messages=panel.Canvas.GetComponentsInChildren<ScrollRect>(true).Single(s=>s.name=="Messages").GetComponent<RectTransform>();status=panel.Canvas.GetComponentsInChildren<Text>(true).Single(t=>t.name=="Mode").rectTransform;status.sizeDelta=new Vector2(850,38);
            toggle=SalvageWidgets.Button(parent,"L02_Mode","启用本地AI问答（实验）",900,68,390,()=>SetAIEnabled(!AIEnabled));cancel=SalvageWidgets.Button(parent,"L02_Cancel","取消AI请求",1090,115,200,CancelQuestion);
            source=SalvageWidgets.Label(SalvageWidgets.Rect("L02_Source",parent,415,500,875,126),"",18);source.color=new Color(.65f,.83f,.9f);
            interceptor=HandleQuestion;targetInterceptor=Choose;panel.QuestionInterceptor=interceptor;panel.TargetClickInterceptor=targetInterceptor;panel.Closed+=Closed;persistence.Restored+=Restored;initialized=true;RefreshMode();
        }
        string Version()=>persistence.Epoch+":"+facility.Revision+":"+world.Inventory.Revision;
        public bool SetAIEnabled(bool value)
        {
            Initialize();if(disposing||value&&(!isActiveAndEnabled||!world.ActionsAllowed||!world.Input.HasApplicationFocus))return false;
            if(session==null&&value){if(factory==null)factory=new LocalQwen3Factory(Path.Combine(Application.streamingAssetsPath,"EchoLLM","Qwen3_06B_Q4"),new JsonIntentConstraintProvider(),promptProfile:SceneQuestionPromptV2.Profile);session=new SceneQuestionSession(factory,Thread.CurrentThread.ManagedThreadId,clock);}
            session?.SetEnabled(value);chosen=null;source.text="";RefreshMode();return AIEnabled==value;
        }
        void RefreshMode()
        {
            bool enabled=AIEnabled;shownEnabled=enabled;GameRuntimeOptions.UseLLM=enabled;panel.CaptionOverride=enabled?"本地AI v2问答实验 · 质量未达标 · 动作走脚本":null;panel.TargetActionLabel=enabled?"问答":"调查";
            messages.sizeDelta=new Vector2(messages.sizeDelta.x,enabled?320:450);source.gameObject.SetActive(enabled);toggle.GetComponentInChildren<Text>().text=enabled?"关闭AI · 返回脚本":"启用本地AI问答（实验）";cancel.gameObject.SetActive(enabled);panel.RefreshExtensionView();
            panel.SetNotice(enabled?(Phase==RequestPhase.Loading?"正在加载本地模型，可取消或返回脚本；就绪后再发送问题。":"模型解释未经完整事实核验，请对照下方扫描报告。先选择目标再提问。"):"脚本模式：跟随、待命、调查、电量和报告。");
        }
        bool Choose(string id)
        {if(!AIEnabled)return false;session.Invalidate(LifecycleChange.ManualOverride);chosen=id;ShowSource(panel.KnownContext,id);panel.SetNotice("已选择问答对象。输入问题后发送；调查仍可输入“调查这个”。");return true;}
        void ShowSource(EchoTextContext known,string id)
        {
            var target=known.Targets.SingleOrDefault(t=>t.Id==id);source.text=target==null?"来源：请先选择已发现目标。":string.IsNullOrEmpty(target.Report)?"来源："+target.Name+"｜尚未扫描":target.Stale?"来源："+target.Name+"｜报告已过时，请重新扫描。":"来源："+target.Name+"｜本次有效扫描摘要\n"+target.Report+"\nAI回复为实验解释，历史消息不代表当前状态。";
        }
        bool HandleQuestion(string text,EchoTextContext known,string pointed)
        {
            if(!AIEnabled)return false;if(Busy){panel.SetNotice("上一条请求尚未结束。请等待或取消，不会自动排队。");return true;}var selected=SceneQuestion.Select(text,known,chosen??pointed,Version());
            if(!selected.Success){panel.AddExternalReply("Echo（脚本校验）",selected.Message);return true;}
            chosen=selected.Capture.Source.Id;ShowSource(known,chosen);var admission=session.Submit(selected.Capture);
            panel.SetNotice(admission==Admission.Accepted?"Echo正在本地思考。可取消请求或返回探索。":admission==Admission.Busy?"上一条请求尚未结束。请等待或取消，不会自动排队。":admission==Admission.CoolingDown?"推理刚结束，请稍候再发送。":"本地模型正在加载或释放资源，请稍候再发送。");return true;
        }
        public void CancelQuestion(){if(session==null)return;if(Phase==RequestPhase.Loading){SetAIEnabled(false);panel.SetNotice("已取消AI加载，返回脚本模式。");return;}session.Invalidate(LifecycleChange.ManualOverride);panel.SetNotice("已取消AI请求。旧结果不会显示；完成清理后可重发。");}
        void Closed(){session?.Invalidate(LifecycleChange.ManualOverride);chosen=null;if(source)source.text="";}
        void Restored(){session?.Invalidate(LifecycleChange.LoadGame);chosen=null;LastReply=null;if(source)source.text="";}
        void Update()
        {
            if(!initialized||disposing)return;
            if(AIEnabled&&!GameRuntimeOptions.UseLLM)SetAIEnabled(false);
            if(!AIEnabled&&GameRuntimeOptions.UseLLM)GameRuntimeOptions.UseLLM=false;
            if(session!=null)
            {
                session.Poll(panel.KnownContext,Version());
                if(session.TryTake(out var reply))
                {
                    LastReply=reply;if(panel.IsOpen){if(reply.Success){ShowSource(panel.KnownContext,reply.Source.Source.Id);panel.AddExternalReply("Echo（本地AI实验·"+reply.Source.Source.Name+"）",reply.Content);panel.SetNotice("此解释未经完整事实核验，请对照来源报告。");}
                        else{panel.AddExternalReply("Echo（脚本兜底）",IntentJsonParser.FallbackLine);panel.SetNotice(reply.Error);}}
                }
                if(shownEnabled!=AIEnabled)RefreshMode();
            }
            if(AIEnabled&&!world.ActionsAllowed)SetAIEnabled(false);
            if(AIEnabled&&panel.IsOpen&&chosen!=null)ShowSource(panel.KnownContext,chosen);
            if(AIEnabled&&Phase==RequestPhase.Ready&&previousPhase==RequestPhase.Loading)panel.SetNotice("本地AI已就绪。请选择已扫描目标，再输入现场问题。");previousPhase=Phase;
            if(cancel)cancel.interactable=Busy||Phase==RequestPhase.Loading;
        }
        void LateUpdate(){if(initialized&&Busy&&presentation&&echo.Mode!=IntentKind.Warn&&world.GetComponent<EnvironmentHazards>().RateAt(world.transform.position)<=0)presentation.ApplyFrame(0,EchoMood.Thinking,Time.unscaledDeltaTime,Time.unscaledTime,false);}
        void OnDisable(){if(!initialized)return;session?.SetEnabled(false);GameRuntimeOptions.UseLLM=false;if(panel&&panel.Canvas&&toggle&&source)RefreshMode();}
        void OnDestroy()
        {
            disposing=true;if(!initialized)return;if(panel){if(panel.QuestionInterceptor==interceptor)panel.QuestionInterceptor=null;if(panel.TargetClickInterceptor==targetInterceptor)panel.TargetClickInterceptor=null;panel.Closed-=Closed;panel.CaptionOverride=null;panel.TargetActionLabel="调查";}if(persistence)persistence.Restored-=Restored;session?.Dispose();GameRuntimeOptions.UseLLM=false;if(messages)messages.sizeDelta=new Vector2(messages.sizeDelta.x,450);if(status)status.sizeDelta=new Vector2(1230,38);if(toggle)Destroy(toggle.gameObject);if(cancel)Destroy(cancel.gameObject);if(source)Destroy(source.gameObject);
        }
    }
}
