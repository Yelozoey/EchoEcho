using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EchoStation.AI.Tokenization;
using EchoStation.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EchoStation.AI.Context
{
 public enum Qwen3PromptLayout{SingleUser,RulesSystem,RulesSystemExamples}
 public sealed class Qwen3PromptProfile
 {
  private const string MemorySlot="{retrieved_memory}",InputSlot="{player_input}";
  private readonly int memoryAt,inputAt;
  public string Id {get;}public string Text {get;}public string Sha256 {get;}public Qwen3PromptLayout Layout {get;}public IReadOnlyList<Qwen3ChatExample> Examples {get;}public string ExamplesSha256 {get;}
  public static Qwen3PromptProfile V5 {get;}=new Qwen3PromptProfile("qwen3_v5",Qwen3PromptTemplate.Text);
  public static Qwen3PromptProfile V6 {get;}=new Qwen3PromptProfile("qwen3_v6",Qwen3V6Prompt.Text);
  public static Qwen3PromptProfile V7 {get;}=new Qwen3PromptProfile("qwen3_v7",Qwen3V7Prompt.Text);
  public static Qwen3PromptProfile V8 {get;}=new Qwen3PromptProfile("qwen3_v8_system",Qwen3V8Prompt.Text,Qwen3PromptLayout.RulesSystem);
  public static Qwen3PromptProfile V9 {get;}=new Qwen3PromptProfile("qwen3_v9_examples",Qwen3V9Prompt.Text,Qwen3PromptLayout.RulesSystemExamples,Qwen3V9Prompt.Examples);
  public static Qwen3PromptProfile V10 {get;}=new Qwen3PromptProfile("qwen3_v10_english",Qwen3V10Prompt.Text,Qwen3PromptLayout.RulesSystemExamples,Qwen3V9Prompt.Examples);
  public static Qwen3PromptProfile V11 {get;}=new Qwen3PromptProfile("qwen3_v11_boundaries",Qwen3V11Prompt.Text,Qwen3PromptLayout.RulesSystemExamples,Qwen3V11Prompt.Examples);
  public static Qwen3PromptProfile Selected=>V5;
  public Qwen3PromptProfile(string id,string text,Qwen3PromptLayout layout=Qwen3PromptLayout.SingleUser,IReadOnlyList<Qwen3ChatExample> examples=null)
  {
   if(!Enum.IsDefined(typeof(Qwen3PromptLayout),layout))throw new ArgumentException("Unknown official prompt layout");
   if(examples!=null&&examples.Count>8)throw new ArgumentException("At most eight trusted examples");var copy=examples?.ToArray()??Array.Empty<Qwen3ChatExample>();if(copy.Any(e=>e==null)||(layout==Qwen3PromptLayout.RulesSystemExamples?copy.Length==0:copy.Length!=0))throw new ArgumentException("Examples must match explicit layout");Examples=Array.AsReadOnly(copy);
   if(!MemoryLimits.IsValidId(id)||string.IsNullOrWhiteSpace(text)||text.Length>16384||text.Contains("\r"))throw new ArgumentException("Canonical bounded Qwen3 prompt profile required");
   var bytes=new UTF8Encoding(false,true).GetBytes(text);if(bytes.Length>16384)throw new ArgumentException("Prompt profile exceeds16KiB");
   memoryAt=text.IndexOf(MemorySlot,StringComparison.Ordinal);inputAt=text.IndexOf(InputSlot,StringComparison.Ordinal);
   if(memoryAt<0||inputAt<=memoryAt||text.IndexOf(MemorySlot,memoryAt+MemorySlot.Length,StringComparison.Ordinal)>=0||text.IndexOf(InputSlot,inputAt+InputSlot.Length,StringComparison.Ordinal)>=0)throw new ArgumentException("Exactly one ordered memory/input placeholder required");
   Id=id;Text=text;Layout=layout;using(var hash=SHA256.Create())Sha256=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
   var exampleJson=new JArray(copy.Select(e=>new JObject{{"user",e.User},{"assistant",e.Assistant}})).ToString(Formatting.None);using(var hash=SHA256.Create())ExamplesSha256=BitConverter.ToString(hash.ComputeHash(new UTF8Encoding(false,true).GetBytes(exampleJson))).Replace("-","").ToLowerInvariant();
  }
  public string Fill(string memory,string input)
  {if(memory==null||input==null)throw new ArgumentNullException("Prompt data");return Text.Substring(0,memoryAt)+memory+Text.Substring(memoryAt+MemorySlot.Length,inputAt-memoryAt-MemorySlot.Length)+input+Text.Substring(inputAt+InputSlot.Length);}
  public int[] Wrap(QwenByteBpeTokenizer tokenizer,string body)
  {
   if(Layout==Qwen3PromptLayout.SingleUser)return Qwen3ChatTemplate.WrapUser(tokenizer,body);
   var header=Text.Substring(0,memoryAt);if(body==null)throw new ArgumentNullException(nameof(body));if(!body.StartsWith(header,StringComparison.Ordinal))throw new ArgumentException("System profile requires its unchanged trusted header");
   return Layout==Qwen3PromptLayout.RulesSystemExamples?Qwen3ChatTemplate.WrapSystemExamplesUser(tokenizer,header,Examples,body.Substring(memoryAt)):Qwen3ChatTemplate.WrapSystemUser(tokenizer,header,body.Substring(memoryAt));
  }
 }
}
