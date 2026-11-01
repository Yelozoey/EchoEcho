using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoStation.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader:MonoBehaviour
    {
        private InputActionMap map;private InputAction move,look,release,capture,jump,torch,interact,journal;
        private bool applicationFocused,captured,uiBlocked,cursorSaved,subscribed,ignoreLook;private CursorLockMode oldLock;private bool oldVisible;
        public bool CanPlay=>isActiveAndEnabled&&applicationFocused&&captured&&!uiBlocked;
        public bool UiBlocked=>uiBlocked;
        public bool PointerCaptured=>captured;
        public bool HasApplicationFocus=>applicationFocused;
        public Vector2 Move{get{Ensure();return CanPlay?Vector2.ClampMagnitude(move.ReadValue<Vector2>(),1):Vector2.zero;}}
        public Vector2 Look{get{Ensure();return CanPlay&&!ignoreLook?look.ReadValue<Vector2>():Vector2.zero;}}
        public bool JumpPressed{get{Ensure();return CanPlay&&jump.WasPressedThisFrame();}}
        public bool TorchPressed{get{Ensure();return CanPlay&&torch.WasPressedThisFrame();}}
        public bool InteractPressed{get{Ensure();return CanPlay&&interact.WasPressedThisFrame();}}
        public bool JournalPressed{get{Ensure();return isActiveAndEnabled&&applicationFocused&&journal.WasPressedThisFrame();}}
        public bool CancelPressed{get{Ensure();return isActiveAndEnabled&&applicationFocused&&release.WasPressedThisFrame();}}
        private void Ensure()
        {
            if(!cursorSaved&&isActiveAndEnabled){oldLock=Cursor.lockState;oldVisible=Cursor.visible;cursorSaved=true;}
            if(!subscribed&&isActiveAndEnabled){InputSystem.onAfterUpdate+=AfterInputUpdate;subscribed=true;}
            if(map!=null)return;
            map=new InputActionMap("Exploration");
            move=map.AddAction("Move",InputActionType.Value,expectedControlLayout:"Vector2");
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            look=map.AddAction("Look",InputActionType.PassThrough,"<Mouse>/delta",expectedControlLayout:"Vector2");
            release=map.AddAction("ReleasePointer",InputActionType.Button,"<Keyboard>/escape");
            capture=map.AddAction("CapturePointer",InputActionType.Button,"<Mouse>/leftButton");
            jump=map.AddAction("Jump",InputActionType.Button,"<Keyboard>/space");
            torch=map.AddAction("Torch",InputActionType.Button,"<Keyboard>/f");
            interact=map.AddAction("Interact",InputActionType.Button,"<Keyboard>/e");
            journal=map.AddAction("Journal",InputActionType.Button,"<Keyboard>/j");
            if(isActiveAndEnabled)map.Enable();
        }
        private void Awake(){Ensure();applicationFocused=Application.isFocused;}
        private void OnEnable(){Ensure();map.Enable();}
        private void Start(){if(applicationFocused)SetPointerCaptured(true);}
        private void Update(){if(release.WasPressedThisFrame())SetPointerCaptured(false);else if(capture.WasPressedThisFrame()&&applicationFocused&&!uiBlocked)SetPointerCaptured(true);}
        public void SetApplicationFocus(bool focused){Ensure();applicationFocused=focused;if(!focused)SetPointerCaptured(false);}
        private void OnApplicationFocus(bool focused)=>SetApplicationFocus(focused);
        public void SetUiBlocked(bool blocked){Ensure();uiBlocked=blocked;if(blocked)SetPointerCaptured(false);}
        public void SetPointerCaptured(bool value)
        {
            Ensure();captured=value&&applicationFocused&&!uiBlocked&&isActiveAndEnabled;ignoreLook=true;if(!isActiveAndEnabled)return;Cursor.lockState=captured?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!captured;
        }
        private void AfterInputUpdate()=>ignoreLook=false;
        private void Restore(){captured=false;if(subscribed){InputSystem.onAfterUpdate-=AfterInputUpdate;subscribed=false;}if(cursorSaved){Cursor.lockState=oldLock;Cursor.visible=oldVisible;cursorSaved=false;}}
        private void OnDisable(){map?.Disable();Restore();}
        private void OnDestroy(){Restore();map?.Dispose();map=null;}
    }
}
