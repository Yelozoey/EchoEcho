using System;

namespace EchoStation.AI.Inference
{
 public enum LifecycleChange{NewGame,LoadGame,ManualOverride,WorldChanged,Ending,SceneUnloaded,LLMDisabled,LLMEnabled}
 public readonly struct RequestStamp
 {
  public long Session {get;}public long Revision {get;}public long Request {get;}
  internal RequestStamp(long session,long revision,long request){Session=session;Revision=revision;Request=request;}
 }
 // Single control-thread data. Revalidate immediately before applying a previously taken result.
 public sealed class SessionFence
 {
  public long Session {get;private set;}=1;
  public long Revision {get;private set;}
  public long LastRequest {get;private set;}
  public LifecycleChange? LastChange {get;private set;}
  public RequestStamp Issue()
  {LastRequest=checked(LastRequest+1);return new RequestStamp(Session,Revision,LastRequest);}
  public void Change(LifecycleChange reason)
  {
   if(!Enum.IsDefined(typeof(LifecycleChange),reason))throw new ArgumentOutOfRangeException(nameof(reason));
   if(reason==LifecycleChange.NewGame||reason==LifecycleChange.LoadGame){Session=checked(Session+1);Revision=0;}
   else Revision=checked(Revision+1);LastChange=reason;
  }
  public bool IsCurrent(RequestStamp stamp)=>stamp.Request>0&&stamp.Session==Session&&stamp.Revision==Revision&&stamp.Request==LastRequest;
 }
}
