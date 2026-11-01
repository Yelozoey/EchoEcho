using System;
using EchoStation.Echo;
using EchoStation.World;
using UnityEngine;

namespace EchoStation.Player
{
    [RequireComponent(typeof(PlayerInteraction)),DisallowMultipleComponent]
    public sealed class PlayerJournal:MonoBehaviour
    {
        [SerializeField]private EchoScanController echo;
        private JournalStore store;private PlayerInteraction interaction;private bool interactionAttached;private EchoScanLedger ledger;
        public JournalStore Store=>store??(store=new JournalStore());
        public JournalAddResult LastResult{get;private set;}
        public string LastError{get;private set;}="";
        private void Awake(){interaction=GetComponent<PlayerInteraction>();_=Store;}
        private void OnEnable()
        {
            if(!interaction)interaction=GetComponent<PlayerInteraction>();if(!interactionAttached){interaction.ClueRead+=ArchiveClue;interactionAttached=true;}AttachEcho();
        }
        public void BindEcho(EchoScanController source){DetachEcho();echo=source;if(isActiveAndEnabled)AttachEcho();}
        private void AttachEcho()
        {
            if(ledger!=null||!echo)return;ledger=echo.Scanner.Ledger;ledger.Discovered+=ArchiveReceipt;
            foreach(var id in ledger.GetDiscoveredIds())ArchiveScan(id,null);
        }
        private void DetachEcho(){if(ledger!=null)ledger.Discovered-=ArchiveReceipt;ledger=null;}
        private void ArchiveClue(ClueRecord record)
        {
            JournalEntry entry;try{entry=JournalEntry.FromClue(record);}catch(ArgumentException exception){LastResult=JournalAddResult.Invalid;LastError=exception.Message;return;}Add(entry);
        }
        private void ArchiveReceipt(EchoScanReceipt receipt){if(receipt!=null&&ledger!=null&&ledger.HasDiscovered(receipt.TargetId))ArchiveScan(receipt.TargetId,receipt.Summary);}
        private void ArchiveScan(string id,string receiptSummary)
        {
            if(ledger==null||!ledger.HasDiscovered(id)||!WorldTarget.Active.TryGet(id,out var target)||!target.isActiveAndEnabled){LastError="扫描目标目前不可用。";return;}
            var clue=target.GetComponent<ClueSource>();
            if(clue){if(clue.isActiveAndEnabled&&clue.TryRead(out var record))ArchiveClue(record);else LastError="扫描线索尚未授权读取。";return;}
            var permissions=target.GetComponent<EchoTargetPermissions>();if(!permissions||!permissions.CanInvestigate){LastError="扫描信息权限不足。";return;}
            var summary=receiptSummary??permissions.ScanFact;
            JournalEntry entry;try{entry=new JournalEntry(id,target.DisplayName+" · 扫描记录","Echo已完成对"+target.DisplayName+"的扫描。\n"+summary,summary,ClueKind.Scan);}catch(ArgumentException exception){LastResult=JournalAddResult.Invalid;LastError=exception.Message;return;}Add(entry);
        }
        private void Add(JournalEntry entry){LastResult=Store.Add(entry);LastError=LastResult==JournalAddResult.Conflict?"同一线索出现冲突记录。":LastResult==JournalAddResult.CapacityReached?"日志容量已满。":"";}
        private void OnDisable(){if(interactionAttached&&interaction)interaction.ClueRead-=ArchiveClue;interactionAttached=false;DetachEcho();}
    }
}
