using System;
using EchoStation.AI.Tokenization;

namespace EchoStation.AI.Context
{
 public static partial class Qwen3PromptTemplate
 {
  public static string Fill(string memory,string input)
  {
   if(memory==null||input==null)throw new ArgumentNullException("Prompt placeholder");
   const string m="{retrieved_memory}",p="{player_input}";var mi=Text.IndexOf(m,StringComparison.Ordinal);var pi=Text.IndexOf(p,StringComparison.Ordinal);
   return Text.Substring(0,mi)+memory+Text.Substring(mi+m.Length,pi-mi-m.Length)+input+Text.Substring(pi+p.Length);
  }
  public static int[] Wrap(QwenByteBpeTokenizer tokenizer,string body)=>Qwen3ChatTemplate.WrapUser(tokenizer,body);
 }
}
