using UnityEngine;
using UnityEngine.AI;
namespace EchoStation.Station
{
    [RequireComponent(typeof(BoxCollider),typeof(NavMeshObstacle)),DisallowMultipleComponent]
    public sealed class StationGate:MonoBehaviour
    {
        [SerializeField]private bool open;
        public bool IsOpen=>open;
        private void Awake()=>Apply(open);
        internal void Apply(bool value){open=value;GetComponent<BoxCollider>().enabled=!open;GetComponent<NavMeshObstacle>().enabled=!open;var r=GetComponent<Renderer>();if(r)r.enabled=!open;}
    }
}
