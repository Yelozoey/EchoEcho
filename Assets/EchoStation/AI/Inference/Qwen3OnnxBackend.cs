using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using EchoStation.AI.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace EchoStation.AI.Inference
{
 // Caller retains the verified file lease; this Owner alone owns native sessions and tensors.
 public sealed class Qwen3OnnxBackend:ITokenModel,IDisposable
 {
  private readonly int main;private readonly InferenceSession session;private readonly RunOptions options;
  private readonly OrtValue[] emptyPast=new OrtValue[56],feeds=new OrtValue[59];
  private readonly string[] inputs=new string[59],outputs=new string[57];
  private IDisposableReadOnlyCollection<OrtValue> ownedOutputs;private OrtValue[] values;
  private int consumed,calls,nativeActive;private bool disposed,faulted;private long runTicks,validationTicks;
  public int OwnerThreadId {get;}
  public int CpuThreads {get;}
  public int EmptyPastCreated {get;private set;}public int EmptyPastDisposed {get;private set;}
  public int CollectionsCreated {get;private set;}public int CollectionsDisposed {get;private set;}
  public int DisposeThread {get;private set;}public int MainThreadNativeCalls {get;private set;}
  public bool IsDisposed=>disposed;public int TotalNativeCalls=>Volatile.Read(ref calls);
  public double NativeRunSeconds=>Volatile.Read(ref runTicks)/(double)Stopwatch.Frequency;
  public double ValidationSeconds=>Volatile.Read(ref validationTicks)/(double)Stopwatch.Frequency;
  public NativeActivity Activity=>new NativeActivity(Volatile.Read(ref calls),OwnerThreadId,Volatile.Read(ref nativeActive)!=0);
  public int ConsumedTokens {get{Owner();return consumed;}}
  public ReadOnlySpan<float> Logits {get{Owner();if(values==null||faulted)throw new InvalidOperationException("No valid Qwen3 output");return values[0].GetTensorDataAsSpan<float>();}}
  public ReadOnlySpan<float> KeyValue(int layer,bool value)
  {Owner();if(layer<0||layer>=28)throw new ArgumentOutOfRangeException(nameof(layer));if(values==null||faulted)throw new InvalidOperationException("No valid Qwen3 cache");return values[1+layer*2+(value?1:0)].GetTensorDataAsSpan<float>();}
  public Qwen3OnnxBackend(VerifiedModelPackage package,int unityMainThread,int cpuThreads=4)
  {
   OwnerThreadId=Thread.CurrentThread.ManagedThreadId;main=unityMainThread;
   if(main<=0||OwnerThreadId==main)throw new InvalidOperationException("Qwen3 requires a background Owner");
   if(cpuThreads!=2&&cpuThreads!=4)throw new ArgumentOutOfRangeException(nameof(cpuThreads));CpuThreads=cpuThreads;
   if(package==null||package.IsDisposed||package.ReleaseId!=Qwen3Catalog.Current.Id)throw new ArgumentException("Verified Qwen3 package required");
   try
   {
    OrtEnv.Instance().DisableTelemetryEvents();
    using(var setup=new SessionOptions{IntraOpNumThreads=cpuThreads,InterOpNumThreads=1,ExecutionMode=ExecutionMode.ORT_SEQUENTIAL,GraphOptimizationLevel=GraphOptimizationLevel.ORT_ENABLE_ALL})
    {setup.AddSessionConfigEntry("session.intra_op.allow_spinning","0");setup.AddSessionConfigEntry("session.inter_op.allow_spinning","0");setup.AppendExecutionProvider_CPU(1);session=new InferenceSession(package.ModelPath,setup);}
    Qwen3TensorContract.ValidateMetadata(Describe(session.InputMetadata),Describe(session.OutputMetadata));options=new RunOptions();
    inputs[0]="input_ids";inputs[1]="attention_mask";inputs[2]="position_ids";outputs[0]="logits";
    for(var layer=0;layer<28;layer++)for(var value=0;value<2;value++)
    {var index=layer*2+value;inputs[index+3]=Qwen3TensorContract.Past(layer,value==1);outputs[index+1]=Qwen3TensorContract.Present(layer,value==1);emptyPast[index]=OrtValue.CreateTensorValueFromMemory(Array.Empty<float>(),new long[]{1,8,0,128});EmptyPastCreated++;}
   }
   catch{Dispose();throw;}
  }
  private static Dictionary<string,TensorDescription> Describe(IReadOnlyDictionary<string,NodeMetadata> set)
  {return set.ToDictionary(item=>item.Key,item=>new TensorDescription(item.Value.IsTensor&&item.Value.ElementType==typeof(long)?TensorScalar.Int64:item.Value.IsTensor&&item.Value.ElementType==typeof(float)?TensorScalar.Float32:TensorScalar.Unsupported,item.Value.Dimensions.Select(value=>(long)value)),StringComparer.Ordinal);}
  public void Prefill(IReadOnlyList<int> ids)
  {CanRun();Qwen3TensorContract.ValidateInput(ids);var copied=ids.Select(value=>(long)value).ToArray();ResetCache();Run(copied);}
  public void Step(int token)
  {CanRun();Qwen3TensorContract.ValidateToken(token);if(values==null)throw new InvalidOperationException("Prefill required");if(consumed>=2048)throw new InvalidOperationException("Qwen3 context exhausted");Run(new[]{(long)token});}
  private void Run(long[] ids)
  {
   var total=consumed+ids.Length;if(total>2048)throw new InvalidOperationException("Qwen3 context exhausted");
   var attention=new long[total];for(var i=0;i<total;i++)attention[i]=1;var positions=new long[ids.Length];for(var i=0;i<positions.Length;i++)positions[i]=consumed+i;
   using(var input=OrtValue.CreateTensorValueFromMemory(ids,new long[]{1,ids.Length}))
   using(var mask=OrtValue.CreateTensorValueFromMemory(attention,new long[]{1,total}))
   using(var position=OrtValue.CreateTensorValueFromMemory(positions,new long[]{1,ids.Length}))
   {
    feeds[0]=input;feeds[1]=mask;feeds[2]=position;for(var i=0;i<56;i++)feeds[i+3]=values==null?emptyPast[i]:values[i+1];
    IDisposableReadOnlyCollection<OrtValue> next=null;
    try
    {
     Interlocked.Increment(ref calls);if(Thread.CurrentThread.ManagedThreadId==main)MainThreadNativeCalls++;
     var started=Stopwatch.GetTimestamp();Volatile.Write(ref nativeActive,1);
     try{next=session.Run(options,inputs,feeds,outputs);}finally{Volatile.Write(ref nativeActive,0);runTicks+=Stopwatch.GetTimestamp()-started;}
     CollectionsCreated++;var checking=Stopwatch.GetTimestamp();var nextValues=next.ToArray();if(nextValues.Length!=57)throw new InvalidDataException("Qwen3 runtime output count differs");
     for(var i=0;i<nextValues.Length;i++)
     {
      var shape=nextValues[i].GetTensorTypeAndShape();if(shape.ElementDataType!=TensorElementType.Float)throw new InvalidDataException("Qwen3 runtime dtype differs");
      if(i==0)Qwen3TensorContract.ValidateLogits(nextValues[i].GetTensorDataAsSpan<float>(),shape.Shape);else Qwen3TensorContract.ValidateKv(nextValues[i].GetTensorDataAsSpan<float>(),shape.Shape,total);
     }
     validationTicks+=Stopwatch.GetTimestamp()-checking;ReleaseOutputs();ownedOutputs=next;values=nextValues;next=null;consumed=total;
    }
    catch{faulted=true;if(next!=null){next.Dispose();CollectionsDisposed++;}ClearCache();throw;}
    finally{Array.Clear(feeds,0,feeds.Length);}
   }
  }
  public void ResetCache(){Owner();ClearCache();}
  private void ReleaseOutputs(){var previous=ownedOutputs;ownedOutputs=null;values=null;if(previous!=null){previous.Dispose();CollectionsDisposed++;}}
  private void ClearCache(){ReleaseOutputs();consumed=0;}
  private void Owner(){if(Thread.CurrentThread.ManagedThreadId!=OwnerThreadId)throw new InvalidOperationException("Only the Qwen3 Owner may access tensors");if(disposed)throw new ObjectDisposedException(nameof(Qwen3OnnxBackend));}
  private void CanRun(){Owner();if(faulted)throw new InvalidOperationException("Faulted Qwen3 session must be recreated");}
  public void Dispose()
  {
   if(Thread.CurrentThread.ManagedThreadId!=OwnerThreadId)throw new InvalidOperationException("Only the Qwen3 Owner may dispose native resources");if(disposed)return;Exception first=null;
   try{ClearCache();}catch(Exception error){first=error;}
   for(var i=0;i<emptyPast.Length;i++){var value=emptyPast[i];emptyPast[i]=null;try{if(value!=null){value.Dispose();EmptyPastDisposed++;}}catch(Exception error){if(first==null)first=error;}}
   try{options?.Dispose();}catch(Exception error){if(first==null)first=error;}try{session?.Dispose();}catch(Exception error){if(first==null)first=error;}
   DisposeThread=Thread.CurrentThread.ManagedThreadId;disposed=true;if(first!=null)throw new InvalidOperationException("Qwen3 cleanup failed",first);
  }
 }
}
