using UnityEngine;
namespace EchoStation.SalvageNext
{
    [RequireComponent(typeof(Camera)),DisallowMultipleComponent]
    public sealed class NextFogView:MonoBehaviour
    {
        [SerializeField]NextFogField source;
        public NextFogField Source{get=>source;set=>source=value;}
    }
}
