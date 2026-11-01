using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EchoStation.AI.Tokenization;
using EchoStation.Memory;
using Newtonsoft.Json.Linq;

namespace EchoStation.AI.Context
{
 public sealed class RetrievedContext
 {
  public InferenceSnapshot Snapshot {get;}public IReadOnlyList<string> Entries {get;}public string MemoryText {get;}public int MemoryUtf8Bytes {get;}public int MemoryTokens {get;}public string TargetsJson {get;}public int TargetTokens {get;}public int DroppedEntries {get;}
  internal RetrievedContext(InferenceSnapshot snapshot,string[] entries,string memory,int bytes,int tokens,string targets,int targetTokens,int dropped)
  {Snapshot=snapshot;Entries=Array.AsReadOnly(entries);MemoryText=memory;MemoryUtf8Bytes=bytes;MemoryTokens=tokens;TargetsJson=targets;TargetTokens=targetTokens;DroppedEntries=dropped;}
 }
 public sealed class ContextAssembler
 {
  private readonly MemoryContextBuilder retrieval;private readonly QwenByteBpeTokenizer tokenizer;private static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
  public ContextAssembler(MemoryContextBuilder builder,QwenByteBpeTokenizer vocabulary){retrieval=builder??throw new ArgumentNullException(nameof(builder));tokenizer=vocabulary??throw new ArgumentNullException(nameof(vocabulary));}
  public RetrievedContext Build(InferenceSnapshot snapshot)
  {
   if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));var slice=retrieval.Build(snapshot.Known,snapshot.RetrievalQuery,snapshot.ElapsedGameSeconds);
   var secrets=snapshot.Known.Clues.Where(c=>c.Password!=null).Select(c=>c.Password).Distinct(StringComparer.Ordinal).OrderByDescending(p=>p.Length).ToArray();
   var entries=slice.Entries.Select(e=>Redact(e.Text,secrets)).ToList();var originalCount=entries.Count;string memory;int bytes,tokens;
   for(;;)
   {
    memory=string.Join("\n",entries);bytes=Utf8.GetByteCount(memory);tokens=tokenizer.EncodePlain(memory).Length;
    if(bytes<=1200&&tokens<=512)break;entries.RemoveAt(entries.Count-1);
   }
   var targets=new JArray();foreach(var target in snapshot.Targets)
   {
    var actions=new JArray();if(target.CanInvestigate)actions.Add("investigate");if(target.IsDanger)actions.Add("warn");
    targets.Add(new JObject{{"id",target.Id},{"name",Redact(target.Name,secrets)},{"actions",actions}});
   }
   var json=targets.ToString(Newtonsoft.Json.Formatting.None);return new RetrievedContext(snapshot,entries.ToArray(),memory,bytes,tokens,json,tokenizer.EncodePlain(json).Length,originalCount-entries.Count);
  }
  private static string Redact(string text,IEnumerable<string> secrets)
  {foreach(var secret in secrets)text=text.Replace(secret,"[密码已归档]");return text;}
 }
}
