using System;
using System.IO;
using System.Linq;
using EchoStation.AI.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3PackageMetadataTests
 {
  private static JObject Record()=>JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-Q3-A01b/StagedRelease.json"));
  private static string PackageRoot()
  {
   var stage=Path.GetFullPath("Library/EchoStation/S3Staging/"+Qwen3Catalog.DirectoryName);
   var installed=Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName);
   if(Directory.Exists(stage)==Directory.Exists(installed))throw new InvalidOperationException("Exactly one new package location required");
   return Directory.Exists(installed)?installed:stage;
  }
  [Test]public void NewCatalogHasExactReleaseInventoryAndBudget()
  {
   var release=Qwen3Catalog.Current;Assert.That(release.Id,Is.EqualTo("echo-qwen3-06b-q4-last-v1"));
   Assert.That(release.Files.Keys,Is.EquivalentTo(new[]{"model.onnx","tokenizer.json","tokenizer_config.json","config.json","generation_config.json","LICENSE.txt","MODEL_CARD.md","NOTICE.md","PROVENANCE.json"}));
   Assert.That(release.Files.Values.Sum(f=>f.Bytes),Is.EqualTo(Record()["sumBytes"].Value<long>()));Assert.That(release.Files.Values.Sum(f=>f.Bytes),Is.LessThan(1L<<30));
   Assert.That(release.Files["model.onnx"].Bytes,Is.EqualTo(919096845L));Assert.That(release.Files["model.onnx"].Sha256,Is.EqualTo("3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303"));
  }
  [Test]public void NewManifestMatchesCompiledTrustedCatalog()
  {ModelPackage.ValidateManifest(File.ReadAllText(Path.Combine(PackageRoot(),"manifest.json")),Qwen3Catalog.Current);Assert.That(Record()["sourceFilesVerified"].Value<bool>(),Is.True);}
  [Test]public void OfficialTokenizerNeedsExplicitBoundedTextBudget()
  {
   var file=Qwen3Catalog.Current.Files["tokenizer.json"];Assert.That(file.Bytes,Is.EqualTo(11422654L));Assert.That(file.Bytes,Is.GreaterThan(8*1024*1024));Assert.That(file.Bytes,Is.LessThan(16*1024*1024));
   Assert.That(file.Sha256,Is.EqualTo("aeb13307a71acd8fe81861d94ad54ab689df773318809eed3cbe794b4492dae4"));
  }
  [Test]public void OriginalLicenseAndDerivativeSourceArePreserved()
  {
   var root=PackageRoot();Assert.That(File.ReadAllText(Path.Combine(root,"LICENSE.txt")),Does.Contain("Apache License"));
   var notice=File.ReadAllText(Path.Combine(root,"NOTICE.md"));Assert.That(notice,Does.Contain("c1899de289a04d12100db370d81485cdf75e47ca"));Assert.That(notice,Does.Contain("58ca03a5e7b354b480b1a82b34bde8dc7b854cc0"));Assert.That(notice,Does.Contain("not an FP32-model quality claim"));
   var provenance=JObject.Parse(File.ReadAllText(Path.Combine(root,"PROVENANCE.json")));Assert.That(provenance["weightsUnchangedByLastPositionGather"].Value<bool>(),Is.True);Assert.That(provenance["candidateOnly"].Value<bool>(),Is.True);
  }
  [Test]public void ChangedReleaseOrFutureManifestCannotOverrideCatalog()
  {
   var manifest=(JObject)Record()["manifest"].DeepClone();manifest["version"]=2;
   Assert.That(Assert.Throws<ModelPackageException>(()=>ModelPackage.ValidateManifest(manifest.ToString(),Qwen3Catalog.Current)).Code,Is.EqualTo(ModelPackageError.UnsupportedVersion));
   manifest=(JObject)Record()["manifest"].DeepClone();manifest["releaseId"]="unknown-release";
   Assert.That(Assert.Throws<ModelPackageException>(()=>ModelPackage.ValidateManifest(manifest.ToString(),Qwen3Catalog.Current)).Code,Is.EqualTo(ModelPackageError.WrongRelease));
  }
  [Test]public void HistoricalCatalogWasNotReplaced()
  {Assert.That(ModelCatalog.Current.Id,Is.Not.EqualTo(Qwen3Catalog.Current.Id));Assert.That(ModelCatalog.DirectoryName,Is.Not.EqualTo(Qwen3Catalog.DirectoryName));}
 }
}
