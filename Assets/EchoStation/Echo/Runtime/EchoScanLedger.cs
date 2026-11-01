using System;
using System.Collections.Generic;
using EchoStation.World;

namespace EchoStation.Echo
{
    public sealed class EchoScanReceipt
    {
        public string TargetId{get;}public string Summary{get;}public bool HiddenMark{get;}public bool IsNewDiscovery{get;}
        internal EchoScanReceipt(string id,string summary,bool hidden,bool fresh){TargetId=id;Summary=summary;HiddenMark=hidden;IsNewDiscovery=fresh;}
    }
    public sealed class EchoScanLedger
    {
        private readonly Dictionary<string,EchoScanReceipt> known=new Dictionary<string,EchoScanReceipt>(StringComparer.Ordinal);
        public event Action<EchoScanReceipt> Discovered;
        public string[] GetDiscoveredIds(){var result=new string[known.Count];known.Keys.CopyTo(result,0);return result;}
        public bool HasDiscovered(string id)=>id!=null&&known.ContainsKey(id);
        internal void ClearForPersistence()=>known.Clear();
        // Public clients cannot bypass sensor validation and commit a discovery directly.
        internal bool TryCommit(WorldTarget target,out EchoScanReceipt receipt)
            =>Commit(target,out receipt,true);
        internal bool RestoreCommit(WorldTarget target)=>Commit(target,out _,false);
        private bool Commit(WorldTarget target,out EchoScanReceipt receipt,bool publish)
        {
            receipt=null;if(!target||!target.isActiveAndEnabled||!TargetRegistry.IsValidId(target.Id))return false;
            var permissions=target.GetComponent<EchoTargetPermissions>();if(!permissions||!permissions.CanInvestigate)return false;
            var clue=target.GetComponent<ClueSource>();string summary;bool hidden;
            if(clue)
            {
                try{if(!clue.TryRevealFromEcho(out var record))return false;summary=record.Summary;hidden=record.Kind==ClueKind.Scan;}
                catch(ArgumentException){return false;}
            }
            else{summary=permissions.ScanFact;hidden=false;if(string.IsNullOrWhiteSpace(summary)||summary.Length>120)return false;}
            var fresh=!known.ContainsKey(target.Id);receipt=new EchoScanReceipt(target.Id,summary,hidden,fresh);
            if(fresh){known.Add(target.Id,receipt);if(publish)Discovered?.Invoke(receipt);}return true;
        }
    }
}
