using System;
using UnityEngine;

namespace EchoStation.World
{
    [DisallowMultipleComponent]
    public sealed class WorldTarget : MonoBehaviour
    {
        [SerializeField]private string uniqueId;
        [SerializeField]private string displayName;
        public string Id=>uniqueId;
        public string DisplayName=>displayName;
        public bool Registered{get;private set;}
        public static TargetRegistry Active{get;private set;}=new TargetRegistry();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()=>Active=new TargetRegistry();
        public void Configure(string id,string label)
        {
            if(isActiveAndEnabled)throw new InvalidOperationException("Identity must be configured while disabled.");
            if(!TargetRegistry.IsValidId(id))throw new ArgumentException("Invalid world target ID.",nameof(id));
            if(string.IsNullOrWhiteSpace(label))throw new ArgumentException("Display name is required.",nameof(label));
            uniqueId=id;displayName=label;
        }
        private void OnEnable()
        {
            Registered=Active.TryRegister(this,out var failure);
            if(!Registered)Debug.LogError("World target registration rejected: "+failure+" / "+uniqueId,this);
        }
        private void OnDisable(){Active.Unregister(this);Registered=false;}
        private void OnDestroy(){Active.Unregister(this);Registered=false;}
    }
}
