using System;
namespace EchoStation.SalvageNext
{
    public interface IDisplayBackend
    {
        DisplaySelection Capture();
        bool TryApply(DisplaySelection selection,out string error);
    }
    public enum DisplayPhase { Idle, Applying, Confirming, Reverting, Failed }
    // Native display changes complete asynchronously. Persistence happens only after
    // an observed match and explicit confirmation; every other exit restores Before.
    public sealed class DisplayTransaction
    {
        readonly IDisplayBackend backend;readonly IDisplaySettingsWriter writer;
        double last=double.NegativeInfinity,deadline,matchedSince=double.NaN;
        public DisplayPhase Phase{get;private set;}
        public DisplaySelection Before{get;private set;}
        public DisplaySelection Requested{get;private set;}
        public string Message{get;private set;}="";
        public bool IsPending=>Phase==DisplayPhase.Applying||Phase==DisplayPhase.Confirming||Phase==DisplayPhase.Reverting;
        public DisplayTransaction(IDisplayBackend backend,IDisplaySettingsWriter writer)
        {this.backend=backend??throw new ArgumentNullException(nameof(backend));this.writer=writer??throw new ArgumentNullException(nameof(writer));}
        void Time(double now){if(double.IsNaN(now)||double.IsInfinity(now)||now<last)throw new ArgumentException("Monotonic finite time required.");last=now;}
        bool Apply(DisplaySelection value,out string error){try{return backend.TryApply(value,out error);}catch(Exception e){error=e.Message;return false;}}
        bool Matches(DisplaySelection value){try{return value!=null&&value.Equals(backend.Capture());}catch(Exception e){Message=e.Message;return false;}}
        public double SecondsLeft(double now)=>Math.Max(0,deadline-now);
        public bool Begin(DisplaySelection value,double now)
        {
            Time(now);if(value==null||Phase!=DisplayPhase.Idle)return false;
            try{Before=backend.Capture();if(Before==null)throw new InvalidOperationException("No display snapshot.");}catch(Exception e){Message=e.Message;return false;}
            Requested=value;Phase=DisplayPhase.Applying;deadline=now+3;matchedSince=double.NaN;Message="正在应用显示设置…";
            if(!Apply(value,out var error))Revert(now,"应用失败："+error);return true;
        }
        void Revert(double now,string reason)
        {Message=reason;Phase=DisplayPhase.Reverting;deadline=now+3;if(!Apply(Before,out var error)){Message+="；还原请求失败："+error;}}
        public void Cancel(double now,string reason="已取消，正在还原。")
        {Time(now);if(Phase==DisplayPhase.Applying||Phase==DisplayPhase.Confirming)Revert(now,reason);}
        public void Tick(double now,bool focused)
        {
            Time(now);
            if(Phase==DisplayPhase.Applying)
            {
                if(!focused){Revert(now,"窗口失焦，正在还原。");return;}
                if(Matches(Requested)){if(double.IsNaN(matchedSince))matchedSince=now;if(now-matchedSince>=1){Phase=DisplayPhase.Confirming;deadline=now+15;Message="保留这些设置？";return;}}else matchedSince=double.NaN;
                if(now>=deadline)Revert(now,"系统未稳定接受显示设置，正在还原。");
            }
            else if(Phase==DisplayPhase.Confirming)
            {if(!focused||!Matches(Requested)||now>=deadline)Revert(now,!focused?"窗口失焦，正在还原。":now>=deadline?"确认超时，正在还原。":"检测到外部显示改动，正在还原。");}
            else if(Phase==DisplayPhase.Reverting)
            {if(Matches(Before)){Phase=DisplayPhase.Idle;Message+=" 已还原。";}else if(now>=deadline){Phase=DisplayPhase.Failed;Message+=" 无法确认已还原，请重新检查显示设置。";}}
        }
        public bool Confirm(double now,bool focused)
        {
            Time(now);if(Phase!=DisplayPhase.Confirming)return false;
            if(!focused||now>=deadline||!Matches(Requested)){Revert(now,"确认条件失效，正在还原。");return false;}
            string error;bool ok;try{ok=writer.TryWrite(Requested,out error);}catch(Exception e){ok=false;error=e.Message;}
            if(!ok){Revert(now,"配置未保存："+error);return false;}Phase=DisplayPhase.Idle;Message="显示设置已保存。";return true;
        }
    }
}
