using System;
using System.Linq;
using EchoStation.AI.Tokenization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EchoStation.AI.Context
{
 public sealed class Qwen3PromptPreparation
 {
  private readonly QwenByteBpeTokenizer tokenizer;private readonly Qwen3PromptProfile profile;
  public Qwen3PromptPreparation(QwenByteBpeTokenizer vocabulary,Qwen3PromptProfile promptProfile=null)
  {tokenizer=vocabulary??throw new ArgumentNullException(nameof(vocabulary));if(tokenizer.DefinedTokenCount!=151669)throw new ArgumentException("Qwen3 tokenizer required");profile=promptProfile??Qwen3PromptProfile.Selected;}
  public PreparedPrompt Build(RetrievedContext retrieved)
  {
   if(retrieved==null)throw new ArgumentNullException(nameof(retrieved));var entries=retrieved.Entries.ToList();var targets=JArray.Parse(retrieved.TargetsJson);var count=targets.Count;
   for(;;)
   {
    var memory=string.Join("\n",entries);var context="相关记忆（摘要）：\n"+memory+"\n允许目标（ID/名称/允许行为）：\n"+targets.ToString(Formatting.None);
    var body=profile.Fill(context,retrieved.Snapshot.Input);var ids=profile.Wrap(tokenizer,body);
    if(ids.Length<=1536)
    {if(ids.Length+256>2048)throw new ContextBudgetException("Qwen3 input and output reserve exceed context");return new PreparedPrompt(retrieved.Snapshot,body,ids,retrieved.Snapshot.Targets.Take(count).ToArray(),memory,entries.Count,tokenizer.EncodePlain(memory).Length);}
    if(entries.Count>0){entries.RemoveAt(entries.Count-1);continue;}if(count>0){targets.RemoveAt(--count);continue;}
    throw new ContextBudgetException("Minimum Qwen3 prompt exceeds1536 tokens");
   }
  }
 }
}
