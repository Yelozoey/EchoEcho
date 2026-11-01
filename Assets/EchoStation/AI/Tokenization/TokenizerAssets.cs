using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using EchoStation.AI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EchoStation.AI.Tokenization
{
 public sealed class AddedToken
 {
  public int Id {get;}public string Content {get;}public bool IsSpecial {get;}
  internal AddedToken(int id,string content,bool special){Id=id;Content=content;IsSpecial=special;}
 }
 public sealed class TokenizerAssets
 {
  internal readonly struct MergeRule {public readonly int Rank,Result;public MergeRule(int rank,int result){Rank=rank;Result=result;}}
  private readonly IReadOnlyDictionary<long,MergeRule> pairs;
  private readonly int[] byteIds=new int[512];
  public IReadOnlyDictionary<string,int> Vocabulary {get;}
  public IReadOnlyDictionary<int,string> ById {get;}
  public IReadOnlyDictionary<string,int> MergeRanks {get;}
  public IReadOnlyList<AddedToken> AddedTokens {get;}
  private TokenizerAssets(Dictionary<string,int> vocab,Dictionary<int,string> byId,Dictionary<string,int> ranks,Dictionary<long,MergeRule> mergePairs,AddedToken[] added)
  {Vocabulary=new ReadOnlyDictionary<string,int>(vocab);ById=new ReadOnlyDictionary<int,string>(byId);MergeRanks=new ReadOnlyDictionary<string,int>(ranks);pairs=new ReadOnlyDictionary<long,MergeRule>(mergePairs);AddedTokens=Array.AsReadOnly(added);for(var i=0;i<byteIds.Length;i++)byteIds[i]=-1;for(var b=0;b<256;b++){var c=ByteAlphabet.ForByte((byte)b);byteIds[c]=vocab[c.ToString()];}}
  internal static long PairKey(int left,int right)=>((long)left<<32)|(uint)right;
  internal bool TryMerge(int left,int right,out MergeRule rule)=>pairs.TryGetValue(PairKey(left,right),out rule);
  internal int ByteId(char value){if(value>=byteIds.Length||byteIds[value]<0)throw new InvalidDataException("Invalid encoded byte character");return byteIds[value];}
  public static TokenizerAssets Load(VerifiedModelPackage package)
  {if(package.ReleaseId!=ModelCatalog.Current.Id)throw new InvalidDataException("Tokenizer release differs");return Parse(package.ReadUtf8("tokenizer.json"));}
  public static TokenizerAssets LoadQwen3(VerifiedModelPackage package)
  {if(package==null)throw new ArgumentNullException(nameof(package));if(package.ReleaseId!=Qwen3Catalog.Current.Id)throw new InvalidDataException("Qwen3 tokenizer release differs");return ParseQwen3(package.ReadUtf8("tokenizer.json",16*1024*1024));}
  public static TokenizerAssets Parse(string json)=>ParseCore(json,false);
  public static TokenizerAssets ParseQwen3(string json)=>ParseCore(json,true);
  private static TokenizerAssets ParseCore(string json,bool qwen3)
  {
   if(json==null||json.Length>(qwen3?16:8)*1024*1024)throw new InvalidDataException("Invalid tokenizer asset length");JObject root;
   using(var reader=new JsonTextReader(new StringReader(json)){MaxDepth=16,DateParseHandling=DateParseHandling.None})
   {root=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});if(reader.Read())throw new InvalidDataException("Multiple tokenizer JSON values");}
   Need(root["normalizer"]?["type"]?.Value<string>()=="NFC","NFC normalizer required");
   Need(root["truncation"]?.Type==JTokenType.Null&&root["padding"]?.Type==JTokenType.Null,"External padding/truncation is forbidden");
   var pre=root["pre_tokenizer"]?["pretokenizers"] as JArray;Need(root["pre_tokenizer"]?["type"]?.Value<string>()=="Sequence"&&pre?.Count==2,"Unknown pre-tokenizer sequence");
   Need(pre[0]["type"]?.Value<string>()=="Split"&&pre[0]["pattern"]?["Regex"]?.Value<string>()==QwenPreTokenizer.Pattern&&pre[0]["behavior"]?.Value<string>()=="Isolated"&&pre[0]["invert"]?.Value<bool>()==false,"Unsupported split rules");
   ByteLevel(pre[1]);ByteLevel(root["decoder"]);ByteLevel(root["post_processor"]);
   var model=root["model"] as JObject;Need(model!=null&&model["type"]?.Value<string>()=="BPE","BPE model required");
   Need(model["dropout"]?.Type==JTokenType.Null&&model["unk_token"]?.Type==JTokenType.Null&&model["fuse_unk"]?.Value<bool>()==false&&model["byte_fallback"]?.Value<bool>()==false&&model["continuing_subword_prefix"]?.Value<string>()==""&&model["end_of_word_suffix"]?.Value<string>()=="","Unsupported BPE options");
   var vocabObject=model["vocab"] as JObject;var merges=model["merges"] as JArray;Need(vocabObject?.Count==151643&&merges?.Count==151387,"Pinned tokenizer inventory differs");
   var vocab=new Dictionary<string,int>(151643,StringComparer.Ordinal);var byId=new Dictionary<int,string>(qwen3?151669:151665);
   foreach(var property in vocabObject.Properties())
   {
    Need(property.Value.Type==JTokenType.Integer,"Integer vocabulary ID required");var id=property.Value.Value<int>();Need(id>=0&&id<151643&&!byId.ContainsKey(id),"Duplicate/out-of-range vocabulary ID");
    ByteAlphabet.DecodeBytes(property.Name);vocab.Add(property.Name,id);byId.Add(id,property.Name);
   }
   for(var value=0;value<256;value++)Need(vocab.ContainsKey(ByteAlphabet.ForByte((byte)value).ToString()),"Byte vocabulary coverage missing");
   var ranks=new Dictionary<string,int>(151387,StringComparer.Ordinal);var mergePairs=new Dictionary<long,MergeRule>(151387);
   for(var rank=0;rank<merges.Count;rank++)
   {
    string rule;string[] pair;
    if(qwen3)
    {var value=merges[rank] as JArray;Need(value?.Count==2&&value.All(t=>t.Type==JTokenType.String),"Qwen3 pair-array merge rule required");pair=value.Values<string>().ToArray();rule=pair[0]+" "+pair[1];}
    else{Need(merges[rank].Type==JTokenType.String,"String merge rule required");rule=merges[rank].Value<string>();pair=rule.Split(' ');}
    Need(pair.Length==2&&vocab.ContainsKey(pair[0])&&vocab.ContainsKey(pair[1])&&vocab.ContainsKey(pair[0]+pair[1])&&!ranks.ContainsKey(rule),"Invalid or repeated BPE merge");ranks.Add(rule,rank);mergePairs.Add(PairKey(vocab[pair[0]],vocab[pair[1]]),new MergeRule(rank,vocab[pair[0]+pair[1]]));
   }
   var addedArray=root["added_tokens"] as JArray;Need(addedArray?.Count==(qwen3?26:22),"Pinned added-token inventory differs");var added=new List<AddedToken>();var seen=new HashSet<string>(StringComparer.Ordinal);
   foreach(var token in addedArray)
   {
    Need(token["id"]?.Type==JTokenType.Integer&&token["content"]?.Type==JTokenType.String&&token["special"]?.Type==JTokenType.Boolean,"Invalid added token");
    var id=token["id"].Value<int>();var content=token["content"].Value<string>();Need(id>=151643&&id<=(qwen3?151668:151664)&&!byId.ContainsKey(id)&&content.Length>0&&seen.Add(content),"Invalid added-token identity");
    foreach(var flag in new[]{"single_word","lstrip","rstrip","normalized"})Need(token[flag]?.Value<bool>()==false,"Unsupported added-token matching");
    byId.Add(id,content);added.Add(new AddedToken(id,content,token["special"].Value<bool>()));
   }
   Need(byId[151643]=="<|endoftext|>"&&byId[151644]=="<|im_start|>"&&byId[151645]=="<|im_end|>","Chat separator identity differs");
   Need(added.Count(t=>t.IsSpecial)==14&&added.Where(t=>t.Id<=151645).All(t=>t.IsSpecial),"Special-token flags differ");
   if(qwen3)
   {
    var markers=new[]{"<tool_response>","</tool_response>","<think>","</think>"};
    for(var i=0;i<markers.Length;i++)Need(byId[151665+i]==markers[i]&&!added.Single(t=>t.Id==151665+i).IsSpecial,"Qwen3 ordinary marker identity differs");
   }
   return new TokenizerAssets(vocab,byId,ranks,mergePairs,added.ToArray());
  }
  private static void ByteLevel(JToken value)=>Need(value?["type"]?.Value<string>()=="ByteLevel"&&value["add_prefix_space"]?.Value<bool>()==false&&value["trim_offsets"]?.Value<bool>()==false&&value["use_regex"]?.Value<bool>()==false,"Unsupported ByteLevel configuration");
  private static void Need(bool value,string detail){if(!value)throw new InvalidDataException(detail);}
 }
}
