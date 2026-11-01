using System;
using System.Linq;
using UnityEngine;
using EchoStation.Dialogue;
using EchoStation.Echo;

namespace EchoStation.Salvage
{
    [Serializable]public sealed class SalvagePose
    {
        public float X,Y,Z,Yaw,Pitch;
        public Vector3 Position()=>new Vector3(X,Y,Z);
        public static SalvagePose From(Transform t,float pitch=0)=>new SalvagePose{X=t.position.x,Y=t.position.y,Z=t.position.z,Yaw=Mathf.Repeat(t.eulerAngles.y,360),Pitch=pitch};
    }
    [Serializable]public sealed class SavedCargo{public string Id;public int State;public float X,Y,Z;public Vector3 Position()=>new Vector3(X,Y,Z);}
    [Serializable]public sealed class SavedDialogue
    {
        public long Number;public int Speaker;public string Text;public bool Refusal;
        public DialogueMessage Message()=>new DialogueMessage(Number,(DialogueSpeaker)Speaker,Text,Refusal);
        public static SavedDialogue From(DialogueMessage m)=>new SavedDialogue{Number=m.Number,Speaker=(int)m.Speaker,Text=m.Text,Refusal=m.IsRefusal};
    }
    [Serializable]public sealed class SavedCollaboration
    {
        public int BaseMode=(int)IntentKind.Follow;public string[] Discovered=Array.Empty<string>();public SavedDialogue[] Messages=Array.Empty<SavedDialogue>();
        public DialogueSession Session(){Validate(this);var session=new DialogueSession();if(!session.TryRestoreRecent(Messages.Select(m=>m.Message()).ToArray()))throw new ArgumentException("Invalid saved history.");return session;}
        public static void Validate(SavedCollaboration s)
        {
            if(s==null||s.BaseMode!=(int)IntentKind.Follow&&s.BaseMode!=(int)IntentKind.Wait||s.Discovered==null||s.Discovered.Length>8||s.Discovered.Distinct(StringComparer.Ordinal).Count()!=s.Discovered.Length||s.Discovered.Any(id=>!SalvageTargetCatalog.TryGet(id,out _))||s.Messages==null||s.Messages.Length>50||s.Messages.Any(m=>m==null))throw new ArgumentException("Invalid collaboration snapshot.");
            long previous=0;foreach(var m in s.Messages){var message=m.Message();if(message.Number<=previous||message.Number==long.MaxValue)throw new ArgumentException("Invalid history order.");previous=message.Number;}
        }
    }
    [Serializable]public sealed class SalvageSnapshot
    {
        public SalvagePose Player,Echo;public bool Flashlight,Warned,Completed;public float Charge;public double Elapsed;public SavedCargo[] Cargo;
        public SavedCollaboration Collaboration=new SavedCollaboration();
        public static void Validate(SalvageSnapshot s)
        {
            if(s==null||s.Player==null||s.Echo==null||s.Cargo==null||s.Cargo.Length!=7)throw new ArgumentException("Incomplete salvage snapshot.");
            SavedCollaboration.Validate(s.Collaboration);
            Pose(s.Player,4);Pose(s.Echo,1);
            if(!float.IsFinite(s.Charge)||s.Charge<0||s.Charge>100||s.Charge<25&&!s.Warned||!double.IsFinite(s.Elapsed)||s.Elapsed<0||s.Elapsed>86400)throw new ArgumentException("Invalid battery or elapsed.");
            if(s.Cargo.Any(c=>c==null)||s.Cargo.Select(c=>c.Id).Distinct().Count()!=7||s.Cargo.Count(c=>c.State==1)>1)throw new ArgumentException("Duplicate or multiple held cargo.");
            foreach(var c in s.Cargo){var d=SalvageCatalog.Get(c.Id);if(c.State<0||c.State>2||d.Kilograms>8&&c.State!=0||!Inside(c.Position(),.5f))throw new ArgumentException("Invalid cargo state/position.");}
            var delivered=s.Cargo.Where(c=>c.State==2).ToArray();if(s.Completed&&(delivered.Length<3||delivered.Sum(c=>SalvageCatalog.Get(c.Id).Points)<12))throw new ArgumentException("Unmet completed mission.");
        }
        static void Pose(SalvagePose p,float maxY){if(!Inside(p.Position(),maxY)||!float.IsFinite(p.Yaw)||p.Yaw<0||p.Yaw>=360||!float.IsFinite(p.Pitch)||Mathf.Abs(p.Pitch)>80)throw new ArgumentException("Invalid pose.");}
        static bool Inside(Vector3 p,float maxY)=>CargoRegistry.Finite(p)&&p.x>=.3f&&p.x<=35.7f&&p.z>=.3f&&p.z<=23.7f&&p.y>=-.1f&&p.y<=maxY;
    }
}
