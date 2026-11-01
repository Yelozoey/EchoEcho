using System;
using System.IO;
using System.Linq;
using System.Text;
using EchoStation.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EchoStation.AI.IntentJson
{
 public enum LlmIntentKind{Follow,Investigate,Wait,Warn,Answer,Refuse}
 public enum IntentJsonFailure{None,Empty,TooLarge,Encoding,Json,Shape,Intent,Target,Content}
 public sealed class StructuredIntent
 {
  public LlmIntentKind Kind {get;}public string TargetName {get;}public string DangerTarget {get;}public string Content {get;}
  internal StructuredIntent(LlmIntentKind kind,string target,string danger,string content){Kind=kind;TargetName=target;DangerTarget=danger;Content=content;}
 }
 public sealed class IntentJsonResult
 {
  public bool Success=>Failure==IntentJsonFailure.None;public IntentJsonFailure Failure {get;}public StructuredIntent Intent {get;}
  internal IntentJsonResult(IntentJsonFailure failure,StructuredIntent intent=null){Failure=failure;Intent=intent;}
 }
 public static class IntentJsonParser
 {
  public const string FallbackLine="抱歉，我没能理解你的指令，请换一种说法。";
  private static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
  public static IntentJsonResult Parse(string raw)
  {
   if(string.IsNullOrWhiteSpace(raw))return Fail(IntentJsonFailure.Empty);if(raw.Length>4096)return Fail(IntentJsonFailure.TooLarge);
   try{if(Utf8.GetByteCount(raw)>4096)return Fail(IntentJsonFailure.TooLarge);}catch(EncoderFallbackException){return Fail(IntentJsonFailure.Encoding);}
   JObject root;
   try
   {
    MemoryJson.ValidateLexemes(raw);ValidateEscapedUnicode(raw);
    using(var reader=new JsonTextReader(new StringReader(raw)){MaxDepth=8,DateParseHandling=DateParseHandling.None})
    {root=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});if(reader.Read())return Fail(IntentJsonFailure.Json);}
   }
   catch(Exception error)when(error is JsonException||error is ArgumentException){return Fail(IntentJsonFailure.Json);}
   if(root["intent"]?.Type!=JTokenType.String)return Fail(IntentJsonFailure.Intent);var name=root["intent"].Value<string>();LlmIntentKind kind;string target=null,danger=null,content=null;string[] fields;
   switch(name)
   {
    case "follow":kind=LlmIntentKind.Follow;fields=new[]{"intent"};break;
    case "wait":kind=LlmIntentKind.Wait;fields=new[]{"intent"};break;
    case "investigate":kind=LlmIntentKind.Investigate;fields=new[]{"intent","targetName","content"};target=String(root,"targetName");break;
    case "warn":kind=LlmIntentKind.Warn;fields=new[]{"intent","dangerTarget","content"};danger=String(root,"dangerTarget");break;
    case "answer":kind=LlmIntentKind.Answer;fields=new[]{"intent","content"};break;
    case "refuse":kind=LlmIntentKind.Refuse;fields=new[]{"intent","content"};break;
    default:return Fail(IntentJsonFailure.Intent);
   }
   if(root.Count!=fields.Length||root.Properties().Any(p=>Array.IndexOf(fields,p.Name)<0))return Fail(IntentJsonFailure.Shape);
   if(kind==LlmIntentKind.Investigate&&!MemoryLimits.IsValidId(target)||kind==LlmIntentKind.Warn&&!MemoryLimits.IsValidId(danger))return Fail(IntentJsonFailure.Target);
   if(kind!=LlmIntentKind.Follow&&kind!=LlmIntentKind.Wait)
   {content=String(root,"content");if(string.IsNullOrWhiteSpace(content)||content.Length>280)return Fail(IntentJsonFailure.Content);try{Utf8.GetByteCount(content);}catch(EncoderFallbackException){return Fail(IntentJsonFailure.Encoding);}}
   return new IntentJsonResult(IntentJsonFailure.None,new StructuredIntent(kind,target,danger,content));
  }
  private static string String(JObject obj,string key)=>obj[key]?.Type==JTokenType.String?obj[key].Value<string>():null;
  private static IntentJsonResult Fail(IntentJsonFailure reason)=>new IntentJsonResult(reason);
  private static void ValidateEscapedUnicode(string text)
  {
   var quoted=false;for(var i=0;i<text.Length;i++)
   {
    var c=text[i];if(c=='"'){quoted=!quoted;continue;}if(!quoted||c!='\\')continue;c=text[++i];if(c!='u')continue;
    var unit=Hex(text,i+1);i+=4;if(unit>=0xd800&&unit<=0xdbff)
    {if(i+6>=text.Length||text[i+1]!='\\'||text[i+2]!='u')throw new ArgumentException("Unpaired high surrogate escape");var low=Hex(text,i+3);if(low<0xdc00||low>0xdfff)throw new ArgumentException("Invalid surrogate pair");i+=6;}
    else if(unit>=0xdc00&&unit<=0xdfff)throw new ArgumentException("Unpaired low surrogate escape");
   }
  }
  private static int Hex(string text,int at){if(at+4>text.Length)throw new ArgumentException("Short Unicode escape");return Convert.ToInt32(text.Substring(at,4),16);}
 }
}
