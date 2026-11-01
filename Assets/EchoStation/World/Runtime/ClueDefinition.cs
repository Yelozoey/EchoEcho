using System;
using UnityEngine;

namespace EchoStation.World
{
    public enum ClueKind{Note,Data,Scan}
    public sealed class ClueRecord
    {
        public string Id{get;}public string Title{get;}public string FullText{get;}public string Summary{get;}public ClueKind Kind{get;}
        internal ClueRecord(string id,string title,string full,string summary,ClueKind kind){Id=id;Title=title;FullText=full;Summary=summary;Kind=kind;}
    }
    [CreateAssetMenu(menuName="EchoStation/Clue Definition")]
    public sealed class ClueDefinition : ScriptableObject
    {
        [SerializeField]private string uniqueId,title;
        [SerializeField,TextArea]private string fullText;
        [SerializeField,TextArea]private string summary;
        [SerializeField]private ClueKind kind;
        public string Id=>uniqueId;public string Title=>title;public ClueKind Kind=>kind;
        public void Configure(string id,string label,string full,string brief,ClueKind category)
        {
            Validate(id,label,full,brief,category);uniqueId=id;title=label;fullText=full;summary=brief;kind=category;
        }
        private static void Validate(string id,string label,string full,string brief,ClueKind category)
        {
            if(!TargetRegistry.IsValidId(id)||string.IsNullOrWhiteSpace(label)||string.IsNullOrWhiteSpace(full)||full.Length>4000||
                string.IsNullOrWhiteSpace(brief)||brief.Length>120||brief==full||!Enum.IsDefined(typeof(ClueKind),category))throw new ArgumentException("Invalid clue definition or non-distinct summary.");
        }
        internal ClueRecord ReadRecord(){Validate(uniqueId,title,fullText,summary,kind);return new ClueRecord(uniqueId,title,fullText,summary,kind);}
    }
}
