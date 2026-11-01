using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EchoStation.Memory;

namespace EchoStation.AI.Authorization
{
 public sealed class TrustedTargetIdentity
 {
  public string Id {get;}public string Name {get;}public IReadOnlyList<string> Aliases {get;}
  internal IReadOnlyList<string> SearchNames {get;}
  public TrustedTargetIdentity(string id,string name,params string[] aliases)
  {
   if(!MemoryLimits.IsValidId(id)||aliases==null||aliases.Length>16)throw new ArgumentException("Invalid bounded target identity");
   var normalizedName=TargetNameMatcher.Normalize(name,80,160);var copy=aliases.Select(alias=>TargetNameMatcher.Normalize(alias,80,160)).Distinct(StringComparer.Ordinal).ToArray();
   Id=id;Name=name;Aliases=Array.AsReadOnly(copy);SearchNames=Array.AsReadOnly(new[]{normalizedName}.Concat(copy).Distinct(StringComparer.Ordinal).ToArray());
  }
 }
 public sealed class TargetNameMatch
 {
  public IReadOnlyList<string> Ids {get;}public bool IsAmbiguous=>Ids.Count>1;public string UniqueId=>Ids.Count==1?Ids[0]:null;
  internal TargetNameMatch(string[] ids){Ids=Array.AsReadOnly(ids);}
 }
 public static class TargetNameMatcher
 {
  internal static string Normalize(string value,int maximum,int normalizedMaximum)
  {
   if(string.IsNullOrWhiteSpace(value)||value.Length>maximum)throw new ArgumentException("Invalid bounded target text");
   try{new UTF8Encoding(false,true).GetByteCount(value);}catch(EncoderFallbackException error){throw new ArgumentException("Invalid target UTF16",error);}
   var normalized=value.Normalize(NormalizationForm.FormKC).ToLowerInvariant().Trim();
   if(normalized.Length==0||normalized.Length>normalizedMaximum)throw new ArgumentException("Normalized target text exceeds budget");return normalized;
  }
  public static TargetNameMatch Match(string input,IEnumerable<TrustedTargetIdentity> catalog)
  {
   var value=Normalize(input,200,400);if(catalog==null)throw new ArgumentException("Target catalog missing");
   var ids=new HashSet<string>(StringComparer.Ordinal);var matched=new List<string>();var count=0;
   foreach(var target in catalog)
   {
    if(++count>128||target==null||!ids.Add(target.Id))throw new ArgumentException("Target catalog exceeds budget or repeats IDs");
    if(Mentions(value,target.Id,true)||target.SearchNames.Any(name=>Mentions(value,name,name.All(c=>c<128))))matched.Add(target.Id);
   }
   return new TargetNameMatch(matched.ToArray());
  }
  private static bool Mentions(string input,string term,bool boundary)
  {
   var offset=0;while(offset<=input.Length-term.Length)
   {
    var index=input.IndexOf(term,offset,StringComparison.Ordinal);if(index<0)return false;var end=index+term.Length;
    if(!boundary||(index==0||!AsciiWord(input[index-1]))&&(end==input.Length||!AsciiWord(input[end])))return true;offset=index+1;
   }
   return false;
  }
  private static bool AsciiWord(char value)=>value>='a'&&value<='z'||value>='0'&&value<='9'||value=='_';
 }
}
