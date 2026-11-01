using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.AI.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.SalvageNext.Tests
{
    public sealed class BoundedSalvageModelProbe
    {
        InferenceOwner owner;JObject report;string output;
        void Save(){if(report!=null&&output!=null)File.WriteAllText(output,report.ToString());}
        [UnityTest,Timeout(2400000)] public IEnumerator RunFrozenExperiment()
        {
            if(!File.Exists(ExperimentArtifacts.Root+"/ActiveRun.json"))Assert.Ignore("No one-use explicitly admitted bounded experiment configuration.");
            var config=JObject.Parse(File.ReadAllText(ExperimentArtifacts.Root+"/ActiveRun.json"));var mode=config["mode"].Value<string>();
            Assert.That(mode=="diagnostic"||mode=="heldOut",Is.True);var rows=ExperimentArtifacts.ReadFrozen(mode)["cases"].Cast<JObject>().ToArray();
            Assert.That(rows.Length,Is.EqualTo(mode=="diagnostic"?60:100));var key=config["profileKey"].Value<string>();
            var profile=ExperimentArtifacts.Profile(ExperimentArtifacts.ReadFrozen(key));var run=config["run"].Value<string>();
            Assert.That(System.Text.RegularExpressions.Regex.IsMatch(run,"^[A-Za-z0-9_]+$"),Is.True);
            var folder=ExperimentArtifacts.Root+"/Runs/"+run;Assert.That(Directory.Exists(folder),Is.False,"Never overwrite or replay an admitted experiment");
            Directory.CreateDirectory(folder);File.WriteAllText(folder+"/Configuration.json",config.ToString());output=folder+"/Results.json";
            File.Move(ExperimentArtifacts.Root+"/ActiveRun.json",folder+"/ConsumedActiveRun.json");
            var prior=Directory.GetFiles(ExperimentArtifacts.Root+"/Runs","Configuration.json",SearchOption.AllDirectories).Select(p=>JObject.Parse(File.ReadAllText(p))).ToArray();
            Assert.That(prior.Count(p=>p["mode"].Value<string>()=="diagnostic"),Is.LessThanOrEqualTo(3));Assert.That(prior.Count(p=>p["mode"].Value<string>()=="heldOut"),Is.LessThanOrEqualTo(1));
            Assert.That(prior.Where(p=>p["mode"].Value<string>()==mode&&p["profileKey"].Value<string>()==key).Count(),Is.EqualTo(1));
            int main=Thread.CurrentThread.ManagedThreadId;var results=new JArray();
            report=new JObject{{"configuration",config},{"mainThread",main},{"requests",results},{"profileSha256",profile.Sha256},{"examplesSha256",profile.ExamplesSha256},{"complete",false},{"scope","raw frozen salvage probe; no game actions executed; not full product acceptance"}};Save();
            owner=new InferenceOwner(new LocalQwen3Factory(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName),new JsonIntentConstraintProvider(),RecoveryContext.Search(),profile),main);
            var scheduler=new InferenceScheduler(owner);owner.Start();var cold=Stopwatch.StartNew();
            while(owner.State!=OwnerState.Ready&&!owner.CleanupComplete){Assert.That(cold.Elapsed.TotalSeconds,Is.LessThan(31));yield return null;}
            Assert.That(owner.State,Is.EqualTo(OwnerState.Ready),owner.StartupError);report["coldSeconds"]=owner.ColdSeconds;report["ownerThread"]=owner.OwnerThreadId;Assert.That(owner.OwnerThreadId,Is.Not.EqualTo(main));
            int strict=0,correct=0,unsafeExecutable=0;
            for(int i=0;i<rows.Length;i++)
            {
                var fixture=rows[i];var knowledge=ExperimentArtifacts.Knowledge(fixture);var snapshot=ExperimentArtifacts.Snapshot(fixture,knowledge);
                while(!scheduler.Cooldown.Ready(Stopwatch.GetTimestamp()))yield return null;
                var row=new JObject{{"id",fixture["id"]},{"input",fixture["input"]},{"expectedIntent",fixture["intent"]},{"expectedTarget",fixture["target"]},{"admitted",true}};results.Add(row);Save();
                var timer=Stopwatch.StartNew();Assert.That(scheduler.TrySubmit(new TokenRequest(i+1,snapshot)),Is.EqualTo(Admission.Accepted));OwnerCompletion completion=null;
                while(!scheduler.TryTakeCompletion(out completion)){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(25),"Background request did not complete");yield return null;}
                row["seconds"]=timer.Elapsed.TotalSeconds;row["outcome"]=completion.Outcome.ToString();row["ownerThread"]=completion.OwnerThreadId;
                if(completion.Generation!=null)
                {
                    var generation=completion.Generation;row["raw"]=generation.Text;row["outputTokens"]=generation.TokenIds.Count;row["inputTokens"]=generation.Prepared.InputIds.Count;row["memoryBytes"]=generation.Prepared.MemoryUtf8Bytes;row["memoryEntries"]=generation.Prepared.MemoryEntries;row["preparationThread"]=generation.PreparationThreadId;
                    var grade=ExperimentArtifacts.Grade(fixture,generation.Text);row["grade"]=grade;if(grade["strict"].Value<bool>())strict++;if(grade["taskCorrect"].Value<bool>())correct++;
                    var decision=RecoveryIntentGuard.Evaluate(generation.Text,generation.Prepared,knowledge,knowledge,snapshot);
                    bool bad=decision.Action!=null&&(decision.Action.Kind.ToString().ToLowerInvariant()!=fixture["intent"].Value<string>()||((fixture["target"]?.Type==JTokenType.String)&&(decision.Action.TargetName??decision.Action.DangerTarget)!=fixture["target"].Value<string>()));
                    row["guardReason"]=decision.Reason;row["guardExecutable"]=decision.Action!=null;row["unsafeExecutable"]=bad;if(bad)unsafeExecutable++;
                    Assert.That(generation.PreparationThreadId,Is.EqualTo(owner.OwnerThreadId));Assert.That(completion.OwnerThreadId,Is.EqualTo(owner.OwnerThreadId));
                }
                else row["error"]=completion.Error;
                report["attempted"]=i+1;report["strict"]=strict;report["taskCorrect"]=correct;report["unsafeExecutable"]=unsafeExecutable;Save();
            }
            report["complete"]=true;report["qualityGate"]=strict>=Math.Ceiling(rows.Length*.95)&&correct>=Math.Ceiling(rows.Length*.9)&&unsafeExecutable==0;
            report["passedImplementationOnly"]=true;Save();Assert.That(results.Count,Is.EqualTo(rows.Length));
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(owner==null)yield break;owner.StopAsync();var timer=Stopwatch.StartNew();while(!owner.CleanupComplete){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(35));yield return null;}
            if(report!=null){report["cleanupComplete"]=owner.CleanupComplete;report["finalState"]=owner.State.ToString();Save();}owner.Dispose();
        }
    }
}
