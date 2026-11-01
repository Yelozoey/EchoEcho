using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EchoStation.AI.Context;
using EchoStation.AI.IntentJson;
using EchoStation.AI.Tokenization;
using EchoStation.Salvage;
using Newtonsoft.Json.Linq;

namespace EchoStation.SalvageNext.Tests
{
    public static class ExperimentArtifacts
    {
        public const string Root="Docs/Tests/SalvagePrototype/Evidence/R3-L01";
        public const string Inputs=Root+"/Inputs";
        public static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        public static JObject ReadFrozen(string key)
        {
            var receipt=JObject.Parse(File.ReadAllText(Inputs+"/Freeze.json"))[key];var path=receipt["path"].Value<string>();
            var full=Path.GetFullPath(path);if(!full.StartsWith(Path.GetFullPath(Inputs)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Frozen input path escaped root.");
            var bytes=File.ReadAllBytes(full);if(Hash(bytes)!=receipt["sha256"].Value<string>())throw new ArgumentException("Frozen input hash changed.");
            return JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
        }
        public static Qwen3PromptProfile Profile(JObject source)=>new Qwen3PromptProfile(source["id"].Value<string>(),source["text"].Value<string>(),(Qwen3PromptLayout)Enum.Parse(typeof(Qwen3PromptLayout),source["layout"].Value<string>(),false),
            source["examples"].Select(e=>new Qwen3ChatExample(e["user"].Value<string>(),e["assistant"].Value<string>())).ToArray());
        public static RecoveryKnowledge Knowledge(JObject row)
        {
            var states=(JObject)row["states"];var delivered=states.Properties().Where(p=>p.Value.Value<int>()==2).ToArray();
            var observations=row["known"].Values<string>().Select(id=>{
                SalvageTargetCatalog.TryGet(id,out var target);if(target==null)throw new ArgumentException("Unknown fixture ID.");
                CargoState? state=target.Kind==SalvageTargetKind.Cargo?(CargoState)(states[id]?.Value<int>()??0):(CargoState?)null;
                double? path=state.HasValue&&state!=CargoState.World?null:(double?)Array.IndexOf(SalvageTargetCatalog.Items.Select(t=>t.Id).ToArray(),id)+4;
                return new ObservedTarget(id,true,state,path.HasValue,path);
            }).ToArray();
            return new RecoveryKnowledge(1,1,RecoveryPreference.Nearby,delivered.Length,delivered.Sum(p=>SalvageCatalog.Get(p.Name).Points),observations);
        }
        public static InferenceSnapshot Snapshot(JObject row,RecoveryKnowledge knowledge)=>RecoveryContext.Snapshot(knowledge,row["input"].Value<string>(),0,row["pointed"]?.Type==JTokenType.String?row["pointed"].Value<string>():null);
        public static JObject Grade(JObject row,string raw)
        {
            var parse=IntentJsonParser.Parse(raw);bool strict=parse.Success,correct=false;
            if(strict)
            {
                var intent=parse.Intent;var expected=row["intent"].Value<string>();var target=intent.TargetName??intent.DangerTarget;
                correct=intent.Kind.ToString().ToLowerInvariant()==expected&&(row["target"].Type==JTokenType.Null||target==row["target"].Value<string>());
                var requirements=row["requiredAny"].Values<string>().ToArray();
                if(requirements.Length>0)correct&=requirements.Any(term=>(intent.Content??"").Contains(term));
                if(expected=="answer"&&row["input"].Value<string>().Contains("高分")&&((JObject)row["states"]).Count>0)
                {
                    var choice=Knowledge(row).Suggestions(RecoveryPreference.HighScore).FirstOrDefault();
                    correct&=choice==null?true:(intent.Content??"").Contains(choice.Id)||(intent.Content??"").Contains(choice.Name.Substring(2));
                }
                correct&=!new[]{"门已打开","已经打开门","已经交付","已搬到","已完成交付"}.Any(term=>(intent.Content??"").Contains(term));
            }
            return new JObject{{"strict",strict},{"taskCorrect",correct},{"parseFailure",parse.Failure.ToString()}};
        }
    }
}
