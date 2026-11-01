using System;
using System.Collections.Generic;
using System.Linq;
namespace EchoStation.SalvageExpansion
{
    public static class ExpansionCatalog
    {
        public const string Wrench="wrench",Insulator="insulator",Battery="battery",Core="core",Instrument="instrument",Records="records",Samples="samples";
        static InventoryFootprint Rect(int w,int h)=>new InventoryFootprint(Enumerable.Range(0,h).SelectMany(y=>Enumerable.Range(0,w).Select(x=>new InventoryCell(x,y))));
        public static readonly IReadOnlyList<InventoryItemDefinition> Items=Array.AsReadOnly(new[]{
            new InventoryItemDefinition(Wrench,"维修扳手",InventoryItemKind.Tool,Rect(1,3),2),
            new InventoryItemDefinition(Insulator,"绝缘工具",InventoryItemKind.Tool,new InventoryFootprint(new[]{new InventoryCell(0,0),new InventoryCell(0,1),new InventoryCell(1,1)}),1),
            new InventoryItemDefinition(Battery,"便携电池",InventoryItemKind.Battery,Rect(1,2),2),
            new InventoryItemDefinition(Core,"数据核心",InventoryItemKind.Core,Rect(2,3),6),
            new InventoryItemDefinition(Instrument,"精密仪器",InventoryItemKind.Salvage,Rect(2,3),6,10),
            new InventoryItemDefinition(Records,"记录匣",InventoryItemKind.Salvage,new InventoryFootprint(new[]{new InventoryCell(0,0),new InventoryCell(0,1),new InventoryCell(0,2),new InventoryCell(1,2)}),1,6),
            new InventoryItemDefinition(Samples,"样本箱",InventoryItemKind.Salvage,Rect(2,2),2,4)});
        public static InventoryStore NewInventory()
        {
            var store=new InventoryStore(Items);
            if(!store.TryStow(Wrench,0,0,0,out _)||!store.TryStow(Insulator,1,0,0,out _)||!store.TryStow(Battery,1,2,0,out _))throw new InvalidOperationException("Initial loadout must fit.");
            return store;
        }
    }
}
