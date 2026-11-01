using EchoStation.Echo;
using EchoStation.Player;
using UnityEngine;

namespace EchoStation.Salvage
{
    public sealed class SalvageEcho:MonoBehaviour
    {
        [SerializeField]Transform player;
        EchoNavigation navigation;EchoPresentation presentation;PlayerInputReader input;SalvageMotor motor;float repath,yieldRemaining;Vector3 previousPlayer;
        public EchoBattery Battery{get;}=new EchoBattery();
        public EchoNavigation Navigation=>navigation?navigation:navigation=GetComponent<EchoNavigation>();
        public bool Initialized{get;private set;}
        public bool ExternalControl{get;set;}
        public bool Docked=>Vector3.Distance(transform.position,SalvageDelivery.Dock)<2.5f;
        public void Configure(Transform value)=>player=value;
        public bool Initialize()
        {if(!player)return false;input=player.GetComponent<PlayerInputReader>();motor=player.GetComponent<SalvageMotor>();presentation=GetComponent<EchoPresentation>();previousPlayer=player.position;yieldRemaining=0;Initialized=Navigation.TryBind(transform.position);return Initialized;}
        void Start()=>Initialize();
        void Update(){if(Initialized&&!ExternalControl)Advance(Mathf.Min(Time.deltaTime,.1f),input.UiBlocked||!Application.isFocused||Time.timeScale==0);}
        public void Advance(float seconds,bool paused)
        {
            if(!Initialized)return;AdvanceResources(seconds,paused);StepFollow(seconds,paused,true);
        }
        public void AdvanceResources(float seconds,bool paused){if(!Initialized)return;Battery.Step(seconds,Navigation.IsBound&&Navigation.Agent.velocity.sqrMagnitude>.01f,Docked,paused);Navigation.Agent.speed=Battery.FollowSpeed;}
        public void AdvanceFollow(float seconds)=>StepFollow(seconds,false,false);
        void StepFollow(float seconds,bool paused,bool render)
        {
            var movement=player.position-previousPlayer;movement.y=0;previousPlayer=player.position;
            if(paused){Navigation.Cancel();yieldRemaining=0;return;}
            repath-=seconds;var delta=player.position-transform.position;delta.y=0;yieldRemaining-=seconds;
            if(yieldRemaining>0&&Navigation.State==NavigationState.Moving){if(render)Present(seconds);return;}
            // Staying still inside the follow radius must not block an approaching player.
            var wanted=motor?motor.DesiredHorizontalVelocity:movement;
            if(delta.magnitude<1.6f&&wanted.sqrMagnitude>.000001f&&Vector3.Dot(wanted.normalized,-delta.normalized)>.2f)
            {
                var side=Vector3.Cross(Vector3.up,wanted.normalized)*1.6f;
                if(TryYield(transform.position+side)||TryYield(transform.position-side)||TryYield(transform.position+wanted.normalized*1.6f)){yieldRemaining=1.4f;if(render)Present(seconds);return;}
            }
            if(delta.magnitude<1.9f)Navigation.Cancel();else if(repath<=0){Navigation.TryMove(player.position-delta.normalized*1.7f);repath=.3f;}
            if(render)Present(seconds);
        }
        bool TryYield(Vector3 goal)
        {
            if(!Navigation.TryMove(goal))return false;
            if(Navigation.State==NavigationState.Moving&&Vector3.Distance(Navigation.Goal,transform.position)>.65f&&Vector3.Distance(Navigation.Goal,player.position)>1.15f)return true;
            Navigation.Cancel();return false;
        }
        void Present(float seconds)
        {
            if(presentation)presentation.ApplyFrame(Vector3.Dot(Navigation.Agent.velocity,transform.forward),Battery.Charge<25?EchoMood.Alert:EchoMood.Calm,seconds,Time.unscaledTime,false);
        }
        void OnDisable(){if(navigation)navigation.Cancel();Initialized=false;}
    }
}
