using System;
using UnityEngine;
using UnityEngine.AI;
namespace EchoStation.Salvage
{
    [RequireComponent(typeof(CargoActor),typeof(NavMeshObstacle))]
    public sealed class SalvageCargoObstacle:MonoBehaviour
    {
        [SerializeField]SalvageCarry owner;CargoActor actor;NavMeshObstacle obstacle;
        public void Configure(SalvageCarry value){if(!value||value.gameObject.scene!=gameObject.scene)throw new ArgumentException("Owned carry required.");owner=value;}
        void LateUpdate()=>Refresh();
        public void Refresh(){if(!owner)return;if(!actor)actor=GetComponent<CargoActor>();if(!obstacle)obstacle=GetComponent<NavMeshObstacle>();bool world=owner.Registry.Get(actor.Id).State==CargoState.World&&actor.Body.enabled;obstacle.enabled=world;}
        void OnDisable(){if(obstacle)obstacle.enabled=false;}
    }
}
