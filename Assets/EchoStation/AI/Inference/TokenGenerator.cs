using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using EchoStation.AI.Tokenization;
using EchoStation.AI.Context;

namespace EchoStation.AI.Inference
{
 public interface ITokenModel
 {ReadOnlySpan<float> Logits {get;}void Prefill(IReadOnlyList<int> ids);void Step(int token);void ResetCache();}
 public interface ITokenConstraint
 {string Name {get;}bool CanEnd {get;}bool Allows(int id,ReadOnlySpan<byte> bytes);void Accept(int id,ReadOnlySpan<byte> bytes);}
 public interface ITokenConstraintProvider{ITokenConstraint Create(PreparedPrompt prepared);}
 public enum GenerationStop {Eos,TokenLimit}
 public enum GenerationFault {Timeout,OutputBudget,NoAllowedToken,InvalidUtf8,InvalidContext}
 public sealed class GenerationException:Exception
 {public GenerationFault Code {get;}public GenerationException(GenerationFault code,string detail,Exception inner=null):base(detail,inner){Code=code;}}
 public sealed class GenerationResult
 {
  public string Text {get;}public IReadOnlyList<int> TokenIds {get;}public GenerationStop Stop {get;}public double Seconds {get;}public string Mode {get;}
  public PreparedPrompt Prepared {get;}public int PreparationThreadId {get;}public double PreparationSeconds {get;}
  public TokenSamplingOptions SamplingOptions {get;}public string SamplingMode=>SamplingOptions==null?"greedy":TopKTokenSampler.Name;
  public bool Complete=>Stop==GenerationStop.Eos;
  internal GenerationResult(string text,List<int> ids,GenerationStop stop,double seconds,string mode,PreparedPrompt prepared=null,int preparationThread=0,double preparationSeconds=0,TokenSamplingOptions samplingOptions=null){Text=text;TokenIds=Array.AsReadOnly(ids.ToArray());Stop=stop;Seconds=seconds;Mode=mode;Prepared=prepared;PreparationThreadId=preparationThread;PreparationSeconds=preparationSeconds;SamplingOptions=samplingOptions;}
 }
 public sealed class TokenGenerator
 {
  private readonly ITokenModel model;private readonly QwenByteBpeTokenizer tokenizer;private readonly Func<double> now;
  public TokenGenerator(ITokenModel backend,QwenByteBpeTokenizer vocabulary,Func<double> monotonicSeconds=null)
  {model=backend??throw new ArgumentNullException(nameof(backend));tokenizer=vocabulary??throw new ArgumentNullException(nameof(vocabulary));now=monotonicSeconds??(()=>Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency);}
  public GenerationResult Generate(IReadOnlyList<int> input,CancellationToken cancellation=default,int maximumNewTokens=256,double timeoutSeconds=20,ITokenConstraint constraint=null,TokenSamplingOptions sampling=null)
  {
   QwenTensorContract.ValidateInput(input);if(maximumNewTokens<1||maximumNewTokens>256||double.IsNaN(timeoutSeconds)||double.IsInfinity(timeoutSeconds)||timeoutSeconds<=0||timeoutSeconds>20)throw new ArgumentOutOfRangeException("Generation budget");
   var started=now();var generated=new List<int>(maximumNewTokens);var textIds=new List<int>(maximumNewTokens);var byteCount=0;
   try
   {
    var sampler=sampling==null?null:new TopKTokenSampler(sampling);
    Check(cancellation,started,timeoutSeconds);model.Prefill(input);Check(cancellation,started,timeoutSeconds);
    for(var step=0;step<maximumNewTokens;step++)
    {
     Check(cancellation,started,timeoutSeconds);var logits=model.Logits;if(logits.Length!=QwenTensorContract.Vocabulary)throw new InvalidDataException("Generation vocabulary shape differs");
     var token=sampler==null?Select(logits,constraint):sampler.Select(logits,tokenizer,constraint);Check(cancellation,started,timeoutSeconds);var eos=token==151643||token==151645;generated.Add(token);
     if(eos)return Complete(textIds,generated,GenerationStop.Eos,now()-started,constraint,sampling);
     var bytes=tokenizer.BytesForToken(token);if(bytes.Length>4096-byteCount)throw new GenerationException(GenerationFault.OutputBudget,"Raw output exceeds4KiB");
     constraint?.Accept(token,bytes.Span);byteCount+=bytes.Length;textIds.Add(token);
     if(step+1==maximumNewTokens)return Complete(textIds,generated,GenerationStop.TokenLimit,now()-started,constraint,sampling);
     model.Step(token);Check(cancellation,started,timeoutSeconds);
    }
    throw new InvalidOperationException("Unreachable generation state");
   }
   finally{model.ResetCache();}
  }
  private int Select(ReadOnlySpan<float> logits,ITokenConstraint constraint)
  {
   var best=-1;var value=float.NegativeInfinity;
   for(var id=0;id<tokenizer.DefinedTokenCount;id++)
   {
    var score=logits[id];if(float.IsNaN(score)||float.IsPositiveInfinity(score))throw new InvalidDataException("Invalid generation logits");if(score<=value)continue;
    var eos=id==151643||id==151645;if(eos){if(constraint!=null&&!constraint.CanEnd)continue;}
    else if(tokenizer.IsSpecial(id)||(constraint!=null&&!constraint.Allows(id,tokenizer.BytesForToken(id).Span)))continue;
    best=id;value=score;
   }
   if(best<0)throw new GenerationException(GenerationFault.NoAllowedToken,"No valid token remains");return best;
  }
  private GenerationResult Complete(List<int> text,List<int> generated,GenerationStop stop,double seconds,ITokenConstraint constraint,TokenSamplingOptions sampling)
  {
   try{return new GenerationResult(tokenizer.Decode(text,strictUtf8:true),generated,stop,seconds,constraint?.Name??(sampling==null?"greedy":TopKTokenSampler.Name),samplingOptions:sampling);}
   catch(System.Text.DecoderFallbackException error){throw new GenerationException(GenerationFault.InvalidUtf8,"Model output ends with incomplete UTF8",error);}
  }
  private void Check(CancellationToken cancellation,double start,double timeout){cancellation.ThrowIfCancellationRequested();if(now()-start>=timeout)throw new GenerationException(GenerationFault.Timeout,"Inference logical deadline exceeded");}
 }
}
