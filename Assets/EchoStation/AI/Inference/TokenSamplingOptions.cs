using System;
using System.IO;
using EchoStation.AI.Tokenization;

namespace EchoStation.AI.Inference
{
 public sealed class TokenSamplingOptions
 {
  public double Temperature {get;}public int TopK {get;}public double TopP {get;}public uint Seed {get;}
  public TokenSamplingOptions(double temperature=.7,int topK=20,double topP=.8,uint seed=42)
  {if(double.IsNaN(temperature)||double.IsInfinity(temperature)||temperature<=0||temperature>2||topK<1||topK>20||double.IsNaN(topP)||double.IsInfinity(topP)||topP<=0||topP>1||seed==0)throw new ArgumentOutOfRangeException("Bounded non-thinking sampling options");Temperature=temperature;TopK=topK;TopP=topP;Seed=seed;}
 }
 public sealed class TopKTokenSampler
 {
  private readonly TokenSamplingOptions options;private readonly int[] ids;private readonly float[] scores;private readonly double[] weights;private uint state;
  public const string Name="topk-topp-xorshift32-v1";
  public int Draws {get;private set;}
  public TopKTokenSampler(TokenSamplingOptions configuration)
  {options=configuration??throw new ArgumentNullException(nameof(configuration));state=options.Seed;ids=new int[options.TopK];scores=new float[options.TopK];weights=new double[options.TopK];}
  public int Select(ReadOnlySpan<float> logits,QwenByteBpeTokenizer tokenizer,ITokenConstraint constraint=null)
  {
   if(tokenizer==null)throw new ArgumentNullException(nameof(tokenizer));if(logits.Length!=QwenTensorContract.Vocabulary)throw new InvalidDataException("Sampling vocabulary shape differs");var count=0;
   for(var id=0;id<tokenizer.DefinedTokenCount;id++)
   {
    var score=logits[id];if(float.IsNaN(score)||float.IsPositiveInfinity(score))throw new InvalidDataException("Invalid sampling logits");if(float.IsNegativeInfinity(score)||count==options.TopK&&score<=scores[count-1])continue;
    var eos=id==151643||id==151645;if(eos){if(constraint!=null&&!constraint.CanEnd)continue;}else if(tokenizer.IsSpecial(id)||constraint!=null&&!constraint.Allows(id,tokenizer.BytesForToken(id).Span))continue;
    if(count<options.TopK)count++;var at=count-1;while(at>0&&score>scores[at-1]){scores[at]=scores[at-1];ids[at]=ids[at-1];at--;}scores[at]=score;ids[at]=id;
   }
   if(count==0)throw new GenerationException(GenerationFault.NoAllowedToken,"No allowed sampling candidate");double total=0;for(var i=0;i<count;i++){weights[i]=Math.Exp(((double)scores[i]-scores[0])/options.Temperature);total+=weights[i];}
   double kept=0;var nucleus=0;while(nucleus<count){kept+=weights[nucleus++];if(kept>=options.TopP*total)break;}
   state^=state<<13;state^=state>>17;state^=state<<5;Draws++;var draw=((double)state+.5)/4294967296.0*kept;double cumulative=0;for(var i=0;i<nucleus;i++){cumulative+=weights[i];if(draw<cumulative)return ids[i];}return ids[nucleus-1];
  }
 }
}
