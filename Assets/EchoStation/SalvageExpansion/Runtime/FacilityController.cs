using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace EchoStation.SalvageExpansion
{
    [DefaultExecutionOrder(20)]
    public sealed class FacilityController:MonoBehaviour,IExpansionSaveSection
    {
        [SerializeField]FacilityDevice[] devices;
        ExpansionWorld world;bool initialized,removed,archived;string lastSlot;
        public bool ValveClosed{get;private set;}
        public bool Isolated{get;private set;}
        public int Clamps{get;private set;}
        public float Charge{get;private set;}=90;
        public float DataSeconds{get;private set;}
        public long Revision{get;private set;}
        public bool DataReady=>DataSeconds>=8;
        public bool CoreRemoved=>removed||world&&world.Inventory!=null&&world.Inventory.Get(ExpansionCatalog.Core).Location!=InventoryItemLocation.World;
        public bool Archived=>removed?archived:CoreRemoved&&DataReady;
        public string Key=>"facility";
        public Func<FacilityDevice,bool> ToolRequested{get;set;}
        public FacilityDevice[] Devices=>devices?.ToArray()??Array.Empty<FacilityDevice>();
        public string BatterySlot
        {get{if(!world||world.Inventory==null)return null;var row=world.Inventory.Get(ExpansionCatalog.Battery);return row.Location==InventoryItemLocation.Installed?row.Slot:null;}}
        public bool FanPowered=>Charge>0&&BatterySlot=="fan_socket";
        public bool DataPowered=>Charge>0&&BatterySlot=="data_socket";
        public bool SteamSuppressed=>ValveClosed||FanPowered;
        public bool CanRemoveCore=>CoreRemoved||Isolated&&Clamps==7;
        public void Configure(FacilityDevice[] owned){if(initialized)throw new InvalidOperationException("Facility already initialized.");devices=owned;}
        void Start()=>Initialize();
        public void Initialize()
        {
            if(initialized)return;world=GetComponent<ExpansionWorld>();if(!world)throw new InvalidOperationException("Expansion world required.");world.Initialize();
            if(devices==null||devices.Length!=7||devices.Any(d=>!d||d.gameObject.scene!=gameObject.scene)||devices.Select(d=>d.Kind).Distinct().Count()!=7)throw new InvalidOperationException("Seven unique owned devices required.");
            var persistence=GetComponent<ExpansionPersistence>();if(!persistence)persistence=gameObject.AddComponent<ExpansionPersistence>();persistence.Register(this);world.PickupGuard=id=>id==ExpansionCatalog.Core&&!CanRemoveCore?"核心仍带电或夹具未解除。先隔离供电，再逐个处理夹具。":"";world.InteractOther=Interact;world.DescribeOther=hit=>{var d=hit.collider.GetComponentInParent<FacilityDevice>();return Owns(d)?"E "+d.DisplayName:"";};world.InstalledPosition=id=>{var d=devices.FirstOrDefault(item=>item.Id==id);return d?(Vector3?)(d.transform.position+Vector3.up*.5f):null;};lastSlot=BatterySlot;initialized=true;Present();
        }
        bool Owns(FacilityDevice device)=>device&&devices.Contains(device);
        bool Aim(FacilityDevice device)=>Owns(device)&&world.ActionsAllowed&&!world.Pause.Paused&&world.Input.HasApplicationFocus&&world.TryRay(out var hit)&&hit.collider.GetComponentInParent<FacilityDevice>()==device;
        public bool Interact(RaycastHit hit)
        {
            var device=hit.collider.GetComponentInParent<FacilityDevice>();if(!Owns(device))return false;
            if(device.Kind==FacilityDeviceKind.FanSocket||device.Kind==FacilityDeviceKind.DataSocket)return Socket(device);
            if(ToolRequested!=null)return ToolRequested(device);world.Say("请装备相应工具，再操作 "+device.DisplayName+"。");return true;
        }
        public bool Socket(FacilityDevice device)
        {
            if(!Aim(device)||device.Kind!=FacilityDeviceKind.FanSocket&&device.Kind!=FacilityDeviceKind.DataSocket)return false;
            var battery=world.Inventory.Get(ExpansionCatalog.Battery);string error;
            if(battery.Location==InventoryItemLocation.Installed&&battery.Slot==device.Id)
            {
                for(int y=0;y<4;y++)for(int x=0;x<6;x++)if(world.Inventory.Board().CheckPlacement(world.Inventory.Definition(battery.Id).Footprint,x,y,battery.Rotation)==InventoryPlacementStatus.Valid)
                {if(!world.Inventory.TryRecoverInstalled(battery.Id,device.Id,x,y,battery.Rotation,out error)){world.Say(error);return false;}Changed("已收回便携电池。");return true;}
                world.Say("背包空间不足，电池仍保留在插口。");return false;
            }
            if(world.Inventory.HeldId!=ExpansionCatalog.Battery){world.Say(battery.Location==InventoryItemLocation.Installed?"电池仍在另一个设备插口。":"先在背包中装备便携电池。");return false;}
            if(!world.Inventory.TryInstall(ExpansionCatalog.Battery,device.Id,out error)){world.Say(error);return false;}Changed("便携电池已接入 "+device.DisplayName+"。");return true;
        }
        public bool TryIsolate(FacilityDevice device)
        {
            if(!Aim(device)||device.Kind!=FacilityDeviceKind.Isolator||world.Inventory.HeldId!=ExpansionCatalog.Insulator){world.Say("需要装备绝缘工具并对准隔离器。");return false;}
            if(Isolated){world.Say("主供电已隔离。");return false;}Isolated=true;Changed("主供电已隔离，漏电区域安全。核心夹具可处理。");return true;
        }
        public bool TryReleaseClamp(FacilityDevice device)
        {
            int index=Owns(device)?(int)device.Kind-(int)FacilityDeviceKind.ClampA:-1;
            if(index<0||index>2||!Aim(device)||!Isolated||world.Inventory.HeldId!=ExpansionCatalog.Wrench){world.Say("先隔离主供电，再装备扳手对准夹具。");return false;}
            if((Clamps&(1<<index))!=0){world.Say("该夹具已解除。");return false;}Clamps|=1<<index;Changed(Clamps==7?"三个夹具均已解除，可以收纳核心。":"夹具已解除。");return true;
        }
        public bool TryCloseValve(FacilityDevice device)
        {
            if(!Aim(device)||device.Kind!=FacilityDeviceKind.Valve||world.Inventory.HeldId!=ExpansionCatalog.Wrench){world.Say("需要扳手并保持对准阀门。");return false;}
            if(ValveClosed)return false;ValveClosed=true;Changed("蒸汽阀门已关闭。");return true;
        }
        void Changed(string message){Revision++;lastSlot=BatterySlot;world.SyncViews();world.Say(message);Present();}
        public void Advance(float seconds,bool paused)
        {
            if(!float.IsFinite(seconds)||seconds<0||seconds>1)throw new ArgumentOutOfRangeException(nameof(seconds));if(paused||!world.ActionsAllowed)return;
            if(!removed&&world.Inventory.Get(ExpansionCatalog.Core).Location!=InventoryItemLocation.World){removed=true;archived=DataReady;Revision++;}
            if(lastSlot!=BatterySlot){lastSlot=BatterySlot;Revision++;}
            bool ready=DataReady,charged=Charge>0;float used=BatterySlot==null?0:Mathf.Min(Charge,seconds);
            if(DataPowered&&!removed)DataSeconds=Mathf.Min(8,DataSeconds+used);Charge=Mathf.Max(0,Charge-used);
            if(ready!=DataReady||charged!=(Charge>0))Revision++;Present();
        }
        void Update(){if(initialized)Advance(Mathf.Min(Time.deltaTime,.1f),world.Pause.Paused||!world.Input.HasApplicationFocus);}
        void Present()
        {
            if(!initialized&&world==null)return;
            foreach(var d in devices)
            {
                bool active=false;string status;
                if(d.Kind==FacilityDeviceKind.FanSocket){active=FanPowered;status=BatterySlot==d.Id?(active?"通风运行":"电池耗尽"):"E 接入电池";}
                else if(d.Kind==FacilityDeviceKind.DataSocket){active=DataPowered;status=DataReady?"校验完成":active?"正在校验":"E 接入电池";}
                else if(d.Kind==FacilityDeviceKind.Isolator){active=Isolated;status=Isolated?"已隔离":"需要绝缘工具";}
                else if(d.Kind==FacilityDeviceKind.Valve){active=ValveClosed;status=ValveClosed?"已关闭":"需要扳手";}
                else{active=(Clamps&(1<<((int)d.Kind-(int)FacilityDeviceKind.ClampA)))!=0;status=active?"已解除":Isolated?"需要扳手":"先隔离供电";}
                d.Present(active,status);
            }
        }
        public JToken Capture()=>new JObject{{"valve",ValveClosed},{"isolated",Isolated},{"clamps",Clamps},{"charge",Charge},{"data",DataSeconds},{"removed",CoreRemoved},{"archived",Archived}};
        void Apply(JObject value)
        {ValveClosed=(bool)value["valve"];Isolated=(bool)value["isolated"];Clamps=(int)value["clamps"];Charge=(float)value["charge"];DataSeconds=(float)value["data"];removed=(bool)value["removed"];archived=(bool)value["archived"];lastSlot=BatterySlot;Revision++;Present();}
        public bool TryPrepare(ExpansionSnapshot snapshot,JToken value,out Action apply,out Action rollback,out string error)
        {
            apply=rollback=null;error="";try
            {
                var v=SaveSectionJson.Fields(value,"valve","isolated","clamps","charge","data","removed","archived");SaveSectionJson.Boolean(v["valve"]);bool isolated=SaveSectionJson.Boolean(v["isolated"]);int clamps=SaveSectionJson.Integer(v["clamps"],0,7);SaveSectionJson.Number(v["charge"],0,90);float progress=SaveSectionJson.Number(v["data"],0,8);bool removedValue=SaveSectionJson.Boolean(v["removed"]),archivedValue=SaveSectionJson.Boolean(v["archived"]);
                var battery=snapshot.Inventory.Single(r=>r.Id==ExpansionCatalog.Battery);if(battery.Location==InventoryItemLocation.Installed&&battery.Slot!="fan_socket"&&battery.Slot!="data_socket")throw new ArgumentException("Unknown facility socket.");
                if(!removedValue&&snapshot.Inventory.Single(r=>r.Id==ExpansionCatalog.Core).Location!=InventoryItemLocation.World||removedValue&&(!isolated||clamps!=7)||archivedValue&&(!removedValue||progress<8))throw new ArgumentException("Core/device conditions inconsistent.");
                var before=(JObject)Capture();long oldRevision=Revision;string oldSlot=lastSlot;var frozen=(JObject)v.DeepClone();apply=()=>Apply(frozen);rollback=()=>{Apply(before);Revision=oldRevision;lastSlot=oldSlot;};return true;
            }
            catch(Exception e)when(e is ArgumentException||e is OverflowException){error=e.Message;return false;}
        }
    }
}
