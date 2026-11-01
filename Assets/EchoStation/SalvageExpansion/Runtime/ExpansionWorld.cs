using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EchoStation.SalvageExpansion
{
    [DefaultExecutionOrder(-20)]
    public sealed class ExpansionWorld:MonoBehaviour
    {
        [SerializeField]SalvageMotor motor;
        [SerializeField]Transform actorContainer;
        [SerializeField]ExpansionItemActor[] actors;
        readonly Dictionary<string,Vector3> positions=new Dictionary<string,Vector3>(StringComparer.Ordinal);
        readonly Dictionary<string,ExpansionItemActor> views=new Dictionary<string,ExpansionItemActor>(StringComparer.Ordinal);
        readonly Dictionary<string,NavMeshObstacle> obstacles=new Dictionary<string,NavMeshObstacle>(StringComparer.Ordinal);
        readonly RaycastHit[] hits=new RaycastHit[32];
        PlayerInputReader input;Transform anchor;Text hud,cross,confirmation;GameObject hudRoot,crossRoot;
        IDisposable deliveryLease;string[] pending;long pendingRevision,synced=-1;float hudAt;bool recapture;
        public static readonly Vector3 Dock=new Vector3(5,0,12);
        public InventoryStore Inventory{get;private set;}
        public BackpackPanel Backpack{get;private set;}
        public ExpansionPause Pause{get;private set;}
        public SalvageMotor Motor=>motor;
        public PlayerInputReader Input=>input;
        public bool ActionsAllowed{get;set;}=true;
        public bool DeliveryPending=>pending!=null;
        public bool AtDock=>Vector3.Distance(transform.position,Dock)<=2;
        public string Message{get;private set;}="I 打开背包，选择工具后装备。";
        public Func<string,string> PickupGuard{get;set;}
        public Func<RaycastHit,bool> InteractOther{get;set;}
        public Func<RaycastHit,string> DescribeOther{get;set;}
        public Func<Vector3,bool> SafeDrop{get;set;}
        public Func<string,Vector3?> InstalledPosition{get;set;}
        public Func<string> ExtraHud{get;set;}
        public IReadOnlyDictionary<string,Vector3> WorldPositions=>positions;
        public IReadOnlyDictionary<string,ExpansionItemActor> Actors=>views;
        public void Configure(SalvageMotor playerMotor,Transform container,ExpansionItemActor[] items)
        {motor=playerMotor;actorContainer=container;actors=items;}
        void Start()=>Initialize();
        public void Initialize()
        {
            if(Inventory!=null)return;
            if(!motor||!motor.View||!actorContainer||actors==null||actors.Length!=ExpansionCatalog.Items.Count)throw new InvalidOperationException("World bindings missing.");
            input=GetComponent<PlayerInputReader>();if(!input)throw new InvalidOperationException("Player input required.");
            foreach(var actor in actors)
            {
                if(!actor||actor.gameObject.scene!=gameObject.scene||views.ContainsKey(actor.Id)||!ExpansionCatalog.Items.Any(d=>d.Id==actor.Id))throw new InvalidOperationException("Owned item views must be unique and complete.");
                views.Add(actor.Id,actor);positions.Add(actor.Id,actor.transform.position);var obstacle=actor.GetComponent<NavMeshObstacle>();if(!obstacle)obstacle=actor.gameObject.AddComponent<NavMeshObstacle>();obstacle.enabled=false;obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=actor.Body.center;obstacle.size=actor.Body.size;obstacle.carving=true;obstacle.carveOnlyStationary=true;obstacle.carvingMoveThreshold=.1f;obstacle.carvingTimeToStationary=.2f;obstacles.Add(actor.Id,obstacle);
            }
            Inventory=ExpansionCatalog.NewInventory();anchor=new GameObject("ExpansionHeldAnchor").transform;anchor.SetParent(motor.View,false);anchor.localPosition=new Vector3(.25f,-.52f,.85f);
            Pause=GetComponent<ExpansionPause>();if(!Pause)Pause=gameObject.AddComponent<ExpansionPause>();Pause.Configure(input,new Behaviour[]{motor});
            Backpack=GetComponent<BackpackPanel>();if(!Backpack)Backpack=gameObject.AddComponent<BackpackPanel>();Backpack.Configure(Inventory,Pause,input);Backpack.DropItem=DropFromBackpack;
            PickupGuard=id=>id==ExpansionCatalog.Core?"核心仍被设备固定，请先检查设备。":"";
            hud=SalvageUi.Panel(transform,"ExpansionHud",760,300,10);hudRoot=SalvageUi.CanvasOf(hud);var rect=(RectTransform)hud.transform.parent;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(20,-20);hud.fontSize=20;
            cross=SalvageUi.Panel(transform,"ExpansionCrosshair",32,32,9);crossRoot=SalvageUi.CanvasOf(cross);cross.transform.parent.GetComponent<Image>().color=Color.clear;cross.enabled=false;Crossbar("Horizontal",new Vector2(12,2));Crossbar("Vertical",new Vector2(2,12));
            SyncViews();RefreshHud();
        }
        public void Say(string value){Message=value??"";RefreshHud();}
        void Crossbar(string name,Vector2 size){var rect=SalvageWidgets.Rect(name,cross.transform.parent,0,0,size.x,size.y);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.78f,.9f,1,.95f);image.raycastTarget=false;}
        public bool TryRay(out RaycastHit result)
        {
            result=default;if(!motor||!motor.View)return false;
            int count=Physics.RaycastNonAlloc(motor.View.position,motor.View.forward,hits,3,~0,QueryTriggerInteraction.Ignore);if(count==hits.Length)return false;
            bool found=false;float distance=float.PositiveInfinity;
            for(int i=0;i<count;i++)if(!hits[i].collider.transform.IsChildOf(transform)&&hits[i].distance<distance){distance=hits[i].distance;result=hits[i];found=true;}
            return found;
        }
        public bool TryPickupFromView()
        {
            if(!ActionsAllowed||!input||!input.CanPlay||Pause.Blocked)return Reject("当前不能拾取。");
            if(!TryRay(out var hit))return Reject("请靠近并对准回收物。");
            var actor=hit.collider.GetComponentInParent<ExpansionItemActor>();
            if(!actor||!views.TryGetValue(actor.Id,out var expected)||actor!=expected||Inventory.Get(actor.Id).Location!=InventoryItemLocation.World)return Reject("不是当前可拾取物。");
            string blocked=PickupGuard?.Invoke(actor.Id);if(!string.IsNullOrEmpty(blocked))return Reject(blocked);
            if(!Inventory.TryQuickStow(actor.Id,out var error))return Reject(error);
            SyncViews();Say("已收纳 "+Inventory.Definition(actor.Id).Name+" · I 整理背包");return true;
        }
        string DropFromBackpack(string id){TryDrop(id,true);return Message;}
        public bool TryDrop(string id,bool fromBackpack=false)
        {
            if(!ActionsAllowed||!input||!input.HasApplicationFocus||(!fromBackpack&&!input.CanPlay)||(fromBackpack&&!Backpack.IsOpen))return Reject("当前不能放下。");
            var row=Inventory.Records.FirstOrDefault(r=>r.Id==id);if(row==null||!row.OccupiesGrid||!views.TryGetValue(id,out var actor))return Reject("物品不在背包或手中。");
            var forward=Vector3.ProjectOnPlane(motor.View.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.5f)return Reject("请朝向前方安全地面。");
            var origin=transform.position+forward*1.2f+Vector3.up*1.8f;
            if(!Physics.Raycast(origin,Vector3.down,out var hit,3,~0,QueryTriggerInteraction.Ignore)||hit.normal.y<.9f||!hit.collider.GetComponentInParent<SalvageDropSurface>())return Reject("前方没有安全地面。");
            Vector3 size=Vector3.Scale(actor.Body.size,actor.transform.lossyScale);size=new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));
            var center=hit.point+Vector3.up*(size.y*.5f+.02f);var position=center-Vector3.Scale(actor.Body.center,actor.transform.lossyScale);
            if(SafeDrop!=null&&!SafeDrop(position))return Reject("落点处于危险区域。");
            if(Physics.OverlapBox(center,size*.49f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Any(c=>c!=hit.collider&&!c.transform.IsChildOf(transform)&&!c.transform.IsChildOf(actor.transform)))return Reject("落点已被占用。");
            if(!Inventory.TryDrop(id,out var error))return Reject(error);positions[id]=position;SyncViews();Say("已放下 "+Inventory.Definition(id).Name);return true;
        }
        public bool BeginDelivery()
        {
            if(!ActionsAllowed||DeliveryPending||!AtDock||Pause.Blocked||!input.HasApplicationFocus)return Reject("请在泊位交付，先关闭其他面板。");
            pending=Inventory.Records.Where(r=>r.OccupiesGrid&&Inventory.Definition(r.Id).Deliverable).Select(r=>r.Id).ToArray();
            if(pending.Length==0){pending=null;return Reject("背包里没有待交付回收物。");}
            pendingRevision=Inventory.Revision;deliveryLease=Pause.Acquire();
            if(!confirmation)confirmation=SalvageUi.Panel(transform,"ExpansionDelivery",820,440,130);
            confirmation.text="确认批量交付\n\n"+string.Join("\n",pending.Select(id=>Inventory.Definition(id).Name))+"\n\n工具和电池会保留。\nEnter 确认 / Esc 取消";
            SalvageUi.CanvasOf(confirmation).SetActive(true);return true;
        }
        public bool ConfirmDelivery()
        {
            if(!DeliveryPending)return false;
            if(!ActionsAllowed||!AtDock||!input.HasApplicationFocus||Inventory.Revision!=pendingRevision){CancelDelivery();return Reject("位置或库存已变化，交付已取消。");}
            var ids=pending;bool result=Inventory.TryDeliver(ids,out var error);CancelDelivery();if(!result)return Reject(error);SyncViews();Say("已交付 "+ids.Length+" 件回收物。");return true;
        }
        public void CancelDelivery(){pending=null;if(confirmation)SalvageUi.CanvasOf(confirmation).SetActive(false);deliveryLease?.Dispose();deliveryLease=null;recapture=true;}
        public void SyncViews()
        {
            if(Inventory==null)return;
            foreach(var row in Inventory.Records)
            {
                var actor=views[row.Id];if(!actor)throw new InvalidOperationException("Owned world item was destroyed: "+row.Id);var obstacle=obstacles[row.Id];if(obstacle)obstacle.enabled=false;
                if(row.Location==InventoryItemLocation.World){actor.transform.SetParent(actorContainer,false);actor.transform.SetPositionAndRotation(positions[row.Id],Quaternion.identity);actor.Body.enabled=true;actor.gameObject.SetActive(true);}
                else if(row.Location==InventoryItemLocation.Held){actor.Body.enabled=false;actor.transform.SetParent(anchor,false);actor.transform.localPosition=Vector3.zero;actor.transform.localRotation=Quaternion.identity;actor.gameObject.SetActive(true);}
                else if(row.Location==InventoryItemLocation.Installed&&InstalledPosition!=null&&InstalledPosition(row.Slot).HasValue){actor.Body.enabled=false;actor.transform.SetParent(actorContainer,false);actor.transform.SetPositionAndRotation(InstalledPosition(row.Slot).Value,Quaternion.identity);actor.gameObject.SetActive(true);}
                else{actor.Body.enabled=false;actor.transform.SetParent(actorContainer,true);actor.gameObject.SetActive(false);}if(obstacle)obstacle.enabled=row.Location==InventoryItemLocation.World&&actor.Body.enabled;
            }
            motor.Speed=Inventory.HeldId==null?3.5f:3.5f-.1f*Inventory.Definition(Inventory.HeldId).Kilograms;synced=Inventory.Revision;Physics.SyncTransforms();
        }
        public bool TrySetWorldPositions(IReadOnlyDictionary<string,Vector3> snapshot)
        {
            if(snapshot==null||snapshot.Count!=views.Count||snapshot.Any(p=>!views.ContainsKey(p.Key)||!float.IsFinite(p.Value.x)||!float.IsFinite(p.Value.y)||!float.IsFinite(p.Value.z)))return false;
            foreach(var row in snapshot.ToArray())positions[row.Key]=row.Value;SyncViews();return true;
        }
        public void ApplyInventorySnapshot(InventoryStore candidate,IReadOnlyDictionary<string,Vector3> worldPositions,Vector3 position,float yaw,float pitch)
        {
            if(candidate==null||candidate.Definitions.Count!=views.Count||candidate.Definitions.Any(d=>!views.ContainsKey(d.Id))||views.Values.Any(v=>!v)||!motor||!GetComponent<CharacterController>())throw new InvalidOperationException("Restore bindings invalid.");
            if(worldPositions==null||worldPositions.Count!=views.Count||worldPositions.Any(p=>!views.ContainsKey(p.Key)||!float.IsFinite(p.Value.x)||!float.IsFinite(p.Value.y)||!float.IsFinite(p.Value.z)))throw new ArgumentException("Invalid world positions.");
            var poseCopy=worldPositions.ToArray();CancelDelivery();Backpack.Close();Inventory=candidate;foreach(var row in poseCopy)positions[row.Key]=row.Value;motor.RestorePose(position,yaw,pitch);Backpack.Configure(Inventory,Pause,input);Backpack.DropItem=DropFromBackpack;SyncViews();Backpack.Render();
        }
        bool Reject(string value){Say(value);return false;}
        void RefreshHud()
        {
            if(!hud||Inventory==null)return;
            string focus="";if(input&&input.CanPlay&&TryRay(out var hit)){var actor=hit.collider.GetComponentInParent<ExpansionItemActor>();focus=actor&&views.TryGetValue(actor.Id,out var owned)&&owned==actor?"E 收纳 "+Inventory.Definition(actor.Id).Name:DescribeOther?.Invoke(hit)??"";}
            bool core=Inventory.Get(ExpansionCatalog.Core).Location==InventoryItemLocation.Delivered;
            hud.text=string.Join("\n",new[]{"取回数据核心  "+(core?"已交付 · Tab 结算":"未交付"),"I 背包  E 交互/泊位交付  G 放下  R Echo  Tab 任务","手持："+(Inventory.HeldId==null?"无":Inventory.Definition(Inventory.HeldId).Name)+"   占格 "+Inventory.Board().OccupiedCells.Count+"/24",ExtraHud?.Invoke(),AtDock?"泊位：E 预览交付清单":focus,Message}.Where(line=>!string.IsNullOrEmpty(line)));
        }
        void Update()
        {
            if(Inventory==null)return;if(synced!=Inventory.Revision)SyncViews();if(DeliveryPending&&!input.HasApplicationFocus)CancelDelivery();
            if(DeliveryPending){if(Keyboard.current!=null){if(Keyboard.current.escapeKey.wasPressedThisFrame)CancelDelivery();else if(Keyboard.current.enterKey.wasPressedThisFrame)ConfirmDelivery();}}
            else if(ActionsAllowed&&input.CanPlay&&!Pause.Blocked)
            {
                if(input.InteractPressed){if(AtDock&&Inventory.Records.Any(r=>r.OccupiesGrid&&Inventory.Definition(r.Id).Deliverable))BeginDelivery();else if(TryRay(out var hit)&&hit.collider.GetComponentInParent<ExpansionItemActor>())TryPickupFromView();else if(InteractOther==null||!TryRay(out hit)||!InteractOther(hit))Say("请对准可交互物。");}
                if(Keyboard.current!=null&&Keyboard.current.gKey.wasPressedThisFrame&&Inventory.HeldId!=null)TryDrop(Inventory.HeldId);
            }
            if(Time.unscaledTime>=hudAt){RefreshHud();hudAt=Time.unscaledTime+.15f;}bool visible=!Pause.Blocked;if(hudRoot&&hudRoot.activeSelf!=visible)hudRoot.SetActive(visible);if(crossRoot&&crossRoot.activeSelf!=visible)crossRoot.SetActive(visible);
        }
        void LateUpdate(){if(recapture){recapture=false;if(Pause)Pause.RestorePointer();}}
        void OnDisable(){CancelDelivery();if(Backpack)Backpack.Close();}
    }
}
