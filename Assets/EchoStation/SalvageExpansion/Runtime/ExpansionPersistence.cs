using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace EchoStation.SalvageExpansion
{
    [DefaultExecutionOrder(100)]
    public sealed class ExpansionPersistence:MonoBehaviour
    {
        readonly Dictionary<string,IExpansionSaveSection> sections=new Dictionary<string,IExpansionSaveSection>(StringComparer.Ordinal);
        ExpansionWorld world;
        public ExpansionSaveStore Store{get;private set;}
        public long Epoch{get;private set;}
        public event Action Restored;
        void Cache(){if(!world)world=GetComponent<ExpansionWorld>();if(!world||world.Inventory==null)throw new InvalidOperationException("Initialized expansion world required.");if(Store==null)Store=new ExpansionSaveStore(Path.Combine(Application.persistentDataPath,"SalvageExpansion"));}
        public void ConfigureTests(string directory){var root=Path.GetFullPath("Library/EchoStation/SalvageExpansion/Tests")+Path.DirectorySeparatorChar;var target=Path.GetFullPath(directory);if(!target.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Test slot outside owned test directory.");Store=new ExpansionSaveStore(target);}
        public void Register(IExpansionSaveSection section)
        {if(section==null||!InventoryItemDefinition.ValidId(section.Key)||sections.ContainsKey(section.Key))throw new ArgumentException("Unique valid save section required.");sections.Add(section.Key,section);}
        public ExpansionSnapshot Capture()
        {
            Cache();var values=new JObject();foreach(var section in sections.OrderBy(p=>p.Key,StringComparer.Ordinal))values[section.Key]=section.Value.Capture()?.DeepClone()??throw new InvalidOperationException("Save section returned null.");
            return new ExpansionSnapshot(world.Inventory.Records,world.WorldPositions,transform.position,Mathf.Repeat(transform.eulerAngles.y,360),Mathf.DeltaAngle(0,world.Motor.View.localEulerAngles.x),values);
        }
        bool Prepare(ExpansionSnapshot snapshot,out InventoryStore inventory,out List<Action> apply,out List<Action> rollback,out string error)
        {
            Cache();inventory=null;apply=new List<Action>();rollback=new List<Action>();error="";
            if(snapshot==null)return Fail("空存档。",out error);
            var values=snapshot.Sections;if(values.Count!=sections.Count||values.Properties().Any(p=>!sections.ContainsKey(p.Name)))return Fail("存档模块不匹配。",out error);
            if(world.Actors.Values.Any(v=>!v)||!GetComponent<CharacterController>())return Fail("场景绑定不完整。",out error);
            var position=snapshot.Player;var controller=GetComponent<CharacterController>();
            float radius=controller.radius*.9f;var lower=position+Vector3.up*(controller.center.y-controller.height*.5f+controller.radius);var upper=position+Vector3.up*(controller.center.y+controller.height*.5f-controller.radius);
            if(Physics.OverlapCapsule(lower,upper,radius,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(transform)&&!c.GetComponentInParent<ExpansionItemActor>()))return Fail("存档玩家位置与场景障碍物重叠。",out error);
            var worldBoxes=new List<Bounds>();
            foreach(var row in snapshot.Inventory.Where(r=>r.Location==InventoryItemLocation.World))
            {
                var actor=world.Actors[row.Id];var size=Vector3.Scale(actor.Body.size,actor.transform.lossyScale);var center=snapshot.Positions[row.Id]+Vector3.Scale(actor.Body.center,actor.transform.lossyScale);
                if(Physics.OverlapBox(center,size*.45f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(transform)&&!c.GetComponentInParent<ExpansionItemActor>()))return Fail("存档物品位置与场景障碍物重叠。",out error);
                var box=new Bounds(center,size*.9f);if(worldBoxes.Any(b=>b.Intersects(box)))return Fail("存档世界物品相互重叠。",out error);worldBoxes.Add(box);
                if(box.Intersects(new Bounds(position+controller.center,new Vector3(controller.radius*2,controller.height,controller.radius*2))))return Fail("存档物品与玩家重叠。",out error);
                var support=Physics.RaycastAll(center-Vector3.up*(size.y*.5f-.1f),Vector3.down,.4f,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.transform.IsChildOf(transform)&&!h.collider.GetComponentInParent<ExpansionItemActor>()).OrderBy(h=>h.distance).FirstOrDefault();
                if(!support.collider||!support.collider.GetComponentInParent<EchoStation.Salvage.SalvageDropSurface>())return Fail("存档物品缺少安全支撑面。",out error);
            }
            foreach(var section in sections.OrderBy(p=>p.Key,StringComparer.Ordinal))
            {
                if(!section.Value.TryPrepare(snapshot,values[section.Key].DeepClone(),out var commit,out var undo,out error)||commit==null||undo==null)return false;apply.Add(commit);rollback.Add(undo);
            }
            inventory=new InventoryStore(ExpansionCatalog.Items);return inventory.TryRestore(snapshot.Inventory,out error);
        }
        public bool TryRestore(ExpansionSnapshot snapshot,out string error)
        {
            if(!Prepare(snapshot,out var inventory,out var apply,out var rollback,out error))return false;
            var before=Capture();var previous=world.Inventory;int attempted=0;
            try
            {
                for(int i=0;i<apply.Count;i++){attempted=i+1;apply[i]();}
                world.ApplyInventorySnapshot(inventory,snapshot.Positions,snapshot.Player,snapshot.Yaw,snapshot.Pitch);Epoch=checked(Epoch+1);Restored?.Invoke();error="";return true;
            }
            catch(Exception e)
            {
                for(int i=attempted-1;i>=0;i--)try{rollback[i]();}catch(Exception undo){error="回滚异常："+undo.Message;return false;}
                if(!ReferenceEquals(world.Inventory,previous))world.ApplyInventorySnapshot(previous,before.Positions,before.Player,before.Yaw,before.Pitch);
                error="读档未提交："+e.Message;return false;
            }
        }
        public bool Save()
        {
            Cache();if(!world.ActionsAllowed||world.Pause.Blocked){world.Say("当前不能保存，请关闭面板并确认任务未失败。");return false;}
            try{var snapshot=Capture();if(!Prepare(snapshot,out _,out _,out _,out var validation)){world.Say(validation);return false;}bool result=Store.TryWrite(snapshot,out var error);world.Say(result?"环境回收进度已保存。":error);return result;}
            catch(Exception e)when(e is ArgumentException||e is InvalidOperationException){world.Say(e.Message);return false;}
        }
        public bool Load()
        {
            Cache();if(!Store.TryRead(out var snapshot,out var message,s=>Prepare(s,out _,out _,out _,out var error)?"":error)){world.Say(message);return false;}
            if(!TryRestore(snapshot,out var error)){world.Say(error);return false;}world.Say(message==""?"已恢复环境回收进度。":message);return true;
        }
        void Update(){if(!world)world=GetComponent<ExpansionWorld>();if(!world||world.Inventory==null||!world.Input.HasApplicationFocus||Keyboard.current==null)return;if(Keyboard.current.f9Key.wasPressedThisFrame&&(!world.Pause.Blocked||!world.ActionsAllowed))Load();else if(Keyboard.current.f5Key.wasPressedThisFrame&&!world.Pause.Blocked)Save();}
        static bool Fail(string message,out string error){error=message;return false;}
    }
}
