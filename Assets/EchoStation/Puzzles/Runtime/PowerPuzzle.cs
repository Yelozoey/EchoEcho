using System;
namespace EchoStation.Puzzles
{
 public static class PowerPuzzle
 {
  public static PuzzleResult Submit(PuzzleProgress progress,bool exhaust,bool recirculation,bool experiment)
  {if(progress==null)return PuzzleResult.Invalid;if(progress.Has("pz03"))return PuzzleResult.AlreadyComplete;if(!progress.Has("pz02"))return PuzzleResult.PrerequisiteMissing;if(!exhaust||recirculation||experiment)return PuzzleResult.WrongCircuit;return progress.Mark("pz03");}
 }
 public sealed class VentilationCycle
 {
  public const float ClearSeconds=3;
  public bool Powered {get;private set;}
  public float Elapsed {get;private set;}
  public float Fraction=>Elapsed/ClearSeconds;
  public bool PassageReady=>Powered&&Elapsed>=ClearSeconds;
  public void Begin(){if(Powered)return;Powered=true;Elapsed=0;}
  public void Step(float seconds){if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));if(Powered)Elapsed=Math.Min(ClearSeconds,Elapsed+seconds);}
  public bool TryRestore(bool powered,float elapsed){if(float.IsNaN(elapsed)||float.IsInfinity(elapsed)||elapsed<0||elapsed>ClearSeconds||!powered&&elapsed!=0)return false;Powered=powered;Elapsed=elapsed;return true;}
 }
}
