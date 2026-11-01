using System;
using System.Collections;
using System.IO;
using System.Threading;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.L01;
using EchoStation.L02;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace EchoStation.L02Q01.Tests
{
    public class RealQualityTests
    {
        const string Folder="Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/L02Q01";SceneQuestionSession running;string activeLabel;
        [UnityTest,Timeout(480000),Category("BoundedRealModelExperiment")]public IEnumerator Diagnostic20(){yield return Run("Diagnostic");}
        [UnityTest,Timeout(480000),Category("BoundedRealModelExperiment")]public IEnumerator HeldOut20(){Assert.That((bool)JObject.Parse(File.ReadAllText(Folder+"/Diagnostic_Quality.json"))["passed"],Is.True);yield return Run("HeldOut");}
        IEnumerator Run(string label)
        {
            var inputs=JArray.Parse(File.ReadAllText(Folder+"/"+label+"_Inputs.json"));Assert.That(inputs.Count,Is.EqualTo(20));var path=Folder+"/"+label+"_Raw.json";Assert.That(File.Exists(path),Is.False);var claim=Folder+"/"+label+"_RunClaim.json";Assert.That(File.Exists(claim),Is.False,"No retry or second consumption of this experiment");
            var claimData=new JObject{{"profile",SceneQuestionPromptV2.Profile.Id},{"promptSha256",SceneQuestionPromptV2.Profile.Sha256},{"examplesSha256",SceneQuestionPromptV2.Profile.ExamplesSha256},{"requestsAccepted",0}};using(var writer=new StreamWriter(new FileStream(claim,FileMode.CreateNew,FileAccess.Write)))writer.Write(claimData.ToString());activeLabel=label;
            var factory=new LocalQwen3Factory(Path.Combine(Application.streamingAssetsPath,"EchoLLM","Qwen3_06B_Q4"),new JsonIntentConstraintProvider(),promptProfile:SceneQuestionPromptV2.Profile);int main=Thread.CurrentThread.ManagedThreadId;running=new SceneQuestionSession(factory,main);var rows=new JArray();running.SetEnabled(true);
            foreach(JObject input in inputs)
            {
                var known=new EchoTextContext(new[]{new EchoTextTarget((string)input["target"],(string)input["name"],true,(string)input["report"])},(string)input["target"],90);var capture=SceneQuestion.Select((string)input["question"],known,(string)input["target"],"case").Capture;Assert.That(capture,Is.Not.Null);
                var timer=System.Diagnostics.Stopwatch.StartNew();Admission admission;
                do{running.Poll(known,"case");admission=running.Submit(capture);if(admission==Admission.Accepted)break;Assert.That(running.Enabled,Is.True,"AI disabled; never silently restart");Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(35));yield return null;}while(true);
                claimData["requestsAccepted"]=(int)claimData["requestsAccepted"]+1;File.WriteAllText(claim,claimData.ToString());timer.Restart();QuestionReply reply=null;
                while(true){running.Poll(known,"case");if(running.TryTake(out reply))break;Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(25));yield return null;}
                var g=reply.Generation;rows.Add(new JObject{{"id",input["id"]},{"question",input["question"]},{"source",input["report"]},{"expected",input["expected"]},{"raw",reply.Raw},{"guardAccepted",reply.Success},{"error",reply.Error},{"wallSeconds",timer.Elapsed.TotalSeconds},{"ownerThread",running.OwnerThreadId},{"mainThread",main},{"preparationThread",g?.PreparationThreadId},{"inputTokens",g?.Prepared?.InputIds.Count},{"memoryTokens",g?.Prepared?.MemoryTokens},{"memoryBytes",g?.Prepared?.MemoryUtf8Bytes},{"usedMemory",g?.Prepared?.UsedMemory}});File.WriteAllText(path,rows.ToString());
            }
            Assert.That(rows.Count,Is.EqualTo(20));
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(running!=null){running.Dispose();var timer=System.Diagnostics.Stopwatch.StartNew();while(!running.CleanupComplete){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(10));yield return null;}if(activeLabel!=null)File.WriteAllText(Folder+"/"+activeLabel+"_Cleanup.json",new JObject{{"complete",running.CleanupComplete},{"profile",SceneQuestionPromptV2.Profile.Id},{"promptSha256",SceneQuestionPromptV2.Profile.Sha256},{"examplesSha256",SceneQuestionPromptV2.Profile.ExamplesSha256}}.ToString());running=null;}
        }
    }
}
