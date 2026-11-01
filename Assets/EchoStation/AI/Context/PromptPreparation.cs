using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EchoStation.AI.Tokenization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EchoStation.AI.Context
{
 public sealed class ContextBudgetException:Exception{public ContextBudgetException(string detail):base(detail){}}
 public sealed class PreparedPrompt
 {
  public InferenceSnapshot Snapshot {get;}public string Body {get;}public IReadOnlyList<int> InputIds {get;}public IReadOnlyList<AllowedTarget> EffectiveTargets {get;}public string UsedMemory {get;}public int MemoryEntries {get;}public int MemoryTokens {get;}public int MemoryUtf8Bytes {get;}
  internal PreparedPrompt(InferenceSnapshot snapshot,string body,int[] ids,AllowedTarget[] targets,string memory,int count,int tokens)
  {Snapshot=snapshot;Body=body;InputIds=Array.AsReadOnly(ids);EffectiveTargets=Array.AsReadOnly(targets);UsedMemory=memory;MemoryEntries=count;MemoryTokens=tokens;MemoryUtf8Bytes=new UTF8Encoding(false,true).GetByteCount(memory);}
 }
 public sealed class PromptPreparation
 {
  private readonly QwenByteBpeTokenizer tokenizer;
  public PromptPreparation(QwenByteBpeTokenizer vocabulary){tokenizer=vocabulary??throw new ArgumentNullException(nameof(vocabulary));}
  public PreparedPrompt Build(RetrievedContext retrieved)
  {
   if(retrieved==null)throw new ArgumentNullException(nameof(retrieved));var entries=retrieved.Entries.ToList();var targets=JArray.Parse(retrieved.TargetsJson);var count=targets.Count;
   for(;;)
   {
    var memory=string.Join("\n",entries);var context="相关记忆（摘要）：\n"+memory+"\n允许目标（ID/名称/允许行为）：\n"+targets.ToString(Formatting.None);var body=PromptTemplate.Fill(context,retrieved.Snapshot.Input);var ids=PromptTemplate.Wrap(tokenizer,body);
    if(ids.Length<=1536)
    {
     if(ids.Length+256>2048)throw new ContextBudgetException("Input and generation reserve exceed context");
     return new PreparedPrompt(retrieved.Snapshot,body,ids,retrieved.Snapshot.Targets.Take(count).ToArray(),memory,entries.Count,tokenizer.EncodePlain(memory).Length);
    }
    if(entries.Count>0){entries.RemoveAt(entries.Count-1);continue;}if(count>0){targets.RemoveAt(--count);continue;}throw new ContextBudgetException("Minimum fixed prompt and player input exceed1536 tokens");
   }
  }
 }
}
