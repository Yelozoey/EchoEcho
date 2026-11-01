using System;
using System.Collections;
using System.IO;
using System.Threading;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.L01;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace EchoStation.L02.Tests
{
    public class RealSceneQuestionTests
    {
        const string Folder="Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/L02";
        [UnityTest,Timeout(480000)]public IEnumerator Diagnostic20(){yield return Run("Diagnostic");}
        [UnityTest,Timeout(480000)]public IEnumerator HeldOut20(){Assert.That((bool)JObject.Parse(File.ReadAllText(Folder+"/Diagnostic_Quality.json"))["passed"],Is.True);yield return Run("HeldOut");}
        IEnumerator Run(string label)
        {
            var inputs=JArray.Parse(File.ReadAllText(Folder+"/"+label+"_Inputs.json"));Assert.That(inputs.Count,Is.EqualTo(20));var path=Folder+"/"+label+"_Raw.json";Assert.That(File.Exists(path),Is.False,"Never reconsume or overwrite actual model evidence");
            var factory=new LocalQwen3Factory(Path.Combine(Application.streamingAssetsPath,"EchoLLM","Qwen3_06B_Q4"),new JsonIntentConstraintProvider(),promptProfile:SceneQuestionPrompt.Profile);var main=Thread.CurrentThread.ManagedThreadId;var session=new SceneQuestionSession(factory,main);var rows=new JArray();
            try
            {
                session.SetEnabled(true);
                foreach(JObject input in inputs)
                {
                    var known=new EchoTextContext(new[]{new EchoTextTarget((string)input["target"],(string)input["name"],true,(string)input["report"])},(string)input["target"],90);var capture=SceneQuestion.Select((string)input["question"],known,(string)input["target"],"case").Capture;Assert.That(capture,Is.Not.Null);
                    var timer=System.Diagnostics.Stopwatch.StartNew();Admission admission;
                    do{session.Poll(known,"case");admission=session.Submit(capture);if(admission==Admission.Accepted)break;Assert.That(session.Enabled,Is.True,"AI auto-disabled; do not silently restart");Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(35));yield return null;}while(true);
                    timer.Restart();QuestionReply reply=null;while(true){session.Poll(known,"case");if(session.TryTake(out reply))break;Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(25));yield return null;}
                    var g=reply.Generation;rows.Add(new JObject{{"id",input["id"]},{"question",input["question"]},{"source",input["report"]},{"expected",input["expected"]},{"raw",reply.Raw},{"guardAccepted",reply.Success},{"error",reply.Error},{"wallSeconds",timer.Elapsed.TotalSeconds},{"ownerThread",session.OwnerThreadId},{"mainThread",main},{"preparationThread",g?.PreparationThreadId},{"inputTokens",g?.Prepared?.InputIds.Count},{"memoryTokens",g?.Prepared?.MemoryTokens},{"memoryBytes",g?.Prepared?.MemoryUtf8Bytes},{"usedMemory",g?.Prepared?.UsedMemory}});File.WriteAllText(path,rows.ToString());
                }
            }
            finally{session.Dispose();}
            var cleanup=System.Diagnostics.Stopwatch.StartNew();while(!session.CleanupComplete){Assert.That(cleanup.Elapsed.TotalSeconds,Is.LessThan(10));yield return null;}
            File.WriteAllText(Folder+"/"+label+"_Cleanup.json",new JObject{{"complete",session.CleanupComplete},{"profile",SceneQuestionPrompt.Profile.Id},{"promptSha256",SceneQuestionPrompt.Profile.Sha256},{"requests",rows.Count}}.ToString());Assert.That(rows.Count,Is.EqualTo(20));
        }
    }
}
