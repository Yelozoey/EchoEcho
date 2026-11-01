using UnityEngine;
namespace EchoStation.Rendering
{
 [RequireComponent(typeof(Camera)),DisallowMultipleComponent]
 public sealed class LocalFogView:MonoBehaviour { [SerializeField]private LocalFogField source;public LocalFogField Source{get=>source;set=>source=value;} }
}
