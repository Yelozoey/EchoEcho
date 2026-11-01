using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EchoStation.AI.Tokenization
{
 public sealed class QwenByteBpeTokenizer
 {
  private readonly TokenizerAssets assets;
  private readonly AddedToken[] ordinary;
  private readonly byte[][] decoded;
  private readonly bool[] special;
  public int DefinedTokenCount=>decoded.Length;
  public QwenByteBpeTokenizer(TokenizerAssets data)
  {
   assets=data??throw new ArgumentNullException(nameof(data));decoded=new byte[data.ById.Count][];special=new bool[data.ById.Count];ordinary=data.AddedTokens.Where(t=>!t.IsSpecial).ToArray();
   foreach(var token in data.Vocabulary)decoded[token.Value]=ByteAlphabet.DecodeBytes(token.Key);
   foreach(var token in data.AddedTokens){decoded[token.Id]=Encoding.UTF8.GetBytes(token.Content);special[token.Id]=token.IsSpecial;}
  }
  public int[] EncodePlain(string text)
  {
   var normalized=QwenPreTokenizer.Normalize(text);var output=new List<int>();var position=0;
   while(position<normalized.Length)
   {
    var found=-1;AddedToken match=null;
    foreach(var token in ordinary)
    {var index=normalized.IndexOf(token.Content,position,StringComparison.Ordinal);if(index>=0&&(found<0||index<found||(index==found&&token.Content.Length>match.Content.Length))){found=index;match=token;}}
    if(found<0){EncodeSegment(normalized.Substring(position),output);break;}
    if(found>position)EncodeSegment(normalized.Substring(position,found-position),output);output.Add(match.Id);position=found+match.Content.Length;
   }
   return output.ToArray();
  }
  private void EncodeSegment(string text,List<int> output){foreach(var piece in QwenPreTokenizer.SplitEncoded(text))EncodePiece(piece,output);}
  private struct Node{public int Id,Previous,Next,Version;public bool Alive;}
  private readonly struct Candidate
  {
   public readonly int Left,Right,LeftVersion,RightVersion,Rank,Result;
   public Candidate(int left,int right,Node[] nodes,TokenizerAssets.MergeRule rule){Left=left;Right=right;LeftVersion=nodes[left].Version;RightVersion=nodes[right].Version;Rank=rule.Rank;Result=rule.Result;}
  }
  private sealed class Heap
  {
   private readonly List<Candidate> values;
   public Heap(int capacity){values=new List<Candidate>(capacity);}public int Count=>values.Count;
   private static bool Before(Candidate a,Candidate b)=>a.Rank<b.Rank||(a.Rank==b.Rank&&a.Left<b.Left);
   public void Push(Candidate item){values.Add(item);var i=values.Count-1;while(i>0){var p=(i-1)/2;if(!Before(item,values[p]))break;values[i]=values[p];i=p;}values[i]=item;}
   public Candidate Pop()
   {
    var first=values[0];var last=values[values.Count-1];values.RemoveAt(values.Count-1);if(values.Count==0)return first;var i=0;
    while(i*2+1<values.Count){var c=i*2+1;if(c+1<values.Count&&Before(values[c+1],values[c]))c++;if(!Before(values[c],last))break;values[i]=values[c];i=c;}values[i]=last;return first;
   }
  }
  private void EncodePiece(string piece,List<int> output)
  {
   if(piece.Length==0)return;var nodes=new Node[piece.Length];var heap=new Heap(Math.Max(16,piece.Length*2));
   for(var i=0;i<nodes.Length;i++)nodes[i]=new Node{Id=assets.ByteId(piece[i]),Previous=i-1,Next=i+1<nodes.Length?i+1:-1,Alive=true};
   for(var i=0;i<nodes.Length-1;i++)Offer(i,i+1,nodes,heap);
   while(heap.Count>0)
   {
    var pair=heap.Pop();var l=pair.Left;var r=pair.Right;
    if(!nodes[l].Alive||!nodes[r].Alive||nodes[l].Next!=r||nodes[l].Version!=pair.LeftVersion||nodes[r].Version!=pair.RightVersion)continue;
    var previous=nodes[l].Previous;var next=nodes[r].Next;nodes[l].Id=pair.Result;nodes[l].Next=next;nodes[l].Version++;nodes[r].Alive=false;nodes[r].Version++;
    if(next>=0)nodes[next].Previous=l;Offer(previous,l,nodes,heap);Offer(l,next,nodes,heap);
   }
   for(var i=0;i>=0;i=nodes[i].Next)output.Add(nodes[i].Id);
  }
  private void Offer(int left,int right,Node[] nodes,Heap heap){if(left>=0&&right>=0&&assets.TryMerge(nodes[left].Id,nodes[right].Id,out var rule))heap.Push(new Candidate(left,right,nodes,rule));}
  public ReadOnlyMemory<byte> BytesForToken(int id){if(id<0||id>=decoded.Length||decoded[id]==null)throw new InvalidDataException("Unknown tokenizer ID");return decoded[id];}
  public bool IsSpecial(int id){BytesForToken(id);return special[id];}
  public string Decode(IReadOnlyList<int> ids,bool skipSpecial=false,bool strictUtf8=false)
  {
   if(ids==null)throw new ArgumentNullException(nameof(ids));if(ids.Count>131072)throw new ArgumentException("Decode budget exceeded");
   using(var bytes=new MemoryStream())
   {foreach(var id in ids){var value=BytesForToken(id);if(skipSpecial&&special[id])continue;var span=value.Span;for(var i=0;i<span.Length;i++)bytes.WriteByte(span[i]);}return (strictUtf8?new UTF8Encoding(false,true):Encoding.UTF8).GetString(bytes.ToArray());}
  }
 }
 public enum ChatSeparator{EndOfText=151643,Start=151644,End=151645}
 public sealed class TrustedTokenSequence
 {
  private readonly QwenByteBpeTokenizer tokenizer;private readonly List<int> ids=new List<int>();private readonly StringBuilder pending=new StringBuilder();private int textCharacters;
  public TrustedTokenSequence(QwenByteBpeTokenizer value){tokenizer=value??throw new ArgumentNullException(nameof(value));}
  public TrustedTokenSequence Text(string value){if(value==null)throw new ArgumentNullException(nameof(value));if(value.Length>QwenPreTokenizer.MaximumCharacters-textCharacters)throw new ArgumentException("Template text budget exceeded");pending.Append(value);textCharacters+=value.Length;return this;}
  public TrustedTokenSequence Separator(ChatSeparator separator){var id=(int)separator;if(id<151643||id>151645)throw new ArgumentException("Unknown trusted separator");Flush();ids.Add(id);return this;}
  public int[] ToArray(){Flush();return ids.ToArray();}
  private void Flush(){if(pending.Length==0)return;ids.AddRange(tokenizer.EncodePlain(pending.ToString()));pending.Clear();}
 }
}
