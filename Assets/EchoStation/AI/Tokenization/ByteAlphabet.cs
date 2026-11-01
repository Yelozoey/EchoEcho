using System;
using System.Collections.Generic;
using System.Text;

namespace EchoStation.AI.Tokenization
{
 public static class ByteAlphabet
 {
  private static readonly char[] Forward=new char[256];
  private static readonly Dictionary<char,byte> Reverse=new Dictionary<char,byte>();
  private static readonly UTF8Encoding StrictUtf8=new UTF8Encoding(false,true);
  static ByteAlphabet()
  {
   var next=256;
   for(var value=0;value<256;value++)
   {
    var visible=(value>=33&&value<=126)||(value>=161&&value<=172)||(value>=174);
    var character=(char)(visible?value:next++);Forward[value]=character;Reverse.Add(character,(byte)value);
   }
  }
  public static char ForByte(byte value)=>Forward[value];
  public static string Encode(string text)=>Encode(StrictUtf8.GetBytes(text??throw new ArgumentNullException(nameof(text))));
  public static string Encode(byte[] bytes){if(bytes==null)throw new ArgumentNullException(nameof(bytes));var chars=new char[bytes.Length];for(var i=0;i<bytes.Length;i++)chars[i]=Forward[bytes[i]];return new string(chars);}
  public static byte[] DecodeBytes(string encoded)
  {if(encoded==null)throw new ArgumentNullException(nameof(encoded));var bytes=new byte[encoded.Length];for(var i=0;i<bytes.Length;i++)if(!Reverse.TryGetValue(encoded[i],out bytes[i]))throw new ArgumentException("Character outside byte alphabet");return bytes;}
 }
}
