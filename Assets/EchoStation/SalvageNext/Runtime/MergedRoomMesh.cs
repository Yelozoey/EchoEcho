using UnityEngine;
namespace EchoStation.SalvageNext
{
    [ExecuteAlways] public sealed class MergedRoomMesh:MonoBehaviour
    {
        [SerializeField]MeshRenderer target;
        [SerializeField]int atlas;
        public int Atlas=>atlas;
        [System.NonSerialized]bool externalManaged;
        public void SetExternalManaged(bool value){externalManaged=value;}
        public void Configure(MeshRenderer renderer,int index){target=renderer;atlas=index;Apply();}
        public void Apply(){if(target){target.lightmapIndex=atlas;target.lightmapScaleOffset=new Vector4(1,1,0,0);}}
        void OnEnable(){if(!externalManaged)Apply();}
        void Start(){if(!externalManaged)Apply();}
    }
}
