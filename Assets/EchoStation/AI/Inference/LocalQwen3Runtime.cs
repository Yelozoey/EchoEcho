using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using EchoStation.AI.Context;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using EchoStation.Memory;

namespace EchoStation.AI.Inference
{
 public sealed class LocalQwen3Factory:ILocalModelFactory
 {
  private readonly string directory;private readonly MemorySearch search;private readonly ITokenConstraintProvider constraints;private readonly Qwen3PromptProfile profile;private readonly TokenSamplingOptions sampling;
  public LocalQwen3Factory(string packageDirectory,ITokenConstraintProvider constraintProvider,MemorySearch retrieval=null,Qwen3PromptProfile promptProfile=null,TokenSamplingOptions samplingOptions=null)
  {if(string.IsNullOrWhiteSpace(packageDirectory))throw new ArgumentException("Qwen3 directory required");directory=packageDirectory;constraints=constraintProvider??throw new ArgumentNullException(nameof(constraintProvider));search=retrieval??new MemorySearch();profile=promptProfile??Qwen3PromptProfile.Selected;sampling=samplingOptions;}
  public ILocalModelRuntime Create(int unityMainThread,CancellationToken cancellation)=>new LocalQwen3Runtime(directory,unityMainThread,cancellation,search,constraints,profile,sampling);
 }
 internal sealed class LocalQwen3Runtime:ILocalModelRuntime,ILocalRuntimeDiagnostics
 {
  private readonly int owner;private readonly VerifiedModelPackage package;private readonly Qwen3OnnxBackend backend;private readonly TokenGenerator generator;
  private readonly ContextAssembler assembler;private readonly Qwen3PromptPreparation preparation;private readonly ITokenConstraintProvider constraints;private readonly TokenSamplingOptions sampling;private bool disposed;
  public NativeActivity Activity=>backend?.Activity??default;
  public LocalQwen3Runtime(string directory,int main,CancellationToken cancellation,MemorySearch search,ITokenConstraintProvider provider,Qwen3PromptProfile profile,TokenSamplingOptions samplingOptions)
  {
   owner=Thread.CurrentThread.ManagedThreadId;if(main<=0||owner==main)throw new InvalidOperationException("Qwen3 runtime initialization must be background");constraints=provider??throw new ArgumentNullException(nameof(provider));sampling=samplingOptions;
   try
   {
    package=ModelPackage.Verify(directory,Qwen3Catalog.Current,cancellation);cancellation.ThrowIfCancellationRequested();var tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.LoadQwen3(package));
    assembler=new ContextAssembler(new MemoryContextBuilder(search),tokenizer);preparation=new Qwen3PromptPreparation(tokenizer,profile);cancellation.ThrowIfCancellationRequested();backend=new Qwen3OnnxBackend(package,main,4);generator=new TokenGenerator(backend,tokenizer);
   }
   catch{try{backend?.Dispose();}finally{package?.Dispose();}throw;}
  }
  public GenerationResult Generate(TokenRequest request,CancellationToken cancellation)
  {
   RequireOwner();cancellation.ThrowIfCancellationRequested();
   if(request?.Snapshot==null)throw new GenerationException(GenerationFault.InvalidContext,"Qwen3 requires a bounded memory/target snapshot");
   var clock=Stopwatch.StartNew();PreparedPrompt prepared;
   try{prepared=preparation.Build(assembler.Build(request.Snapshot));}
   catch(Exception error)when(error is ArgumentException||error is ContextBudgetException){throw new GenerationException(GenerationFault.InvalidContext,"Qwen3 context rejected before native inference",error);}
   cancellation.ThrowIfCancellationRequested();var constraint=constraints.Create(prepared);if(constraint==null)throw new InvalidOperationException("Qwen3 JSON constraint provider returned null");
   var seconds=clock.Elapsed.TotalSeconds;var remaining=20-seconds;if(remaining<=0)throw new GenerationException(GenerationFault.Timeout,"Qwen3 preparation reached request deadline");
   var generated=generator.Generate(prepared.InputIds,cancellation,timeoutSeconds:remaining,constraint:constraint,sampling:sampling);
   return new GenerationResult(generated.Text,generated.TokenIds.ToList(),generated.Stop,generated.Seconds,generated.Mode,prepared,Thread.CurrentThread.ManagedThreadId,seconds,generated.SamplingOptions);
  }
  private void RequireOwner(){if(Thread.CurrentThread.ManagedThreadId!=owner)throw new InvalidOperationException("Only Qwen3 Owner may use runtime");if(disposed)throw new ObjectDisposedException(nameof(LocalQwen3Runtime));}
  public void Dispose()
  {if(Thread.CurrentThread.ManagedThreadId!=owner)throw new InvalidOperationException("Only Qwen3 Owner may dispose runtime");if(disposed)return;try{backend?.Dispose();}finally{package?.Dispose();disposed=true;}}
 }
}
