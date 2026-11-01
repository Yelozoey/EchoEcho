using System.Linq;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EchoStation.Salvage
{
    [RequireComponent(typeof(SalvageCarry))]
    public sealed class SalvageDelivery:MonoBehaviour
    {
        public static readonly Vector3 Dock=new Vector3(5,0,12);
        SalvageCarry carry;PlayerInputReader input;string pendingId;Text panel;
        public bool Pending=>pendingId!=null;
        public bool AtDock=>Vector3.Distance(transform.position,Dock)<=2;
        public int Count=>Carry.Registry.Records.Count(r=>r.State==CargoState.Delivered);
        public int Points=>Carry.Registry.Records.Where(r=>r.State==CargoState.Delivered).Sum(r=>r.Definition.Points);
        SalvageCarry Carry=>carry?carry:carry=GetComponent<SalvageCarry>();
        PlayerInputReader Input=>input?input:input=GetComponent<PlayerInputReader>();
        void Update(){if(!Pending||Keyboard.current==null)return;if(Keyboard.current.escapeKey.wasPressedThisFrame)Cancel();else if(Keyboard.current.enterKey.wasPressedThisFrame)Confirm();}
        public bool Request()
        {
            if(Pending||Input.UiBlocked||!AtDock||Carry.HeldId==null)return false;
            pendingId=Carry.HeldId;Input.SetUiBlocked(true);var d=SalvageCatalog.Get(pendingId);
            if(!panel)panel=SalvageUi.Panel(transform,"DeliveryConfirmation",720,260,50);
            panel.text="确认交付："+d.Name+"\n"+d.Kilograms+"kg / "+d.Points+"分\n\nEnter 确认    Esc 取消";SalvageUi.CanvasOf(panel).SetActive(true);return true;
        }
        public bool Confirm()
        {
            if(!Pending)return false;var id=pendingId;bool valid=AtDock&&Carry.HeldId==id&&Carry.Deliver(id);Close();return valid;
        }
        public void Cancel(){if(Pending)Close();}
        void Close(){pendingId=null;if(panel)SalvageUi.CanvasOf(panel).SetActive(false);Input.SetUiBlocked(false);Input.SetPointerCaptured(true);}
        void OnDisable()=>Cancel();
    }
}
