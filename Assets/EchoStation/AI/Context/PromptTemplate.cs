using System;
using EchoStation.AI.Tokenization;

namespace EchoStation.AI.Context
{
 public static partial class PromptTemplate
 {
  public const string OfficialSystem="You are Qwen, created by Alibaba Cloud. You are a helpful assistant.";
  public static string Fill(string memory,string input)
  {
   if(memory==null||input==null)throw new ArgumentNullException("Prompt placeholder");
   const string m="{retrieved_memory}",p="{player_input}";var mi=Text.IndexOf(m,StringComparison.Ordinal);var pi=Text.IndexOf(p,StringComparison.Ordinal);
   return Text.Substring(0,mi)+memory+Text.Substring(mi+m.Length,pi-mi-m.Length)+input+Text.Substring(pi+p.Length);
  }
  public static int[] Wrap(QwenByteBpeTokenizer tokenizer,string body)
  {
   if(body==null)throw new ArgumentNullException(nameof(body));return new TrustedTokenSequence(tokenizer).Separator(ChatSeparator.Start).Text("system\n"+OfficialSystem).Separator(ChatSeparator.End).Text("\n").Separator(ChatSeparator.Start).Text("user\n"+body).Separator(ChatSeparator.End).Text("\n").Separator(ChatSeparator.Start).Text("assistant\n").ToArray();
  }
 }
}
