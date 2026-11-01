namespace EchoStation.AI.Execution
{
 public enum InputRoute { Script, Inference }
 // Control-thread state only. No input replay, queue, model or save access.
 public sealed class UseLlmRouting
 {
  public bool Enabled {get;private set;}public int ConsecutiveFaults {get;private set;}public bool AutomaticallyDisabled {get;private set;}
  public InputRoute Route=>Enabled?InputRoute.Inference:InputRoute.Script;
  public void SetEnabled(bool value)
  {if(Enabled==value)return;Enabled=value;if(value){ConsecutiveFaults=0;AutomaticallyDisabled=false;}}
  public void RecordSuccess(){if(Enabled)ConsecutiveFaults=0;}
  public bool RecordFault()
  {if(!Enabled)return false;ConsecutiveFaults++;if(ConsecutiveFaults<3)return false;Enabled=false;AutomaticallyDisabled=true;return true;}
 }
}
