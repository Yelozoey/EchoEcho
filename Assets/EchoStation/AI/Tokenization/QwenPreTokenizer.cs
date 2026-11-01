using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EchoStation.AI.Tokenization
{
 public static class QwenPreTokenizer
 {
  public const int MaximumCharacters=32768;
  public const string Pattern=@"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+";
  private static readonly Regex Split=new Regex(Pattern,RegexOptions.Compiled|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
  public static string Normalize(string text)
  {
   if(text==null)throw new ArgumentNullException(nameof(text));if(text.Length>MaximumCharacters)throw new ArgumentException("Tokenizer input exceeds character budget");
   for(var i=0;i<text.Length;i++)
   {
    if(char.IsHighSurrogate(text[i])){if(i+1>=text.Length||!char.IsLowSurrogate(text[++i]))throw new ArgumentException("Unpaired UTF16 surrogate");}
    else if(char.IsLowSurrogate(text[i]))throw new ArgumentException("Unpaired UTF16 surrogate");
   }
   var normalized=text.Normalize(NormalizationForm.FormC);if(normalized.Length>MaximumCharacters)throw new ArgumentException("Normalized input exceeds budget");return normalized;
  }
  public static string[] SplitEncoded(string text)
  {
   var normalized=Normalize(text);if(normalized.Length==0)return Array.Empty<string>();
   var shadow=new StringBuilder(normalized.Length);var offsets=new List<int>(normalized.Length+1);
   for(var index=0;index<normalized.Length;index++)
   {
    offsets.Add(index);var scalar=char.ConvertToUtf32(normalized,index);if(scalar>0xffff)index++;
    // Only ASCII is used by literal alternatives; preserve the exact HF Unicode classes.
    var proxy=scalar<128?(char)scalar:scalar==0x17f?'s':UnicodeClasses.IsLetters(scalar)?'a':UnicodeClasses.IsNumbers(scalar)?'0':UnicodeClasses.IsWhitespace(scalar)?'\t':'@';shadow.Append(proxy);
   }
   offsets.Add(normalized.Length);var input=shadow.ToString();var output=new List<string>();var covered=0;
   foreach(Match match in Split.Matches(input))
   {
    if(match.Index!=covered||match.Length==0)throw new InvalidDataException("Pre-tokenizer did not cover input");
    var start=offsets[match.Index];var end=offsets[match.Index+match.Length];output.Add(ByteAlphabet.Encode(normalized.Substring(start,end-start)));covered+=match.Length;
   }
   if(covered!=input.Length)throw new InvalidDataException("Uncovered tokenizer suffix");return output.ToArray();
  }
 }
}
