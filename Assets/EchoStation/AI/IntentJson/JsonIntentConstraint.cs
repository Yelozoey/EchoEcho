using System;
using System.Collections.Generic;
using System.Text;
using EchoStation.AI.Context;
using EchoStation.AI.Inference;
using EchoStation.Memory;

namespace EchoStation.AI.IntentJson
{
 // Incremental prefix recognizer. Allows copies a small value-state and never mutates generation.
 public sealed class JsonIntentConstraint:ITokenConstraint
 {
  private enum Mode{Literal,Choice,Content,Done}
  private enum Literal{Start,InvestigateTarget,WarnTarget,Content,Close}
  private struct State
  {
   public Mode Mode;public Literal Literal;public int Position,Intent;public uint Mask;public bool Target;
   public int Escape,HexDigits,HexValue,High,Remaining,Point,Minimum,ContinuationMin,ContinuationMax,Characters;public bool NonWhite;
  }
  private static readonly byte[][] Literals={Ascii("{\"intent\":\""),Ascii(",\"targetName\":\""),Ascii(",\"dangerTarget\":\""),Ascii(",\"content\":\""),Ascii("}")};
  private static readonly byte[][] Intents={Ascii("follow\""),Ascii("investigate\""),Ascii("wait\""),Ascii("warn\""),Ascii("answer\""),Ascii("refuse\"")};
  private readonly byte[][] investigate,warn;private readonly uint intentMask;private State state;private int tokens;
  public string Name=>"json-intent-v1-reserve16";
  public bool CanEnd=>state.Mode==Mode.Done;
  public int AcceptedTokens=>tokens;
  public int ContentUtf16Length=>state.Characters;
  public JsonIntentConstraint(IReadOnlyList<AllowedTarget> targets)
  {
   if(targets==null)throw new ArgumentNullException(nameof(targets));if(targets.Count>8)throw new ArgumentException("Grammar target budget");
   var identities=new HashSet<string>(StringComparer.Ordinal);var scan=new List<byte[]>();var danger=new List<byte[]>();
   foreach(var t in targets){if(t==null||!MemoryLimits.IsValidId(t.Id)||!identities.Add(t.Id))throw new ArgumentException("Invalid grammar target identity");if(t.CanInvestigate)scan.Add(Ascii(t.Id+"\""));if(t.IsDanger)danger.Add(Ascii(t.Id+"\""));}
   investigate=scan.ToArray();warn=danger.ToArray();intentMask=(1u<<0)|(1u<<2)|(1u<<4)|(1u<<5);if(investigate.Length>0)intentMask|=1u<<1;if(warn.Length>0)intentMask|=1u<<3;SetLiteral(ref state,Literal.Start);
  }
  public bool Allows(int id,ReadOnlySpan<byte> bytes){var trial=state;return bytes.Length>0&&Advance(ref trial,bytes,tokens>=240);}
  public void Accept(int id,ReadOnlySpan<byte> bytes)
  {var trial=state;if(bytes.Length==0||!Advance(ref trial,bytes,tokens>=240))throw new InvalidOperationException("Token violates JSON prefix grammar");state=trial;tokens++;}
  private bool Advance(ref State s,ReadOnlySpan<byte> bytes,bool ending)
  {
   foreach(var b in bytes)
   {
    if(s.Mode==Mode.Done)return false;
    if(s.Mode==Mode.Literal)
    {
     var literal=Literals[(int)s.Literal];if(b!=literal[s.Position++])return false;if(s.Position!=literal.Length)continue;
     switch(s.Literal)
     {
      case Literal.Start:s.Mode=Mode.Choice;s.Target=false;s.Position=0;s.Mask=intentMask;break;
      case Literal.InvestigateTarget:case Literal.WarnTarget:s.Mode=Mode.Choice;s.Target=true;s.Position=0;s.Mask=(1u<<(s.Intent==1?investigate.Length:warn.Length))-1;break;
      case Literal.Content:s.Mode=Mode.Content;s.Position=0;s.Characters=0;s.NonWhite=false;s.Escape=0;s.Remaining=0;break;
      case Literal.Close:s.Mode=Mode.Done;break;
     }
    }
    else if(s.Mode==Mode.Choice)
    {
     var options=s.Target?(s.Intent==1?investigate:warn):Intents;uint next=0;var complete=-1;
     for(var i=0;i<options.Length;i++)if((s.Mask&(1u<<i))!=0&&s.Position<options[i].Length&&b==options[i][s.Position]){next|=1u<<i;if(s.Position+1==options[i].Length)complete=i;}
     if(next==0)return false;s.Mask=next;s.Position++;
     if(complete>=0)
     {
      if(s.Target)SetLiteral(ref s,Literal.Content);
      else{s.Intent=complete;SetLiteral(ref s,complete==0||complete==2?Literal.Close:complete==1?Literal.InvestigateTarget:complete==3?Literal.WarnTarget:Literal.Content);}
     }
    }
    else if(!Content(ref s,b,ending))return false;
   }
   return true;
  }
  private static void SetLiteral(ref State s,Literal literal){s.Mode=Mode.Literal;s.Literal=literal;s.Position=0;}
  private static byte[] Ascii(string value)=>Encoding.ASCII.GetBytes(value);
  private static bool Content(ref State s,byte b,bool ending)
  {
   if(s.Escape!=0)return Escape(ref s,b,ending);
   if(s.Remaining>0)
   {
    if(b<s.ContinuationMin||b>s.ContinuationMax)return false;s.Point=(s.Point<<6)|(b&63);s.Remaining--;s.ContinuationMin=0x80;s.ContinuationMax=0xbf;
    return s.Remaining>0||s.Point>=s.Minimum&&s.Point<=0x10ffff&&(s.Point<0xd800||s.Point>0xdfff)&&AddScalar(ref s,s.Point,ending);
   }
   if(ending&&s.NonWhite&&b!='"')return false;
   if(b=='"'){if(!s.NonWhite)return false;SetLiteral(ref s,Literal.Close);return true;}
   if(b=='\\'){if(s.Characters>=280)return false;s.Escape=1;return true;}
   if(b<32)return false;if(b<128)return AddScalar(ref s,b,ending);if(s.Characters>=280)return false;
   s.ContinuationMin=0x80;s.ContinuationMax=0xbf;
   if(b>=0xc2&&b<=0xdf){s.Remaining=1;s.Point=b&31;s.Minimum=0x80;return true;}
   if(b>=0xe0&&b<=0xef){s.Remaining=2;s.Point=b&15;s.Minimum=0x800;if(b==0xe0)s.ContinuationMin=0xa0;if(b==0xed)s.ContinuationMax=0x9f;return true;}
   if(b>=0xf0&&b<=0xf4){if(s.Characters>278)return false;s.Remaining=3;s.Point=b&7;s.Minimum=0x10000;if(b==0xf0)s.ContinuationMin=0x90;if(b==0xf4)s.ContinuationMax=0x8f;return true;}return false;
  }
  private static bool Escape(ref State s,byte b,bool ending)
  {
   if(s.Escape==1)
   {
    if(b=='u'){s.Escape=2;s.HexDigits=0;s.HexValue=0;return true;}int point;
    switch(b){case (byte)'"':point='"';break;case (byte)'\\':point='\\';break;case (byte)'/':point='/';break;case (byte)'b':point=8;break;case (byte)'f':point=12;break;case (byte)'n':point=10;break;case (byte)'r':point=13;break;case (byte)'t':point=9;break;default:return false;}s.Escape=0;return AddScalar(ref s,point,ending);
   }
   if(s.Escape==3){if(b!='\\')return false;s.Escape=4;return true;}if(s.Escape==4){if(b!='u')return false;s.Escape=5;s.HexDigits=0;s.HexValue=0;return true;}
   var digit=b>='0'&&b<='9'?b-'0':b>='a'&&b<='f'?b-'a'+10:b>='A'&&b<='F'?b-'A'+10:-1;if(digit<0)return false;
   s.HexValue=(s.HexValue<<4)|digit;s.HexDigits++;var shift=(4-s.HexDigits)*4;var low=s.HexValue<<shift;var high=((s.HexValue+1)<<shift)-1;
   if(s.Escape==5){if(!Touches(low,high,0xdc00,0xdfff))return false;}
   else if(!Touches(low,high,0,0xd7ff)&&!Touches(low,high,0xe000,0xffff)&&!(s.Characters<=278&&Touches(low,high,0xd800,0xdbff)))return false;
   if(s.HexDigits!=4)return true;
   if(s.Escape==5){var point=0x10000+((s.High-0xd800)<<10)+(s.HexValue-0xdc00);s.Escape=0;s.High=0;return AddScalar(ref s,point,ending);}
   if(s.HexValue>=0xd800&&s.HexValue<=0xdbff){s.High=s.HexValue;s.Escape=3;return true;}s.Escape=0;return AddScalar(ref s,s.HexValue,ending);
  }
  private static bool Touches(int low,int high,int allowedLow,int allowedHigh)=>low<=allowedHigh&&high>=allowedLow;
  private static bool AddScalar(ref State s,int point,bool ending)
  {
   var count=s.Characters+(point>=0x10000?2:1);var nonWhite=point>0xffff||!char.IsWhiteSpace((char)point);if(count>280||!s.NonWhite&&!nonWhite&&(count==280||ending))return false;s.Characters=count;s.NonWhite|=nonWhite;return true;
  }
 }
}
