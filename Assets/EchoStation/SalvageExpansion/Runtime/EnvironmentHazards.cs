using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using EchoStation.Salvage;
namespace EchoStation.SalvageExpansion
{
    [DefaultExecutionOrder(30)]
    public sealed class EnvironmentHazards:MonoBehaviour,IExpansionSaveSection
    {
        [SerializeField]HazardZone[] zones;ExpansionWorld world;FacilityController facility;bool initialized,wasDanger,restartPending;IDisposable failureLease;Text failure;
        public float Pressure{get;private set;}
        public float Phase{get;private set;}
        public float SafeSeconds{get;private set;}
        public bool Failed{get;private set;}
        public int Incidents{get;private set;}
        public string Key=>"hazards";
        public Action RestartRequested{get;set;}
        public HazardZone[] Zones=>zones?.ToArray()??Array.Empty<HazardZone>();
        public void Configure(HazardZone[] owned){if(initialized)throw new InvalidOperationException("Hazards already initialized.");zones=owned;}
        void Start()=>Initialize();
        public void Initialize()
        {
            if(initialized)return;world=GetComponent<ExpansionWorld>();facility=GetComponent<FacilityController>();if(!world||!facility)throw new InvalidOperationException("World/facility required.");facility.Initialize();
            if(zones==null||zones.Length!=2||zones.Any(z=>!z||z.gameObject.scene!=gameObject.scene)||zones.Select(z=>z.Kind).Distinct().Count()!=2)throw new InvalidOperationException("Two unique owned hazard zones required.");
            GetComponent<ExpansionPersistence>().Register(this);world.SafeDrop=point=>!zones.Any(z=>z.Contains(point)&&Signal(z)==HazardSignal.Active);initialized=true;Present();
        }
        public HazardSignal Signal(HazardZone zone)
        {
            if(zone.Kind==EnvironmentHazardKind.Electrical)return facility.Isolated?HazardSignal.Safe:HazardSignal.Active;
            if(facility.SteamSuppressed)return HazardSignal.Safe;
            return Phase<2?HazardSignal.Warning:Phase<4?HazardSignal.Active:HazardSignal.Safe;
        }
        public float RateAt(Vector3 position)
        {
            float rate=0;foreach(var zone in zones)if(zone.Contains(position)&&Signal(zone)==HazardSignal.Active)rate+=zone.Kind==EnvironmentHazardKind.Steam?32:55;return rate;
        }
        public void Advance(float seconds,bool paused)
        {
            if(!float.IsFinite(seconds)||seconds<0||seconds>1)throw new ArgumentOutOfRangeException(nameof(seconds));if(paused||Failed||!world.ActionsAllowed)return;
            Phase=Mathf.Repeat(Phase+seconds,8);float rate=RateAt(world.transform.position);
            if(rate>0){if(!wasDanger)Incidents++;SafeSeconds=0;Pressure=Mathf.Min(100,Pressure+rate*seconds);}
            else{float recovering=Mathf.Max(0,seconds-Mathf.Max(0,1-SafeSeconds));SafeSeconds=Mathf.Min(1,SafeSeconds+seconds);Pressure=Mathf.Max(0,Pressure-12*recovering);}
            wasDanger=rate>0;if(Pressure>=100)SetFailed(true);Present();
        }
        void SetFailed(bool value)
        {
            Failed=value;restartPending=false;world.ActionsAllowed=!value;
            if(value)
            {
                world.CancelDelivery();world.Backpack.Close();if(failureLease==null)failureLease=world.Pause.Acquire();
                if(!failure)failure=SalvageUi.Panel(transform,"ExpansionFailure",920,410,150);
                SalvageUi.CanvasOf(failure).SetActive(true);RefreshFailure();
            }
            else{if(failure)SalvageUi.CanvasOf(failure).SetActive(false);failureLease?.Dispose();failureLease=null;}
        }
        void RefreshFailure(){if(failure)failure.text="本局失败 · 防护压力达到上限\n\n尚未交付的物资没有获得收益。\nF9 从最后存档恢复\n"+(restartPending?"确认重开本局？Enter 确认 / Esc 取消":"N 重新开始本局");}
        void Present(){if(zones!=null)foreach(var zone in zones)if(zone)zone.Present(Signal(zone));}
        void Update()
        {
            if(!initialized)return;if(Failed)
            {
                if(!world.Input.HasApplicationFocus||Keyboard.current==null)return;
                if(Keyboard.current.nKey.wasPressedThisFrame){restartPending=true;RefreshFailure();}
                else if(Keyboard.current.escapeKey.wasPressedThisFrame){restartPending=false;RefreshFailure();}
                else if(restartPending&&Keyboard.current.enterKey.wasPressedThisFrame){restartPending=false;RestartRequested?.Invoke();RefreshFailure();}
            }
            else Advance(Mathf.Min(Time.deltaTime,.1f),world.Pause.Paused||!world.Input.HasApplicationFocus);
        }
        public JToken Capture()=>new JObject{{"pressure",Pressure},{"phase",Phase},{"safe",SafeSeconds},{"failed",Failed},{"incidents",Incidents},{"wasDanger",wasDanger}};
        void Apply(JObject value)
        {Pressure=(float)value["pressure"];Phase=(float)value["phase"];SafeSeconds=(float)value["safe"];Incidents=(int)value["incidents"];wasDanger=(bool)value["wasDanger"];SetFailed((bool)value["failed"]);Present();}
        public bool TryPrepare(ExpansionSnapshot snapshot,JToken value,out Action apply,out Action rollback,out string error)
        {
            apply=rollback=null;error="";try
            {
                var v=SaveSectionJson.Fields(value,"pressure","phase","safe","failed","incidents","wasDanger");float pressure=SaveSectionJson.Number(v["pressure"],0,100),phase=SaveSectionJson.Number(v["phase"],0,8);SaveSectionJson.Number(v["safe"],0,1);bool failed=SaveSectionJson.Boolean(v["failed"]);SaveSectionJson.Integer(v["incidents"],0,1000000);SaveSectionJson.Boolean(v["wasDanger"]);
                if(phase>=8||failed!=(pressure>=100))throw new ArgumentException("Failure/pressure state inconsistent.");
                var before=(JObject)Capture();bool pending=restartPending;var frozen=(JObject)v.DeepClone();apply=()=>Apply(frozen);rollback=()=>{Apply(before);restartPending=pending;RefreshFailure();};return true;
            }
            catch(Exception e)when(e is ArgumentException||e is OverflowException){error=e.Message;return false;}
        }
        void OnDisable(){failureLease?.Dispose();failureLease=null;if(failure)SalvageUi.CanvasOf(failure).SetActive(false);}
    }
}
