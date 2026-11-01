using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoStation.Salvage
{
    [RequireComponent(typeof(PlayerInputReader),typeof(SalvageMotor))]
    public sealed class SalvageCarry:MonoBehaviour
    {
        public const float Reach=3;
        CargoRegistry registry;Dictionary<string,CargoActor> actors;Transform anchor;PlayerInputReader input;SalvageMotor motor;
        readonly RaycastHit[] hitBuffer=new RaycastHit[32];
        Dictionary<string,Vector3> initialPositions;Dictionary<string,Transform> initialParents;
        public CargoRegistry Registry{get{Ensure();return registry;}}
        public string HeldId{get;private set;}
        public uint Revision{get;private set;}
        public string LastMessage{get;private set;}="";
        public CargoActor Held=>HeldId!=null?actors[HeldId]:null;
        public bool Initialized=>registry!=null;
        void Ensure()
        {
            if(registry!=null)return;
            input=GetComponent<PlayerInputReader>();motor=GetComponent<SalvageMotor>();registry=new CargoRegistry();actors=new Dictionary<string,CargoActor>(StringComparer.Ordinal);
            foreach(var a in gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CargoActor>(true)))
            {if(!registry.Register(SalvageCatalog.Get(a.Id),a.transform.position))throw new InvalidOperationException("Duplicate scene cargo "+a.Id);actors.Add(a.Id,a);}
            initialPositions=actors.ToDictionary(p=>p.Key,p=>p.Value.transform.position);initialParents=actors.ToDictionary(p=>p.Key,p=>p.Value.transform.parent);
            anchor=new GameObject("CargoHoldAnchor").transform;anchor.SetParent(motor.View,false);anchor.localPosition=new Vector3(.25f,-.52f,.85f);
        }
        void Start()=>Ensure();
        void Update()
        {
            Ensure();if(!input.CanPlay)return;
            if(input.InteractPressed){var delivery=GetComponent<SalvageDelivery>();if(HeldId!=null&&delivery&&delivery.AtDock)delivery.Request();else TryPickFromView();}
            if(Keyboard.current!=null&&Keyboard.current.gKey.wasPressedThisFrame)TryDrop();
        }
        public bool TryPickFromView()
        {
            Ensure();if(!input.CanPlay)return Reject("菜单中不能拾取。");if(HeldId!=null)return Reject("请先放下手中的物品。");
            var actor=Focus();if(!actor||!actors.TryGetValue(actor.Id,out var expected)||expected!=actor)return Reject("请靠近并对准可见回收物。");
            var record=registry.Get(actor.Id);if(record.State!=CargoState.World)return Reject("物品状态已变化。");
            if(record.Definition.Kilograms>8)return Reject("超过8kg单件上限，Echo也不能代搬。");
            actor.Body.enabled=false;actor.transform.SetParent(anchor,false);actor.transform.localPosition=Vector3.zero;actor.transform.localRotation=Quaternion.identity;
            record.State=CargoState.Held;HeldId=actor.Id;motor.Speed=3.5f-.1f*record.Definition.Kilograms;LastMessage="已拿起 "+record.Definition.Name;Revision++;return true;
        }
        public CargoActor Focus()
        {
            Ensure();int count=Physics.RaycastNonAlloc(motor.View.position,motor.View.forward,hitBuffer,Reach,~0,QueryTriggerInteraction.Ignore);
            if(count==hitBuffer.Length)return null;Collider nearest=null;float distance=float.PositiveInfinity;
            for(int i=0;i<count;i++){var h=hitBuffer[i];if(h.collider.transform.IsChildOf(transform))continue;if(h.distance<distance){nearest=h.collider;distance=h.distance;}}
            return nearest?nearest.GetComponentInParent<CargoActor>():null;
        }
        public bool TryDrop()
        {
            Ensure();if(!input.CanPlay)return Reject("菜单中不能放下。");if(HeldId==null)return Reject("手中没有物品。");
            var direction=Vector3.ProjectOnPlane(motor.View.forward,Vector3.up).normalized;if(direction.sqrMagnitude<.5f)return Reject("请朝向地面前方。");
            var origin=transform.position+direction*1.15f+Vector3.up*1.8f;
            if(!Physics.Raycast(origin,Vector3.down,out var hit,3,~0,QueryTriggerInteraction.Ignore)||hit.normal.y<.9f||!hit.collider.GetComponentInParent<SalvageDropSurface>())return Reject("前方没有安全地面，物品仍在手中。");
            var actor=Held;var box=actor.Body;var position=hit.point+Vector3.up*(box.size.y*.5f-box.center.y+.02f);
            var overlaps=Physics.OverlapBox(position+box.center,box.size*.49f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
            if(overlaps.Any(c=>c!=box&&c!=hit.collider&&!c.transform.IsChildOf(actor.transform)))return Reject("落点被占用，物品仍在手中。");
            actor.transform.SetParent(null);actor.transform.SetPositionAndRotation(position,Quaternion.identity);box.enabled=true;
            var record=registry.Get(HeldId);record.Position=position;record.State=CargoState.World;HeldId=null;motor.Speed=3.5f;LastMessage="已放下 "+record.Definition.Name;Revision++;return true;
        }
        internal bool Deliver(string expectedId)
        {
            Ensure();if(HeldId!=expectedId||HeldId==null)return false;var actor=Held;var record=registry.Get(HeldId);if(record.State!=CargoState.Held)return false;
            actor.gameObject.SetActive(false);actor.transform.SetParent(null);record.State=CargoState.Delivered;HeldId=null;motor.Speed=3.5f;LastMessage="交付完成："+record.Definition.Name;Revision++;return true;
        }
        public void ResetToInitial()
        {
            Ensure();HeldId=null;
            foreach(var pair in actors){var a=pair.Value;a.transform.SetParent(initialParents[pair.Key]);a.transform.SetPositionAndRotation(initialPositions[pair.Key],Quaternion.identity);a.gameObject.SetActive(true);a.Body.enabled=true;var record=registry.Get(pair.Key);record.State=CargoState.World;record.Position=initialPositions[pair.Key];}
            motor.Speed=3.5f;LastMessage="新回收任务已开始。";Revision++;
        }
        internal CargoActor Actor(string id){Ensure();return actors[id];}
        internal void RestoreRecords(SavedCargo[] saved)
        {
            ResetToInitial();foreach(var s in saved){var a=actors[s.Id];var record=registry.Get(s.Id);record.State=(CargoState)s.State;record.Position=s.Position();
                if(s.State==1){HeldId=s.Id;a.Body.enabled=false;a.transform.SetParent(anchor,false);a.transform.localPosition=Vector3.zero;a.transform.localRotation=Quaternion.identity;motor.Speed=3.5f-.1f*record.Definition.Kilograms;}
                else if(s.State==2){a.Body.enabled=false;a.gameObject.SetActive(false);}
                else a.transform.position=s.Position();}
            LastMessage="已恢复物品状态。";
        }
        bool Reject(string message){LastMessage=message;return false;}
    }
}
