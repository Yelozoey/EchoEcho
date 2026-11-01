using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EchoStation.AI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class VerifiedTextBudgetTests
 {
  private const int MiB=1024*1024;
  private string root;
  [SetUp]public void Setup()
  {root=Path.GetFullPath("Library/EchoStation/S3Tests/Q3Text/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);}
  [TearDown]public void Cleanup()
  {
   var prefix=Path.GetFullPath("Library/EchoStation/S3Tests/Q3Text")+Path.DirectorySeparatorChar;
   if(root==null||!Path.GetFullPath(root).StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unsafe test cleanup");
   foreach(var file in Directory.GetFiles(root))File.Delete(file);Directory.Delete(root,false);
  }
  private static string Sha(byte[] bytes)
  {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
  private VerifiedModelPackage Package(byte[] text)
  {
   var model=Encoding.UTF8.GetBytes("contract fixture, not an ONNX model");
   File.WriteAllBytes(Path.Combine(root,"model.onnx"),model);File.WriteAllBytes(Path.Combine(root,"tokenizer.json"),text);
   var release=new ModelRelease("q3-text-fixture",new[]{new ModelFileSpec("model.onnx",model.Length,Sha(model)),new ModelFileSpec("tokenizer.json",text.Length,Sha(text))});
   var manifest=new JObject{{"version",1},{"releaseId",release.Id},{"files",new JArray(release.Files.Values.Select(f=>new JObject{{"name",f.Name},{"bytes",f.Bytes},{"sha256",f.Sha256}}))}};
   File.WriteAllText(Path.Combine(root,"manifest.json"),manifest.ToString(Formatting.None));
   return ModelPackage.Verify(root,release);
  }
  [Test]public void DefaultEightMiBStillRejectsLargeTextAndExplicitBudgetReadsIt()
  {
   var bytes=new byte[9*MiB];for(var i=0;i<bytes.Length;i++)bytes[i]=(byte)'a';
   using(var package=Package(bytes))
   {
    Assert.Throws<InvalidOperationException>(()=>package.ReadUtf8("tokenizer.json"));
    var text=package.ReadUtf8("tokenizer.json",16*MiB);Assert.That(text.Length,Is.EqualTo(bytes.Length));Assert.That(text.All(c=>c=='a'),Is.True);
    Assert.That(package.ReadUtf8("tokenizer.json",16*MiB).Length,Is.EqualTo(bytes.Length));
   }
  }
  [Test]public void SmallUtf8AndReadLeaseRemainCompatible()
  {
   const string text="Echo：已发现线索。😀";
   using(var package=Package(Encoding.UTF8.GetBytes(text)))
   {
    Assert.That(package.ReadUtf8("tokenizer.json"),Is.EqualTo(text));Assert.That(package.ReadUtf8("tokenizer.json",16*MiB),Is.EqualTo(text));
    Assert.Throws<IOException>(()=>{using(var stream=new FileStream(Path.Combine(root,"tokenizer.json"),FileMode.Open,FileAccess.Write,FileShare.ReadWrite)){};});
   }
   using(var stream=new FileStream(Path.Combine(root,"tokenizer.json"),FileMode.Open,FileAccess.Write,FileShare.ReadWrite))Assert.That(stream.CanWrite,Is.True);
  }
  [TestCase(0)][TestCase(-1)][TestCase(16*MiB+1)]public void UnboundedOrInvalidBudgetsRejected(int budget)
  {using(var package=Package(new byte[]{65}))Assert.Throws<ArgumentOutOfRangeException>(()=>package.ReadUtf8("tokenizer.json",budget));}
  [Test]public void CallerCannotReadPastItsDeclaredBudget()
  {using(var package=Package(Encoding.UTF8.GetBytes("12345")))Assert.Throws<InvalidOperationException>(()=>package.ReadUtf8("tokenizer.json",4));}
  [Test]public void BadUtf8AndDisposedLeaseRemainRejected()
  {
   var package=Package(new byte[]{0xc3,0x28});
   try{Assert.Throws<DecoderFallbackException>(()=>package.ReadUtf8("tokenizer.json",16*MiB));}finally{package.Dispose();}
   Assert.Throws<ObjectDisposedException>(()=>package.ReadUtf8("tokenizer.json",16*MiB));
  }
  [Test]public void LargerBudgetCannotReadUnverifiedFile()
  {using(var package=Package(new byte[]{65}))Assert.Throws<InvalidOperationException>(()=>package.ReadUtf8("unlisted.json",16*MiB));}
 }
}
