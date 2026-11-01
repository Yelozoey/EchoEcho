using System;
using System.IO;
using System.Runtime.InteropServices;

namespace EchoStation.AI.Inference
{
 public static class FiniteTensorValidator
 {
  // IEEE754 binary32: an all-ones exponent denotes either infinity or NaN.
  // Reinterpretation avoids per-element floating point helper calls on Mono.
  // Every element is still checked, including signed NaNs and subnormals.
  public static void RequireFinite(ReadOnlySpan<float> values)
  {
   var bits=MemoryMarshal.Cast<float,uint>(values);
   for(var i=0;i<bits.Length;i++)
    if((bits[i]&0x7f800000u)==0x7f800000u)
     throw new InvalidDataException("Nonfinite model tensor");
  }
 }
}
