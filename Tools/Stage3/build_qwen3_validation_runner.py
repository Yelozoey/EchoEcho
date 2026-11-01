"""Derive one bounded 100-case fixture from the independently passed runtime runner."""
from pathlib import Path

root=Path(__file__).resolve().parents[2]
source=(root/'Assets/EchoStation/Tests/AIEditor/Qwen3RuntimeTests.cs').read_text(encoding='utf-8')
def replace(old,new):
    global source
    assert source.count(old)==1, 'Unique fixture anchor required: '+old[:60]
    source=source.replace(old,new)

replace('public sealed class Qwen3RuntimeTests','public sealed class Qwen3RuntimeValidationTests')
start=source.index('  [Test]public void FactoryRequiresDirectoryAndConstraintProvider()')
end=source.index('  [UnityTest,Timeout(600000)]',start)
source=source[:start]+source[end:]
replace('using System.Text;','using System.Text;\nusing System.Security.Cryptography;')
replace('[UnityTest,Timeout(600000)]public IEnumerator Actual18InputsUseFormalCSharpPreparationAndCompleteJson()',
        '[UnityTest,Timeout(2400000)]public IEnumerator Frozen100InputsUseFormalCSharpPreparationAndCompleteJson()')
replace('S3-Q3-R01/Runtime18_','S3-Q3-R02/Runtime100_')
replace('clock.Elapsed.TotalSeconds<580','clock.Elapsed.TotalSeconds<2390')
replace('clock.Elapsed.TotalSeconds>=560','clock.Elapsed.TotalSeconds>=2370')
replace('Assert.That(work.Strict,Is.EqualTo(18))','Assert.That(work.Strict,Is.EqualTo(100))')
replace('Assert.That(work.Semantic,Is.GreaterThanOrEqualTo(17)','Assert.That(work.Semantic,Is.GreaterThanOrEqualTo(90)')
replace('Formal runtime smoke gate: ','Formal runtime independent100 gate: ')
replace('formal C# snapshot/retrieval/tokenizer/prompt/backend JSON smoke; no game dispatch or factual acceptance',
        'formal C# frozen100 snapshot/retrieval/tokenizer/prompt/backend validation; no game dispatch or factual acceptance')
replace('var source=JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3/CandidateCases.json"));',
        '''var suite=File.ReadAllBytes("Tools/Stage3/Fixtures/Q3_Validation100_B.json");
    using(var sha=SHA256.Create()){var actual=BitConverter.ToString(sha.ComputeHash(suite)).Replace("-","").ToLowerInvariant();Need(actual=="48ce057de9fecbe441d18b56cba8513ef4754e331043938dd226eacac209a530","Frozen suite changed");report["suiteSha256"]=actual;}
    var source=JObject.Parse(Encoding.UTF8.GetString(suite));Need(source["cases"].Count()==100,"Frozen case count differs");''')
replace('Tuning/V5_V2Contrast_A/Cases.json','Tuning/V5_HeldOut100_CPU4_A/Cases.json')
replace('var search=new MemorySearch(new[]{new MemoryKeywordGroup("设备记录","供电面板","危险设备","power_panel_01","hazard_01")});',
        'var search=Qwen3RuntimeFixtureContext.Search();')
start=source.index('     var store=new MemoryStore();')
end=source.index('     var row=new JObject',start)
source=source[:start]+'     var input=item["input"].Value<string>();var snapshot=Qwen3RuntimeFixtureContext.Create(item,index);\n'+source[end:]
replace('work.Strict==18&&work.Semantic>=17','work.Strict==100&&work.Semantic>=90')
target=root/'Assets/EchoStation/Tests/AIEditor/Qwen3RuntimeValidationTests.cs'
with target.open('xb') as stream:stream.write(source.encode('utf-8'))
print('Created formal frozen100 runner; thresholds100/90, no expected labels in runtime inputs')
