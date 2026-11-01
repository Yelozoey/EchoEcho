using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EchoStation.Dialogue;
using EchoStation.Memory;
using EchoStation.Puzzles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace EchoStation.Persistence
{
 public enum StationReadStatus{Loaded,Backup,Missing,Invalid,UnsupportedVersion,IoError}
 public sealed class StationReadResult
 {
  public StationReadStatus Status{get;}public StationSaveData Data{get;}public string Detail{get;}public bool Success=>Status==StationReadStatus.Loaded||Status==StationReadStatus.Backup;
  public StationReadResult(StationReadStatus status,StationSaveData data=null,string detail=""){Status=status;Data=data;Detail=detail;}
 }
 public static class StationSaveJson
 {
  public const int MaximumBytes=2*1024*1024;public const string Format="echostation.station";private static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
  public static string Encode(StationSaveData s)
  {
   var error=StationSaveAudit.Validate(s);if(error!=null)throw new ArgumentException(error);
   var p=s.Progress;var progress=new JObject{["completed"]=new JArray(p.Completed),["journal"]=new JArray(p.Journal),["scans"]=new JArray(p.Scans),["zone"]=p.Zone,["access"]=p.AccessLevel,["cabinOpen"]=p.CabinOpen,["coreOpen"]=p.CoreOpen,["ventOpen"]=p.VentOpen,["powered"]=p.Powered,["ventElapsed"]=p.VentElapsed};
   var messages=new JArray();foreach(var m in s.Dialogue)messages.Add(new JObject{["number"]=m.Number,["speaker"]=m.Speaker==DialogueSpeaker.Player?"player":"echo",["text"]=m.Text,["refusal"]=m.IsRefusal});
   var payload=new JObject{["scene"]="station_s2",["player"]=Pose(s.Player),["echo"]=Pose(s.Echo),["echoMode"]=s.EchoMode,["flashlight"]=s.Flashlight,["elapsed"]=s.Elapsed,["progress"]=progress,["memory"]=JObject.Parse(s.MemoryJson),["dialogue"]=messages};
   var json=new JObject{["format"]=Format,["version"]=1,["payload"]=payload,["sha256"]=Hash(payload.ToString(Formatting.None))}.ToString(Formatting.None);if(Utf8.GetByteCount(json)>MaximumBytes)throw new ArgumentException("Station save exceeds byte budget.");return json;
  }
  public static StationReadResult Decode(string json)
  {
   try{
    if(string.IsNullOrWhiteSpace(json)||Utf8.GetByteCount(json)>MaximumBytes)return Invalid("empty_or_over_budget");MemoryJson.ValidateLexemes(json);JObject root;
    using(var reader=new JsonTextReader(new StringReader(json)){MaxDepth=24,DateParseHandling=DateParseHandling.None}){root=JToken.ReadFrom(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error}) as JObject;if(root==null||reader.Read())return Invalid("one_object_required");}
    if(Text(root,"format")!=Format)return Invalid("unknown_format");var version=root["version"];if(version==null||version.Type!=JTokenType.Integer)return Invalid("invalid_version");var v=version.ToString(Formatting.None);if(v!="1")return v!="0"&&!v.StartsWith("-",StringComparison.Ordinal)?new StationReadResult(StationReadStatus.UnsupportedVersion,detail:"未来版本存档已保留。"):Invalid("invalid_version");
    Fields(root,"format","version","payload","sha256");var data=Obj(root["payload"]);if(Text(root,"sha256")!=Hash(data.ToString(Formatting.None)))return Invalid("checksum_mismatch");Fields(data,"scene","player","echo","echoMode","flashlight","elapsed","progress","memory","dialogue");if(Text(data,"scene")!="station_s2")return Invalid("wrong_scene");
    var memory=Obj(data["memory"]).ToString(Formatting.None);var mr=MemoryJson.Decode(memory);if(!mr.Success)return Invalid("invalid_memory");var p=Obj(data["progress"]);Fields(p,"completed","journal","scans","zone","access","cabinOpen","coreOpen","ventOpen","powered","ventElapsed");
    var progress=new ProgressState{Completed=Ids(p,"completed",6),Journal=Ids(p,"journal",128),Scans=Ids(p,"scans",128),Events=mr.Store.Events.Select(e=>e.Id).ToArray(),Zone=Text(p,"zone"),AccessLevel=Int(p,"access"),CabinOpen=Bool(p,"cabinOpen"),CoreOpen=Bool(p,"coreOpen"),VentOpen=Bool(p,"ventOpen"),Powered=Bool(p,"powered"),VentElapsed=(float)Number(p,"ventElapsed")};
    var history=Arr(data,"dialogue",50).Select(token=>{var m=Obj(token);Fields(m,"number","speaker","text","refusal");var speaker=Text(m,"speaker");if(speaker!="player"&&speaker!="echo")throw new ArgumentException("invalid_speaker");if(m["number"].Type!=JTokenType.Integer)throw new ArgumentException("integer_number_required");return new DialogueMessage(m["number"].Value<long>(),speaker=="player"?DialogueSpeaker.Player:DialogueSpeaker.Echo,Text(m,"text"),Bool(m,"refusal"));}).ToArray();
    var result=new StationSaveData{Player=ReadPose(Obj(data["player"])),Echo=ReadPose(Obj(data["echo"])),EchoMode=Text(data,"echoMode"),Flashlight=Bool(data,"flashlight"),Elapsed=Number(data,"elapsed"),Progress=progress,MemoryJson=memory,Dialogue=history};var error=StationSaveAudit.Validate(result);return error==null?new StationReadResult(StationReadStatus.Loaded,result):Invalid(error);
   }catch(Exception e)when(e is JsonException||e is ArgumentException||e is FormatException||e is OverflowException){return Invalid(e.Message);}
  }
  private static JObject Pose(SavedPose p)=>new JObject{["x"]=p.X,["y"]=p.Y,["z"]=p.Z,["yaw"]=p.Yaw,["pitch"]=p.Pitch};
  private static SavedPose ReadPose(JObject p){Fields(p,"x","y","z","yaw","pitch");return new SavedPose((float)Number(p,"x"),(float)Number(p,"y"),(float)Number(p,"z"),(float)Number(p,"yaw"),(float)Number(p,"pitch"));}
  private static StationReadResult Invalid(string why)=>new StationReadResult(StationReadStatus.Invalid,detail:why);
  private static JObject Obj(JToken token)=>token as JObject??throw new ArgumentException("object_required");
  private static void Fields(JObject obj,params string[] names){if(obj.Count!=names.Length||names.Any(n=>obj.Property(n,StringComparison.Ordinal)==null))throw new ArgumentException("missing_or_unknown_field");}
  private static JArray Arr(JObject obj,string name,int cap){var a=obj[name]as JArray;if(a==null||a.Count>cap)throw new ArgumentException("invalid_array");return a;}
  private static string[] Ids(JObject obj,string name,int cap)=>Arr(obj,name,cap).Select(t=>t.Type==JTokenType.String?t.Value<string>():throw new ArgumentException("string_id_required")).ToArray();
  private static string Text(JObject obj,string name)=>obj[name]?.Type==JTokenType.String?obj[name].Value<string>():throw new ArgumentException("string_required");
  private static bool Bool(JObject obj,string name)=>obj[name]?.Type==JTokenType.Boolean?obj[name].Value<bool>():throw new ArgumentException("bool_required");
  private static int Int(JObject obj,string name)=>obj[name]?.Type==JTokenType.Integer?obj[name].Value<int>():throw new ArgumentException("integer_required");
  private static double Number(JObject obj,string name)=>obj[name]?.Type==JTokenType.Integer||obj[name]?.Type==JTokenType.Float?obj[name].Value<double>():throw new ArgumentException("number_required");
  private static string Hash(string s){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Utf8.GetBytes(s))).Replace("-","").ToLowerInvariant();}
 }
}
