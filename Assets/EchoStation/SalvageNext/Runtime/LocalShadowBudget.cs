using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace EchoStation.SalvageNext
{
    public sealed class LocalShadowBudget:MonoBehaviour
    {
        [SerializeField]Camera view;
        [SerializeField]UniversalAdditionalLightData[] lights=Array.Empty<UniversalAdditionalLightData>();
        int[] original;float next;
        public void Configure(Camera camera,UniversalAdditionalLightData[] sources){Restore();view=camera;lights=(UniversalAdditionalLightData[])sources.Clone();Capture();}
        void Capture(){original=new int[lights.Length];for(int i=0;i<lights.Length;i++)if(lights[i])original[i]=lights[i].additionalLightsShadowResolutionTier;}
        void OnEnable(){Capture();next=0;}
        void Restore(){if(!Application.isPlaying||original==null)return;for(int i=0;i<lights.Length&&i<original.Length;i++)if(lights[i])lights[i].additionalLightsShadowResolutionTier=original[i];}
        public void Evaluate(Vector3 position)
        {
            if(!Application.isPlaying)return;foreach(var source in lights){if(!source)continue;float distance=Vector3.Distance(position,source.transform.position);int tier=source.additionalLightsShadowResolutionTier;if(tier==2&&distance>11)tier=1;else if(tier!=2&&distance<9)tier=2;if(tier==1&&distance>21)tier=0;else if(tier==0&&distance<19)tier=1;source.additionalLightsShadowResolutionTier=tier;}
        }
        void Update(){if(!view||Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;Evaluate(view.transform.position);}
        void OnDisable()=>Restore();
    }
}
