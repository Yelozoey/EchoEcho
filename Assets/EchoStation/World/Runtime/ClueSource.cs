using System;
using UnityEngine;

namespace EchoStation.World
{
    [RequireComponent(typeof(WorldTarget)),DisallowMultipleComponent]
    public sealed class ClueSource : MonoBehaviour
    {
        [SerializeField]private ClueDefinition definition;
        [SerializeField]private Renderer presentation;
        [SerializeField]private bool revealed;
        public string ClueId=>definition?definition.Id:null;
        public ClueKind Kind=>definition?definition.Kind:ClueKind.Note;
        public bool IsReadable=>definition&&(definition.Kind!=ClueKind.Scan||revealed);
        public bool IsRevealed=>revealed;
        public void Configure(ClueDefinition data,Renderer visual)
        {
            if(isActiveAndEnabled)throw new InvalidOperationException("Configure disabled clue only.");
            if(!data||GetComponent<WorldTarget>().Id!=data.Id)throw new ArgumentException("Clue data must match world target identity.");
            data.ReadRecord();definition=data;presentation=visual;revealed=false;RefreshVisual();
        }
        public bool TryRead(out ClueRecord record)
        {
            record=null;if(!IsReadable)return false;record=definition.ReadRecord();return true;
        }
        private void OnEnable()=>RefreshVisual();
        private void RefreshVisual(){if(presentation)presentation.enabled=IsReadable;}
        internal ClueRecord ReadCanonicalForPersistence()=>definition?definition.ReadRecord():null;
        internal void ResetForPersistence(){revealed=false;RefreshVisual();}
        // Only the traditional Echo execution assembly can commit a verified scan.
        internal bool TryRevealFromEcho(out ClueRecord record)
        {
            record=null;if(!definition)return false;var data=definition.ReadRecord();
            if(data.Id!=GetComponent<WorldTarget>().Id)return false;
            if(definition.Kind==ClueKind.Scan&&!revealed){revealed=true;RefreshVisual();}
            record=data;return true;
        }
    }
}
