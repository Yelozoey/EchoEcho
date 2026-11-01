using System;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.Memory;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;

namespace EchoStation.AI.Inference
{
 public sealed class LocalQwenFactory:ILocalModelFactory
 {
  private readonly string directory;private readonly MemorySearch search;private readonly ITokenConstraintProvider constraints;
  public LocalQwenFactory(string packageDirectory,MemorySearch retrieval=null,ITokenConstraintProvider constraintProvider=null){directory=packageDirectory??throw new ArgumentNullException(nameof(packageDirectory));search=retrieval??new MemorySearch();constraints=constraintProvider;}
  public ILocalModelRuntime Create(int unityMainThread,CancellationToken cancellation)=>new LocalQwenRuntime(directory,unityMainThread,cancellation,search,constraints);
 }
 internal sealed class LocalQwenRuntime:ILocalModelRuntime,ILocalRuntimeDiagnostics
 {
  private readonly VerifiedModelPackage package;private readonly QwenOnnxBackend backend;private readonly TokenGenerator generator;
  private readonly ContextAssembler assembler;private readonly PromptPreparation preparation;private readonly ITokenConstraintProvider constraints;
  public NativeActivity Activity=>backend?.Activity??default;
  public LocalQwenRuntime(string directory,int main,CancellationToken cancellation,MemorySearch search,ITokenConstraintProvider constraintProvider)
  {
   if(Thread.CurrentThread.ManagedThreadId==main)throw new InvalidOperationException("Runtime initialization must be background");constraints=constraintProvider;
   try{package=ModelPackage.Verify(directory,ModelCatalog.Current,cancellation);var tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.Load(package));assembler=new ContextAssembler(new MemoryContextBuilder(search),tokenizer);preparation=new PromptPreparation(tokenizer);cancellation.ThrowIfCancellationRequested();backend=new QwenOnnxBackend(package,main);generator=new TokenGenerator(backend,tokenizer);}
   catch{backend?.Dispose();package?.Dispose();throw;}
  }
  public GenerationResult Generate(TokenRequest request,CancellationToken cancellation)
  {
   cancellation.ThrowIfCancellationRequested();if(request.Snapshot==null)return generator.Generate(request.Input,cancellation);var clock=Stopwatch.StartNew();PreparedPrompt prepared;
   try{prepared=preparation.Build(assembler.Build(request.Snapshot));}
   catch(Exception error)when(error is ArgumentException||error is ContextBudgetException){throw new GenerationException(GenerationFault.InvalidContext,"Input context rejected before native inference",error);}
   cancellation.ThrowIfCancellationRequested();var constraint=constraints?.Create(prepared);if(constraints!=null&&constraint==null)throw new InvalidOperationException("Constraint provider returned null");var seconds=clock.Elapsed.TotalSeconds;var remaining=20-seconds;if(remaining<=0)throw new GenerationException(GenerationFault.Timeout,"Context preparation reached request deadline");
   var generated=generator.Generate(prepared.InputIds,cancellation,timeoutSeconds:remaining,constraint:constraint);return new GenerationResult(generated.Text,generated.TokenIds.ToList(),generated.Stop,generated.Seconds,generated.Mode,prepared,Thread.CurrentThread.ManagedThreadId,seconds);
  }
  public void Dispose(){backend?.Dispose();package?.Dispose();}
 }
}
