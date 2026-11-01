using System;

namespace EchoStation.AI.Execution
{
 public enum FeedbackPhase { Script, Loading, Ready, Thinking, Draining, Failure, CoolingDown }
 public readonly struct InferenceFeedback
 {
  public FeedbackPhase Phase {get;}public string Message {get;}public bool Thinking {get;}public bool CanCancel {get;}
  private InferenceFeedback(FeedbackPhase phase,string message,bool thinking=false,bool cancel=false)
  {Phase=phase;Message=message;Thinking=thinking;CanCancel=cancel;}
  public static InferenceFeedback For(FeedbackPhase phase)
  {
   switch(phase)
   {
    case FeedbackPhase.Script:return new InferenceFeedback(phase,"脚本模式：Echo已就绪。");
    case FeedbackPhase.Loading:return new InferenceFeedback(phase,"正在加载本地AI，请稍候。",true,true);
    case FeedbackPhase.Ready:return new InferenceFeedback(phase,"本地AI已就绪，可以发送指令。");
    case FeedbackPhase.Thinking:return new InferenceFeedback(phase,"Echo正在思考；当前不接收新请求。",true,true);
    case FeedbackPhase.Draining:return new InferenceFeedback(phase,"已取消请求，正在完成资源清理。",true);
    case FeedbackPhase.Failure:return new InferenceFeedback(phase,"本地AI暂不可用，请重试或切回脚本模式。");
    case FeedbackPhase.CoolingDown:return new InferenceFeedback(phase,"请稍候，再发送下一条指令。");
    default:throw new ArgumentOutOfRangeException(nameof(phase));
   }
  }
 }
}
