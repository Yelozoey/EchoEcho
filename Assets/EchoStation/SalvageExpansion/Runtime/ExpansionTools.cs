using System;
using EchoStation.Salvage;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace EchoStation.SalvageExpansion
{
    [DefaultExecutionOrder(40)]
    public sealed class ExpansionTools:MonoBehaviour
    {
        ExpansionWorld world;FacilityController facility;ExpansionPersistence persistence;FacilityDevice target;IDisposable lease;Text panel;RectTransform needle;bool stroke,recapture;float lastAngle;Quaternion initialRotation;Texture2D ringTexture;Sprite ringSprite;
        public bool Active=>target;
        public float Degrees{get;private set;}
        public Vector2 Center=>new Vector2(Screen.width*.5f,Screen.height*.5f);
        public void Initialize()
        {
            if(world)return;world=GetComponent<ExpansionWorld>();facility=GetComponent<FacilityController>();persistence=GetComponent<ExpansionPersistence>();if(!world||!facility||!persistence)throw new InvalidOperationException("Tool services required.");facility.Initialize();facility.ToolRequested=Request;persistence.Restored+=Cancel;
        }
        void Start()=>Initialize();
        public bool Request(FacilityDevice device)
        {
            if(!world||!device||!world.ActionsAllowed||world.Pause.Blocked||!world.Input.CanPlay)return false;
            if(device.Kind==FacilityDeviceKind.Isolator)return facility.TryIsolate(device);
            if(device.Kind>=FacilityDeviceKind.ClampA)return facility.TryReleaseClamp(device);
            if(device.Kind!=FacilityDeviceKind.Valve||facility.ValveClosed||world.Inventory.HeldId!=ExpansionCatalog.Wrench){world.Say("先装备维修扳手，再操作阀门。");return false;}
            if(!world.TryRay(out var hit)||hit.collider.GetComponentInParent<FacilityDevice>()!=device||Array.IndexOf(facility.Devices,device)<0)return false;
            target=device;initialRotation=target.transform.localRotation;Degrees=0;stroke=false;lease=world.Pause.Acquire(false);world.Input.SetPointerCaptured(false);
            if(!panel)
            {
                panel=SalvageUi.Panel(transform,"ExpansionValveOperation",720,560,125);panel.text="阀门操作 · 危险仍在运行\n\n\n\n\n\n\n左键抓住圆环，沿顺时针方向拖动90°\n松开可重新抓取 · Esc 取消";panel.alignment=TextAnchor.UpperCenter;
                var parent=(RectTransform)panel.transform.parent;var ring=SalvageWidgets.Rect("Ring",parent,260,180,200,200);var image=ring.gameObject.AddComponent<Image>();ringTexture=new Texture2D(128,128,TextureFormat.RGBA32,false);var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float radius=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(64,64));pixels[y*128+x]=radius>51&&radius<62?Color.white:Color.clear;}ringTexture.SetPixels(pixels);ringTexture.Apply();ringSprite=Sprite.Create(ringTexture,new Rect(0,0,128,128),new Vector2(.5f,.5f));image.sprite=ringSprite;image.color=new Color(.2f,.6f,.75f);needle=SalvageWidgets.Rect("Needle",parent,360,280,100,10);needle.pivot=new Vector2(0,.5f);needle.gameObject.AddComponent<Image>().color=new Color(.4f,.85f,1);
            }
            SalvageUi.CanvasOf(panel).SetActive(true);Refresh();return true;
        }
        bool Valid()=>target&&world.ActionsAllowed&&!world.Pause.Paused&&world.Input.HasApplicationFocus&&world.Inventory.HeldId==ExpansionCatalog.Wrench&&world.TryRay(out var hit)&&hit.collider.GetComponentInParent<FacilityDevice>()==target;
        bool Angle(Vector2 point,out float angle){var d=point-Center;angle=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;return d.magnitude>=55&&d.magnitude<=250;}
        public bool BeginStroke(Vector2 point){if(!Valid()||!Angle(point,out lastAngle))return false;stroke=true;return true;}
        public bool DragStroke(Vector2 point)
        {
            if(!stroke||!Valid()){CancelStroke();return false;}if(!Angle(point,out float angle)){CancelStroke();return false;}float delta=Mathf.DeltaAngle(lastAngle,angle);lastAngle=angle;if(Mathf.Abs(delta)>45.1f)return false;Degrees=Mathf.Clamp(Degrees-delta,0,90);Refresh();
            if(Degrees>=89.9f){var finished=target;bool result=facility.TryCloseValve(finished);Cancel();return result;}return true;
        }
        public void CancelStroke()=>stroke=false;
        void Refresh(){if(needle)needle.localRotation=Quaternion.Euler(0,0,-Degrees);if(target)target.transform.localRotation=initialRotation*Quaternion.Euler(0,0,-Degrees);}
        public void Cancel(){if(target)target.transform.localRotation=initialRotation;target=null;stroke=false;Degrees=0;if(panel)SalvageUi.CanvasOf(panel).SetActive(false);lease?.Dispose();lease=null;recapture=true;}
        void Update()
        {
            if(!Active)return;if(!Valid()){Cancel();return;}if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){Cancel();return;}var mouse=Mouse.current;if(mouse==null)return;if(mouse.leftButton.wasPressedThisFrame)BeginStroke(mouse.position.ReadValue());if(mouse.leftButton.isPressed)DragStroke(mouse.position.ReadValue());if(mouse.leftButton.wasReleasedThisFrame)CancelStroke();
        }
        void LateUpdate(){if(recapture){recapture=false;if(world)world.Pause.RestorePointer();}}
        void OnDisable(){Cancel();}
        void OnDestroy(){if(persistence)persistence.Restored-=Cancel;if(ringSprite)Destroy(ringSprite);if(ringTexture)Destroy(ringTexture);}
    }
}
