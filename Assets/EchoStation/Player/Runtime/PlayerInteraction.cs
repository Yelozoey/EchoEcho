using System;
using EchoStation.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Player
{
    [RequireComponent(typeof(PlayerInputReader)),DisallowMultipleComponent]
    public sealed class PlayerInteraction:MonoBehaviour
    {
        [SerializeField]private Camera view;
        private PlayerInputReader input;private InteractionRaycaster raycaster;private InteractionSelection selection;private WorldTarget promptTarget;
        public InteractionSelection Selection=>selection;
        public string Prompt{get;private set;}="";
        public string LastMessage{get;private set;}="";
        public event Action<ClueRecord> ClueRead;
        public event Action<string> InformationRead;
        public void Configure(Camera camera){if(!camera||!camera.transform.IsChildOf(transform))throw new ArgumentException("Interaction camera must belong to this player.");view=camera;raycaster=null;}
        private void Cache()
        {
            if(!input)input=GetComponent<PlayerInputReader>();if(!view){var motor=GetComponent<FirstPersonMotor>();if(motor&&motor.View)view=motor.View.GetComponent<Camera>();}if(!view)throw new InvalidOperationException("Interaction view missing.");if(raycaster==null)raycaster=new InteractionRaycaster(gameObject.scene.GetPhysicsScene(),WorldTarget.Active,transform);
        }
        private void Awake()=>Cache();
        private void LateUpdate(){RefreshSelection();if(input.InteractPressed)TryInteract();}
        public void RefreshSelection()
        {
            Cache();var next=default(InteractionSelection);if(isActiveAndEnabled&&input.CanPlay)raycaster.TrySelect(new Ray(view.transform.position,view.transform.forward),out next);selection=next;
            if(!ReferenceEquals(promptTarget,next.Target)){promptTarget=next.Target;Prompt=promptTarget?(next.Clue?"[E] 读取：":"[E] 查看：")+promptTarget.DisplayName:"";}
        }
        public bool TryInteract()
        {
            Cache();if(!isActiveAndEnabled||!input.CanPlay)return false;Physics.SyncTransforms();RefreshSelection();if(!selection.IsValid)return false;
            if(selection.Clue)
            {
                if(!selection.Clue.TryRead(out var record))return false;LastMessage=record.Title;ClueRead?.Invoke(record);return true;
            }
            LastMessage="该设备需要进一步检查。Echo可以提供扫描信息。";InformationRead?.Invoke(LastMessage);return true;
        }
        private void OnDisable(){selection=default;promptTarget=null;Prompt="";}
    }
}
