using System;
using System.IO;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using EchoStation.Memory;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.SalvageNext.Tests
{
    public sealed class ExperimentArtifactTests
    {
        [Test] public void FrozenCountsIdentitiesAndDisjointInputsMatchProtocol()
        {
            var d=ExperimentArtifacts.ReadFrozen("diagnostic")["cases"];var h=ExperimentArtifacts.ReadFrozen("heldOut")["cases"];
            Assert.That(d.Count(),Is.EqualTo(60));Assert.That(h.Count(),Is.EqualTo(100));
            Assert.That(d.Concat(h).Select(r=>r["id"].Value<string>()).Distinct().Count(),Is.EqualTo(160));
            Assert.That(d.Select(r=>r["input"].Value<string>()).Intersect(h.Select(r=>r["input"].Value<string>())),Is.Empty);
            foreach(var row in d.Concat(h)){Assert.That(row["input"].Value<string>().Length,Is.InRange(1,200));Assert.DoesNotThrow(()=>ExperimentArtifacts.Knowledge((JObject)row));}
            Assert.That(d.GroupBy(r=>r["intent"].Value<string>()).OrderBy(g=>g.Key).Select(g=>g.Count()),Is.EqualTo(new[]{12,8,14,10,8,8}));
            Assert.That(h.GroupBy(r=>r["intent"].Value<string>()).OrderBy(g=>g.Key).Select(g=>g.Count()),Is.EqualTo(new[]{25,12,24,15,12,12}));
        }
        [Test] public void RawGradingDoesNotCountScriptCorrectionsOrGenericAdvice()
        {
            var row=(JObject)ExperimentArtifacts.ReadFrozen("diagnostic")["cases"][38];Assert.That(row["intent"].Value<string>(),Is.EqualTo("answer"));
            Assert.That(ExperimentArtifacts.Grade(row,"{\"intent\":\"follow\"}")["taskCorrect"].Value<bool>(),Is.False);
            Assert.That(ExperimentArtifacts.Grade(row,"{\"intent\":\"answer\",\"content\":\"我会提供建议。\"}")["taskCorrect"].Value<bool>(),Is.False);
            Assert.That(ExperimentArtifacts.Grade(row,"{\"intent\":\"answer\",\"content\":\"建议cargo_e，请确认。\"}")["taskCorrect"].Value<bool>(),Is.True);
            Assert.That(ExperimentArtifacts.Grade(row,"{\"intent\":\"answer\",\"intent\":\"follow\"}")["strict"].Value<bool>(),Is.False);
        }
        [TestCase("candidate1")][TestCase("candidate2")][TestCase("candidate3")]
        public void CandidateUsesOfficialWrapperAndEveryDiagnosticKeepsBudgetsAndNeededAdvice(string profileKey)
        {
            var tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));
            var profile=ExperimentArtifacts.Profile(ExperimentArtifacts.ReadFrozen(profileKey));int maximum=0,minMemory=int.MaxValue;
            foreach(JObject row in ExperimentArtifacts.ReadFrozen("diagnostic")["cases"])
            {
                var knowledge=ExperimentArtifacts.Knowledge(row);var snapshot=ExperimentArtifacts.Snapshot(row,knowledge);
                var retrieved=new ContextAssembler(new MemoryContextBuilder(RecoveryContext.Search()),tokenizer).Build(snapshot);
                var prompt=new Qwen3PromptPreparation(tokenizer,profile).Build(retrieved);
                maximum=Math.Max(maximum,prompt.InputIds.Count);minMemory=Math.Min(minMemory,prompt.MemoryEntries);
                Assert.That(prompt.InputIds.Count,Is.LessThanOrEqualTo(1536));Assert.That(prompt.InputIds.Count+256,Is.LessThanOrEqualTo(2048));
                Assert.That(prompt.MemoryUtf8Bytes,Is.LessThanOrEqualTo(1200));Assert.That(prompt.MemoryEntries,Is.LessThanOrEqualTo(8));
                if(row["id"].Value<string>()=="D60_039")Assert.That(prompt.UsedMemory,Does.Contain("首选cargo_e"));
                Assert.That(prompt.InputIds.Count(id=>id==151644),Is.EqualTo(profile.Layout==Qwen3PromptLayout.SingleUser?2:19)); // official wrappers
            }
            Directory.CreateDirectory(ExperimentArtifacts.Root+"/Static");File.WriteAllText(ExperimentArtifacts.Root+"/Static/"+profileKey+"_Budget.json",new JObject{{"maxInputTokens",maximum},{"minimumMemoryEntries",minMemory},{"cases",60},{"inferenceExecuted",false},{"profileSha256",profile.Sha256},{"examplesSha256",profile.ExamplesSha256}}.ToString());
        }
        [Test] public void UnknownCaseDoesNotGainKnownSummaryAndHeldCargoCannotBeSuggested()
        {
            var rows=ExperimentArtifacts.ReadFrozen("heldOut")["cases"].Cast<JObject>().ToArray();
            var unknown=rows.First(r=>!r["known"].Any());Assert.That(ExperimentArtifacts.Knowledge(unknown).Facts,Is.Empty);
            foreach(var row in rows.Where(r=>((JObject)r["states"]).Count>0))Assert.That(ExperimentArtifacts.Knowledge(row).Suggestions(RecoveryPreference.HighScore)[0].Id,Is.EqualTo("cargo_d"));
        }
    }
}
