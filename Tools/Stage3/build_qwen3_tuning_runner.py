"""Create a separate experiment fixture; preserve the rejected A fixture/evidence."""
from pathlib import Path
import hashlib

root=Path(__file__).resolve().parents[2]
original=root/'Assets/EchoStation/Tests/AIEditor/Qwen3CandidateJsonTests.cs'
target=root/'Assets/EchoStation/Tests/AIEditor/Qwen3TuningTests.cs'
assert not target.exists(),'Preserve existing runner'
source=original.read_text(encoding='utf-8')
def replace(before,after):
    global source
    assert source.count(before)==1,'Expected unique anchor: '+before[:100]
    source=source.replace(before,after)

explicit=next(line for line in source.splitlines() if line.lstrip().startswith('[Explicit('))
replace(explicit,' // Deliberate experiment runner. ActiveRun.json is a fresh archived experiment configuration.')
replace('public sealed class Qwen3CandidateJsonTests','public sealed class Qwen3TuningTests')
replace('public int Done,Ready,Owner,Strict,Semantic;public string Error;public bool Clean;',
        'public int Done,Ready,Owner,Strict,Semantic,Total,Exact;public string Error,Mode;public bool Clean;')
replace('[UnityTest,Timeout(600000)]','[UnityTest,Timeout(2400000)]')
replace('Candidate18RealConstrainedResponsesMeetThePredeclaredContinueGate','ActiveExperimentMeetsItsDeclaredGate')
replace('Evidence+"/UnityJson_"','Evidence+"/Tuning/UnityTuning_"')
replace('clock.Elapsed.TotalSeconds<590','clock.Elapsed.TotalSeconds<2390')
replace('Assert.That(work.Strict,Is.EqualTo(18),"Structure is independent of semantics; inspect raw: "+output);\n    Assert.That(work.Semantic,Is.GreaterThanOrEqualTo(17),"Predeclared candidate continue gate, not final T01 acceptance: "+output);',
'''Assert.That(work.Strict,Is.EqualTo(work.Total),"Structure is independent of semantics; inspect raw: "+output);
    if(work.Mode=="baseline_v2")
    {Assert.That(work.Total,Is.EqualTo(18));Assert.That(work.Exact,Is.EqualTo(18),"Optimized checks must preserve every A output token");Assert.That(work.Semantic,Is.EqualTo(15),"Baseline reproduces the rejected result; it is not a quality pass");}
    else if(work.Mode=="smoke18"){Assert.That(work.Total,Is.EqualTo(18));Assert.That(work.Semantic,Is.GreaterThanOrEqualTo(17),"Unchanged candidate gate");}
    else {Assert.That(work.Mode,Is.EqualTo("validation100"));Assert.That(work.Total,Is.EqualTo(100));Assert.That(work.Semantic,Is.GreaterThanOrEqualTo(90),"Unchanged independent validation gate");}''')
replace('var caseBytes=File.ReadAllBytes(Evidence+"/CandidateCases.json");report["caseSha256"]=Hash(caseBytes);var source=JObject.Parse(Encoding.UTF8.GetString(caseBytes));',
'''var baseCaseBytes=File.ReadAllBytes(Evidence+"/CandidateCases.json");
    var configBytes=File.ReadAllBytes(Evidence+"/Tuning/ActiveRun.json");var config=JObject.Parse(Encoding.UTF8.GetString(configBytes));report["configSha256"]=Hash(configBytes);report["configuration"]=config.DeepClone();
    work.Mode=config["mode"].Value<string>();Need(work.Mode=="baseline_v2"||work.Mode=="smoke18"||work.Mode=="validation100","Unsupported experiment mode");
    var casePath=Path.GetFullPath(config["caseFile"].Value<string>());var tuningRoot=Path.GetFullPath(Evidence+"/Tuning")+Path.DirectorySeparatorChar;Need(casePath.StartsWith(tuningRoot,StringComparison.OrdinalIgnoreCase),"Experiment cases must stay in tuning evidence");
    var caseBytes=File.ReadAllBytes(casePath);report["caseSha256"]=Hash(caseBytes);Need(report["caseSha256"].Value<string>()==config["caseSha256"].Value<string>(),"Experiment case hash differs");var source=JObject.Parse(Encoding.UTF8.GetString(caseBytes));''')
replace('Need(source["promptSha256"].Value<string>()==PromptSha,"Approved v2 source differs");var cases=(JArray)source["cases"];Need(cases.Count==18,"Locked 18 cases required");',
'''Need(source["promptSha256"].Value<string>()==config["promptSha256"].Value<string>(),"Experiment prompt SHA differs");
    var promptPath=Path.GetFullPath(config["promptFile"].Value<string>());Need(promptPath.StartsWith(tuningRoot,StringComparison.OrdinalIgnoreCase)&&Hash(File.ReadAllBytes(promptPath))==config["promptSha256"].Value<string>(),"Prompt bytes differ");
    var cases=(JArray)source["cases"];work.Total=cases.Count;Need(cases.Count==(work.Mode=="validation100"?100:18),"Fixed experiment case count");
    var baseline=JObject.Parse(File.ReadAllText(Evidence+"/UnityJson_20261026T1355217488364Z_25156104e17440a3a90ec0157f06a10d.json"));var baselineRows=(JArray)baseline["requests"];
    if(work.Mode=="baseline_v2")Need(source["promptSha256"].Value<string>()==PromptSha&&Hash(caseBytes)==Hash(baseCaseBytes),"Baseline inputs must be exact original v2");''')
replace('binding["candidateCasesSha256"].Value<string>()==report["caseSha256"].Value<string>()',
        'binding["candidateCasesSha256"].Value<string>()==Hash(baseCaseBytes)')
replace('var started=Stopwatch.StartNew();var before=backend.Calls;',
        'var started=Stopwatch.StartNew();var before=backend.Calls;var nativeBefore=backend.NativeRunSeconds;var validationBefore=backend.ValidationSeconds;')
replace('row["raw"]=generated.Text;row["outputIds"]=new JArray(generated.TokenIds);',
'''row["raw"]=generated.Text;row["outputIds"]=new JArray(generated.TokenIds);
      if(work.Mode=="baseline_v2"){var exact=baselineRows[index]["input"].Value<string>()==snapshot.Input&&generated.TokenIds.SequenceEqual(baselineRows[index]["outputIds"].Values<int>());row["exactBaselineTokens"]=exact;if(exact)work.Exact++;}''')
replace('row["nativeRunThread"]=backend.LastRunThread;',
        'row["nativeRunThread"]=backend.LastRunThread;row["nativeRunSeconds"]=backend.NativeRunSeconds-nativeBefore;row["validationSeconds"]=backend.ValidationSeconds-validationBefore;')
replace('report["allRequestsAttempted"]=rows.Count==18;',
        'report["allRequestsAttempted"]=rows.Count==work.Total;report["baselineTokensExact"]=work.Exact;report["gateScope"]=work.Mode=="baseline_v2"?"Reproduction only; semantic acceptance NOT evaluated":"Candidate smoke or independent validation; not final game acceptance";')
replace('report["nativeCalls"]=backend.CallsAfterDispose;',
        'report["nativeRunSecondsTotal"]=backend.NativeRunSeconds;report["validationSecondsTotal"]=backend.ValidationSeconds;report["nativeCalls"]=backend.CallsAfterDispose;')
replace('public int LastRunThread {get;private set;}public int MainNativeCalls {get;private set;}',
'''public int LastRunThread {get;private set;}public int MainNativeCalls {get;private set;}
   private long nativeRunTicks,validationTicks;public double NativeRunSeconds=>nativeRunTicks/(double)Stopwatch.Frequency;public double ValidationSeconds=>validationTicks/(double)Stopwatch.Frequency;''')
replace('calls++;LastRunThread=Thread.CurrentThread.ManagedThreadId;if(LastRunThread==main)MainNativeCalls++;next=session.Run(options,inputs,feeds,outputs);CollectionsCreated++;var nextValues=next.ToArray();Need(nextValues.Length==57,"Candidate runtime output count");',
'''calls++;LastRunThread=Thread.CurrentThread.ManagedThreadId;if(LastRunThread==main)MainNativeCalls++;
      var runStarted=Stopwatch.GetTimestamp();try{next=session.Run(options,inputs,feeds,outputs);}finally{nativeRunTicks+=Stopwatch.GetTimestamp()-runStarted;}
      CollectionsCreated++;var validationStarted=Stopwatch.GetTimestamp();var nextValues=next.ToArray();Need(nextValues.Length==57,"Candidate runtime output count");''')
replace('for(var n=0;n<tensor.Length;n++)Need(!float.IsNaN(tensor[n])&&!float.IsInfinity(tensor[n]),"Candidate nonfinite tensor");',
        'for(var n=0;n<tensor.Length;n++)if(float.IsNaN(tensor[n])||float.IsInfinity(tensor[n]))throw new InvalidDataException("Candidate nonfinite tensor");')
replace('ReleaseOutput();ownedOutputs=next;values=nextValues;next=null;consumed=total;',
        'validationTicks+=Stopwatch.GetTimestamp()-validationStarted;ReleaseOutput();ownedOutputs=next;values=nextValues;next=null;consumed=total;')
# The aggregate field is diagnostic and must match its explicit mode, never
# mark the failed baseline as semantic acceptance.
source=source.replace('work.Strict==18&&work.Semantic>=17', 'work.Strict==work.Total&&(work.Mode=="baseline_v2"?work.Exact==18&&work.Semantic==15:work.Semantic>=(work.Mode=="smoke18"?17:90))')
target.write_bytes(source.replace('\r\n','\n').encode('utf-8'))
print('created',target.relative_to(root),'sha256',hashlib.sha256(target.read_bytes()).hexdigest())
