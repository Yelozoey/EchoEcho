using System;
using EchoStation.AI.Context;
using EchoStation.AI.Inference;

namespace EchoStation.AI.IntentJson
{
 public sealed class JsonIntentConstraintProvider:ITokenConstraintProvider
 {
  public ITokenConstraint Create(PreparedPrompt prepared)
  {if(prepared==null)throw new ArgumentNullException(nameof(prepared));return new JsonIntentConstraint(prepared.EffectiveTargets);}
 }
}
