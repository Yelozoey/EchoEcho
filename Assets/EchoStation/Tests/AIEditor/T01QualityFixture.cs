using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EchoStation.AI.IntentJson;
using EchoStation.Memory;
using Newtonsoft.Json.Linq;

namespace EchoStation.Tests.AI
{
 internal sealed class T01Case
 {
  public string Id {get;}public string Setup {get;}public string Input {get;}public LlmIntentKind Expected {get;}public string Target {get;}
  public T01Case(string id,string setup,string input,LlmIntentKind expected,string target){Id=id;Setup=setup;Input=input;Expected=expected;Target=target;}
 }
 internal static class T01QualityFixture
 {
  public const string Path="Tools/Stage3/Fixtures/T01_Game100_C.json";
  public const string Sha256="55cfbb8fb89bd94ba6ef9dad2683c20b51edcfdfc45734f0352daff7639bc21b";
  public static IReadOnlyList<T01Case> Load()
  {
   var bytes=File.ReadAllBytes(Path);using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=Sha256)throw new ArgumentException("Frozen T01 fixture hash differs");
   return Parse(new UTF8Encoding(false,true).GetString(bytes));
  }
  public static IReadOnlyList<T01Case> Parse(string json,string prefix="T01_C_")
  {
   if(prefix!="T01_C_"&&prefix!="T01_D_"&&prefix!="T01_E_")throw new ArgumentException("Known frozen fixture prefix required");
   var rows=JArray.Parse(json);if(rows.Count!=100)throw new ArgumentException("Exactly100 T01 cases required");var result=new List<T01Case>();var ids=new HashSet<string>(StringComparer.Ordinal);var inputs=new HashSet<string>(StringComparer.Ordinal);
   foreach(var token in rows)
   {
    var row=token as JObject;if(row==null||row.Count!=5||row.Properties().Any(p=>!new[]{"id","setup","input","expected","target"}.Contains(p.Name)))throw new ArgumentException("Closed T01 fixture row required");
    var id=Text(row,"id");var setup=Text(row,"setup");var input=Text(row,"input");var expected=Text(row,"expected");
    if(id!=prefix+(result.Count+1).ToString("D3")||!ids.Add(id)||!new[]{"valley_empty","cabin_known","lab_known"}.Contains(setup)||string.IsNullOrWhiteSpace(input)||input.Length>200)throw new ArgumentException("Bounded ordered T01 metadata required");
    new UTF8Encoding(false,true).GetByteCount(input);if(!inputs.Add(input.Normalize(NormalizationForm.FormKC).Trim()))throw new ArgumentException("Duplicate T01 input");
    if(!Enum.TryParse(expected,false,out LlmIntentKind kind)||!Enum.IsDefined(typeof(LlmIntentKind),kind)||kind.ToString()!=expected)throw new ArgumentException("Unknown T01 intent label");
    string target=null;if(row["target"].Type==JTokenType.String)target=row.Value<string>("target");else if(row["target"].Type!=JTokenType.Null)throw new ArgumentException("Null or ID target required");
    var targeted=kind==LlmIntentKind.Investigate||kind==LlmIntentKind.Warn;
    if(targeted)
    {
     if(!MemoryLimits.IsValidId(target)||kind==LlmIntentKind.Warn&&target!="power_panel_01")throw new ArgumentException("Scoring target invalid");
     var targetSetup=target=="note_entry_inspection_01"?"valley_empty":target=="scan_mark_01"?"cabin_known":target=="scan_lab_calibration_01"||target=="power_panel_01"?"lab_known":null;if(targetSetup!=setup)throw new ArgumentException("Scoring target not in declared controlled context");
    }
    else if(target!=null)throw new ArgumentException("Non-target intent cannot have scoring target");
    result.Add(new T01Case(id,setup,input,kind,target));
   }
   return result.AsReadOnly();
  }
  private static string Text(JObject row,string name){if(row[name]?.Type!=JTokenType.String)throw new ArgumentException("T01 string field missing");return row.Value<string>(name);}
 }
}
