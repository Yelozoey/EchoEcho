using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EchoStation.AI.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace EchoStation.AI.Inference
{
 // Owns native tensors/session only. The caller keeps the verified package lease alive.
 public sealed class QwenOnnxBackend:IDisposable,ITokenModel
 {
  private readonly InferenceSession session;private readonly RunOptions options;
  private readonly OrtValue[] emptyPast=new OrtValue[48];private readonly OrtValue[] feedValues=new OrtValue[51];
  private readonly string[] inputNames=new string[51];private readonly string[] outputNames=new string[49];
  private IDisposableReadOnlyCollection<OrtValue> lastOutputs;private OrtValue[] lastValues;
  private bool disposed,faulted;private int consumed,calls,nativeActive;
  public int OwnerThreadId {get;}
  public NativeActivity Activity=>new NativeActivity(Volatile.Read(ref calls),OwnerThreadId,Volatile.Read(ref nativeActive)!=0);
  public int NativeCalls {get{EnsureOwner();return calls;}}
  public int ConsumedTokens {get{EnsureOwner();return consumed;}}
  public ReadOnlySpan<float> Logits {get{EnsureOwner();if(lastValues==null||faulted)throw new InvalidOperationException("No valid model output");return lastValues[0].GetTensorDataAsSpan<float>();}}
  public QwenOnnxBackend(VerifiedModelPackage package,int unityMainThread)
  {
   OwnerThreadId=Thread.CurrentThread.ManagedThreadId;if(unityMainThread<=0||OwnerThreadId==unityMainThread)throw new InvalidOperationException("Model resources require a background Owner");
   if(package==null||package.IsDisposed||package.ReleaseId!=ModelCatalog.Current.Id)throw new ArgumentException("Verified current model package required");
   OrtEnv.Instance().DisableTelemetryEvents();
   using(var setup=new SessionOptions{IntraOpNumThreads=2,InterOpNumThreads=1,ExecutionMode=ExecutionMode.ORT_SEQUENTIAL,GraphOptimizationLevel=GraphOptimizationLevel.ORT_ENABLE_ALL})
   {
    setup.AddSessionConfigEntry("session.intra_op.allow_spinning","0");setup.AppendExecutionProvider_CPU(1);session=new InferenceSession(package.ModelPath,setup);
   }
   try
   {
    QwenTensorContract.ValidateMetadata(Describe(session.InputMetadata),Describe(session.OutputMetadata));options=new RunOptions();
    inputNames[0]="input_ids";inputNames[1]="attention_mask";inputNames[2]="position_ids";outputNames[0]="logits";
    for(var layer=0;layer<24;layer++)for(var value=0;value<2;value++)
    {var i=layer*2+value;inputNames[i+3]=QwenTensorContract.Past(layer,value==1);outputNames[i+1]=QwenTensorContract.Present(layer,value==1);emptyPast[i]=OrtValue.CreateTensorValueFromMemory(Array.Empty<float>(),new long[]{1,2,0,64});}
   }
   catch{foreach(var tensor in emptyPast)tensor?.Dispose();options?.Dispose();session.Dispose();throw;}
  }
  private static Dictionary<string,TensorDescription> Describe(IReadOnlyDictionary<string,NodeMetadata> metadata)
  {return metadata.ToDictionary(p=>p.Key,p=>new TensorDescription(p.Value.IsTensor&&p.Value.ElementType==typeof(long)?TensorScalar.Int64:p.Value.IsTensor&&p.Value.ElementType==typeof(float)?TensorScalar.Float32:TensorScalar.Unsupported,p.Value.Dimensions.Select(d=>(long)d)),StringComparer.Ordinal);}
  public void Prefill(IReadOnlyList<int> ids)
  {
   EnsureCanRun();QwenTensorContract.ValidateInput(ids);var copy=new long[ids.Count];for(var i=0;i<copy.Length;i++)copy[i]=ids[i];ResetCache();Run(copy);
  }
  public void Step(int token)
  {EnsureCanRun();QwenTensorContract.ValidateToken(token);if(lastValues==null)throw new InvalidOperationException("Prefill required before decode");if(consumed>=2048)throw new InvalidOperationException("Context exhausted");Run(new[]{(long)token});}
  private void Run(long[] ids)
  {
   var total=consumed+ids.Length;var mask=new long[total];for(var i=0;i<mask.Length;i++)mask[i]=1;var positions=new long[ids.Length];for(var i=0;i<positions.Length;i++)positions[i]=consumed+i;
   using(var input=OrtValue.CreateTensorValueFromMemory(ids,new long[]{1,ids.Length}))
   using(var attention=OrtValue.CreateTensorValueFromMemory(mask,new long[]{1,total}))
   using(var position=OrtValue.CreateTensorValueFromMemory(positions,new long[]{1,ids.Length}))
   {
    feedValues[0]=input;feedValues[1]=attention;feedValues[2]=position;
    for(var i=0;i<48;i++)feedValues[i+3]=lastValues==null?emptyPast[i]:lastValues[i+1];
    IDisposableReadOnlyCollection<OrtValue> next=null;
    try
    {
     Interlocked.Increment(ref calls);Volatile.Write(ref nativeActive,1);try{next=session.Run(options,inputNames,feedValues,outputNames);}finally{Volatile.Write(ref nativeActive,0);}var values=next.ToArray();
     if(values.Length!=49)throw new InvalidDataException("Native output count differs");
     for(var i=0;i<values.Length;i++)
     {
      var shape=values[i].GetTensorTypeAndShape();if(shape.ElementDataType!=TensorElementType.Float)throw new InvalidDataException("Native output dtype differs");
      if(i==0)QwenTensorContract.ValidateLogits(values[i].GetTensorDataAsSpan<float>(),shape.Shape);else QwenTensorContract.ValidateKv(values[i].GetTensorDataAsSpan<float>(),shape.Shape,total);
     }
     var previous=lastOutputs;lastOutputs=next;lastValues=values;next=null;consumed=total;previous?.Dispose();
    }
    catch{faulted=true;next?.Dispose();ClearCache();throw;}
    finally{for(var i=0;i<feedValues.Length;i++)feedValues[i]=null;}
   }
  }
  public void ResetCache(){EnsureOwner();ClearCache();}
  private void ClearCache(){lastOutputs?.Dispose();lastOutputs=null;lastValues=null;consumed=0;}
  private void EnsureOwner(){if(Thread.CurrentThread.ManagedThreadId!=OwnerThreadId)throw new InvalidOperationException("Wrong model resource Owner");if(disposed)throw new ObjectDisposedException(nameof(QwenOnnxBackend));}
  private void EnsureCanRun(){EnsureOwner();if(faulted)throw new InvalidOperationException("Faulted model session must be recreated");}
  public void Dispose()
  {
   if(Thread.CurrentThread.ManagedThreadId!=OwnerThreadId)throw new InvalidOperationException("Only Owner can dispose model resources");if(disposed)return;
   ClearCache();foreach(var tensor in emptyPast)tensor?.Dispose();options?.Dispose();session?.Dispose();disposed=true;
  }
 }
}
