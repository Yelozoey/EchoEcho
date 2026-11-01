using System;
using UnityEngine;
namespace EchoStation.Salvage
{
    public sealed class EchoBattery
    {
        public float Charge{get;private set;}=100;
        public int LowWarningCount{get;private set;}
        public bool CanScan=>Charge>=8;
        public float FollowSpeed=>Charge<=0?1.5f:2.2f;
        public void Step(float seconds,bool moving,bool docked,bool paused)
        {
            if(!float.IsFinite(seconds)||seconds<0)throw new ArgumentException("Invalid battery time.");if(paused)return;
            Charge=Mathf.Clamp(Charge+seconds*(docked?20:-(.05f+(moving?.15f:0))),0,100);CheckWarning();
        }
        // R2 must call this only after a trusted successful scan; R1 exposes no scan command.
        public bool SpendSuccessfulScan(){if(!CanScan)return false;Charge-=8;CheckWarning();return true;}
        void CheckWarning(){if(Charge<25&&LowWarningCount==0)LowWarningCount=1;}
        public void Restore(float charge,bool warned)
        {if(!float.IsFinite(charge)||charge<0||charge>100||charge<25&&!warned)throw new ArgumentException("Invalid battery snapshot.");Charge=charge;LowWarningCount=warned?1:0;}
    }
}
