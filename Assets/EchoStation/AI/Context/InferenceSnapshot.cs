using System;
using System.Collections.Generic;
using System.Text;
using EchoStation.Memory;

namespace EchoStation.AI.Context
{
 public sealed class TargetCandidate
 {
  public string Id {get;}public string Name {get;}public string Zone {get;}public bool Available {get;}public bool CanInvestigate {get;}public bool IsDanger {get;}public bool Visible {get;}public double Distance {get;}public bool KnownReference {get;}
  public TargetCandidate(string id,string name,string zone,bool available,bool investigate,bool danger,bool visible,double distance,bool knownReference=false)
  {
   if(!MemoryLimits.IsValidId(id)||!MemoryLimits.IsValidId(zone)||string.IsNullOrWhiteSpace(name)||name.Length>80||double.IsNaN(distance)||double.IsInfinity(distance)||distance<0)throw new ArgumentException("Invalid bounded target candidate");
   new UTF8Encoding(false,true).GetByteCount(name);Id=id;Name=name;Zone=zone;Available=available;CanInvestigate=investigate;IsDanger=danger;Visible=visible;Distance=distance;KnownReference=knownReference;
  }
 }
 public sealed class AllowedTarget
 {
  public string Id {get;}public string Name {get;}public bool CanInvestigate {get;}public bool IsDanger {get;}
  internal AllowedTarget(TargetCandidate c){Id=c.Id;Name=c.Name;CanInvestigate=c.CanInvestigate;IsDanger=c.IsDanger;}
 }
 public sealed class InferenceSnapshot
 {
  public MemorySnapshot Known {get;}public string Input {get;}public string RetrievalQuery {get;}public string Zone {get;}public double ElapsedGameSeconds {get;}public IReadOnlyList<AllowedTarget> Targets {get;}
  public InferenceSnapshot(MemoryStore source,string input,double elapsedGameSeconds,string zone,IEnumerable<TargetCandidate> candidates)
   :this(source,input,elapsedGameSeconds,zone,candidates,input){}
  // Retain the original constructor/behaviour. A game adapter may instead supply
  // its explicitly selected target ID, without turning player text into memory.
  public InferenceSnapshot(MemoryStore source,string input,double elapsedGameSeconds,string zone,IEnumerable<TargetCandidate> candidates,string retrievalQuery)
  {
   if(source==null)throw new ArgumentNullException(nameof(source));if(string.IsNullOrWhiteSpace(input)||input.Length>200||!MemoryLimits.IsValidId(zone)||double.IsNaN(elapsedGameSeconds)||double.IsInfinity(elapsedGameSeconds)||elapsedGameSeconds<0)throw new ArgumentException("Invalid bounded request snapshot");
   if(string.IsNullOrWhiteSpace(retrievalQuery)||retrievalQuery.Length>200)throw new ArgumentException("Invalid bounded retrieval query");
   var utf8=new UTF8Encoding(false,true);utf8.GetByteCount(input);utf8.GetByteCount(retrievalQuery);Known=source.CaptureSnapshot();Input=input;RetrievalQuery=retrievalQuery;ElapsedGameSeconds=elapsedGameSeconds;Zone=zone;Targets=TargetScope.Select(Known,input,zone,candidates);
  }
 }
 public static class TargetScope
 {
  private sealed class Rank{public TargetCandidate Candidate;public int Explicit;public bool Local;}
  public static IReadOnlyList<AllowedTarget> Select(MemorySnapshot known,string input,string zone,IEnumerable<TargetCandidate> candidates)
  {
   if(known==null||input==null||candidates==null)throw new ArgumentNullException("Target scope");if(input.Length>200||!MemoryLimits.IsValidId(zone))throw new ArgumentException("Target scope budget");
   var references=new HashSet<string>(StringComparer.Ordinal);foreach(var clue in known.Clues)if(clue.KeyMarker!=null)references.Add(clue.KeyMarker);foreach(var state in known.Characters)if(state.Kind==CharacterMemoryKind.KnownDanger&&state.TargetId!=null)references.Add(state.TargetId);
   var ranks=new List<Rank>();var identities=new HashSet<string>(StringComparer.Ordinal);var count=0;
   foreach(var c in candidates)
   {
    if(++count>128||c==null||!identities.Add(c.Id))throw new ArgumentException("Target candidates exceed budget or repeat identity");
    if(!c.Available||(!c.CanInvestigate&&!c.IsDanger))continue;var local=c.Zone==zone&&c.Visible;if(!local&&!references.Contains(c.Id)&&!c.KnownReference)continue;
    ranks.Add(new Rank{Candidate=c,Local=local,Explicit=MentionsId(input,c.Id)?1000+c.Id.Length:input.IndexOf(c.Name,StringComparison.OrdinalIgnoreCase)>=0?c.Name.Length:0});
   }
   ranks.Sort((a,b)=>{var v=b.Explicit.CompareTo(a.Explicit);if(v!=0)return v;v=b.Local.CompareTo(a.Local);if(v!=0)return v;v=a.Candidate.Distance.CompareTo(b.Candidate.Distance);return v!=0?v:string.CompareOrdinal(a.Candidate.Id,b.Candidate.Id);});
   var selected=new AllowedTarget[Math.Min(8,ranks.Count)];for(var i=0;i<selected.Length;i++)selected[i]=new AllowedTarget(ranks[i].Candidate);return Array.AsReadOnly(selected);
  }
  private static bool MentionsId(string input,string id)
  {
   var offset=0;while(offset<=input.Length-id.Length){var i=input.IndexOf(id,offset,StringComparison.OrdinalIgnoreCase);if(i<0)return false;var end=i+id.Length;if((i==0||!AsciiWord(input[i-1]))&&(end==input.Length||!AsciiWord(input[end])))return true;offset=i+1;}return false;
  }
  private static bool AsciiWord(char c)=>(c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='0'&&c<='9')||c=='_';
 }
}
