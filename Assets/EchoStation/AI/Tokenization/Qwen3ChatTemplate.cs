using System;
using System.Collections.Generic;
using System.Text;

namespace EchoStation.AI.Tokenization
{
 public sealed class Qwen3ChatExample
 {
  public string User {get;}public string Assistant {get;}
  public Qwen3ChatExample(string user,string assistant)
  {if(string.IsNullOrWhiteSpace(user)||user.Length>200||string.IsNullOrWhiteSpace(assistant)||assistant.Length>512)throw new ArgumentException("Bounded trusted chat example required");var utf8=new UTF8Encoding(false,true);utf8.GetByteCount(user);utf8.GetByteCount(assistant);User=user;Assistant=assistant;}
 }
 // Trusted wrappers matching the pinned official enable_thinking=false template.
 public static class Qwen3ChatTemplate
 {
  public static int[] WrapSystemExamplesUser(QwenByteBpeTokenizer tokenizer,string system,IReadOnlyList<Qwen3ChatExample> examples,string user)
  {
   if(tokenizer==null)throw new ArgumentNullException(nameof(tokenizer));if(tokenizer.DefinedTokenCount!=151669)throw new ArgumentException("Qwen3 tokenizer profile required");
   if(system==null||user==null||examples==null)throw new ArgumentNullException("Official chat contents");if(examples.Count<1||examples.Count>8)throw new ArgumentException("One to eight trusted examples required");
   var sequence=new TrustedTokenSequence(tokenizer).Separator(ChatSeparator.Start).Text("system\n"+system).Separator(ChatSeparator.End).Text("\n");
   foreach(var item in examples)
   {if(item==null)throw new ArgumentException("Missing trusted example");sequence.Separator(ChatSeparator.Start).Text("user\n"+item.User).Separator(ChatSeparator.End).Text("\n").Separator(ChatSeparator.Start).Text("assistant\n"+item.Assistant).Separator(ChatSeparator.End).Text("\n");}
   return sequence.Separator(ChatSeparator.Start).Text("user\n"+user).Separator(ChatSeparator.End).Text("\n").Separator(ChatSeparator.Start).Text("assistant\n<think>\n\n</think>\n\n").ToArray();
  }
  public static int[] WrapSystemUser(QwenByteBpeTokenizer tokenizer,string system,string user)
  {
   if(tokenizer==null)throw new ArgumentNullException(nameof(tokenizer));
   if(tokenizer.DefinedTokenCount!=151669)throw new ArgumentException("Qwen3 tokenizer profile required");
   if(system==null||user==null)throw new ArgumentNullException("Official chat contents");
   return new TrustedTokenSequence(tokenizer).Separator(ChatSeparator.Start).Text("system\n"+system)
    .Separator(ChatSeparator.End).Text("\n").Separator(ChatSeparator.Start).Text("user\n"+user)
    .Separator(ChatSeparator.End).Text("\n").Separator(ChatSeparator.Start)
    .Text("assistant\n<think>\n\n</think>\n\n").ToArray();
  }
  public static int[] WrapUser(QwenByteBpeTokenizer tokenizer,string body)
  {
   if(tokenizer==null)throw new ArgumentNullException(nameof(tokenizer));
   if(tokenizer.DefinedTokenCount!=151669)throw new ArgumentException("Qwen3 tokenizer profile required");
   if(body==null)throw new ArgumentNullException(nameof(body));
   return new TrustedTokenSequence(tokenizer).Separator(ChatSeparator.Start).Text("user\n"+body)
    .Separator(ChatSeparator.End).Text("\n").Separator(ChatSeparator.Start)
    .Text("assistant\n<think>\n\n</think>\n\n").ToArray();
  }
 }
}
