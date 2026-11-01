using UnityEngine;
namespace EchoStation.InfiltrationLevel
{
    [RequireComponent(typeof(Camera)),DisallowMultipleComponent]
    public sealed class LevelFogView:MonoBehaviour
    {
        [SerializeField]LevelFogField source;
        public LevelFogField Source{get=>source;set=>source=value;}
    }
}
