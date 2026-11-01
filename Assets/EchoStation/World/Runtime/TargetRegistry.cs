using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EchoStation.World
{
    public enum RegistrationFailure{None,MissingTarget,InvalidId,DuplicateId}
    public sealed class TargetRegistry
    {
        private static readonly Regex Pattern=new Regex("\\A[a-z][a-z0-9_]{0,63}\\z",RegexOptions.CultureInvariant);
        private readonly Dictionary<string,WorldTarget> targets=new Dictionary<string,WorldTarget>(System.StringComparer.Ordinal);
        public static bool IsValidId(string id)=>id!=null&&Pattern.IsMatch(id);
        public bool TryRegister(WorldTarget target,out RegistrationFailure failure)
        {
            failure=RegistrationFailure.None;
            if(!target){failure=RegistrationFailure.MissingTarget;return false;}
            if(!IsValidId(target.Id)){failure=RegistrationFailure.InvalidId;return false;}
            if(TryGet(target.Id,out var previous)&&!ReferenceEquals(previous,target)){failure=RegistrationFailure.DuplicateId;return false;}
            targets[target.Id]=target;return true;
        }
        public bool TryGet(string id,out WorldTarget target)
        {
            target=null;if(!IsValidId(id)||!targets.TryGetValue(id,out var found))return false;
            if(!found){targets.Remove(id);return false;}target=found;return true;
        }
        public bool Unregister(WorldTarget target)
        {
            if(ReferenceEquals(target,null)||!IsValidId(target.Id)||!targets.TryGetValue(target.Id,out var previous)||!ReferenceEquals(previous,target))return false;
            return targets.Remove(target.Id);
        }
    }
}
