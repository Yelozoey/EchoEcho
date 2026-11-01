using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EchoStation.World;
using UnityEngine;

namespace EchoStation.Player
{
    [Serializable]public sealed class SavePayload
    {
        public string sceneId;public Vector3 playerPosition,echoPosition;public float playerYaw,playerPitch,echoYaw;public bool flashlightOn;public string echoMode;public string[] journalIds,scanIds;
        public void Validate()
        {
            if(sceneId!="cabin_s1"||!Pose(playerPosition)||!Pose(echoPosition)||!Angle(playerYaw)||!Angle(echoYaw)||!float.IsFinite(playerPitch)||Math.Abs(playerPitch)>80||(echoMode!="follow"&&echoMode!="wait"))throw new ArgumentException("Invalid S1 scene, pose or stable Echo mode.");Ids(journalIds);Ids(scanIds);
        }
        private static bool Pose(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)&&Math.Abs(p.x)<=100&&Math.Abs(p.y)<=100&&Math.Abs(p.z)<=100;
        private static bool Angle(float value)=>float.IsFinite(value)&&value>=0&&value<360;
        private static void Ids(string[] values){if(values==null||values.Length>128)throw new ArgumentException("Invalid save ID list.");var unique=new HashSet<string>(StringComparer.Ordinal);foreach(var id in values)if(!TargetRegistry.IsValidId(id)||!unique.Add(id))throw new ArgumentException("Invalid or duplicate save ID.");}
    }
    public enum SaveStatus{Loaded,Backup,Missing,Invalid,UnsupportedVersion,IoError}
    public sealed class SaveReadResult
    {
        public SaveStatus Status{get;}public SavePayload Data{get;}public string Detail{get;}
        public bool Success=>Status==SaveStatus.Loaded||Status==SaveStatus.Backup;
        public SaveReadResult(SaveStatus status,SavePayload data=null,string detail=""){Status=status;Data=data;Detail=detail;}
    }
    public static class SaveCodec
    {
        public const int MaximumBytes=65536;
        [Serializable]private sealed class Envelope{public int version;public string utc,payload,sha256;}
        public static string Encode(SavePayload data)
        {
            if(data==null)throw new ArgumentNullException(nameof(data));data.Validate();var payload=JsonUtility.ToJson(data);var json=JsonUtility.ToJson(new Envelope{version=1,utc=DateTimeOffset.UtcNow.ToString("O"),payload=payload,sha256=Hash(payload)},true);if(Encoding.UTF8.GetByteCount(json)>MaximumBytes)throw new ArgumentException("Save exceeds byte budget.");return json;
        }
        public static SaveReadResult Decode(string json)
        {
            if(string.IsNullOrWhiteSpace(json)||Encoding.UTF8.GetByteCount(json)>MaximumBytes)return new SaveReadResult(SaveStatus.Invalid,detail:"Save empty or too large.");
            try{var envelope=JsonUtility.FromJson<Envelope>(json);if(envelope==null)return new SaveReadResult(SaveStatus.Invalid);if(envelope.version!=1)return new SaveReadResult(envelope.version>1?SaveStatus.UnsupportedVersion:SaveStatus.Invalid,detail:"Unsupported save version.");if(!DateTimeOffset.TryParseExact(envelope.utc,"O",CultureInfo.InvariantCulture,DateTimeStyles.None,out var utc)||utc.Offset!=TimeSpan.Zero||string.IsNullOrEmpty(envelope.payload)||Hash(envelope.payload)!=envelope.sha256)return new SaveReadResult(SaveStatus.Invalid,detail:"Save metadata or checksum mismatch.");var data=JsonUtility.FromJson<SavePayload>(envelope.payload);if(data==null)return new SaveReadResult(SaveStatus.Invalid);data.Validate();return new SaveReadResult(SaveStatus.Loaded,data);}
            catch(ArgumentException exception){return new SaveReadResult(SaveStatus.Invalid,detail:exception.Message);}
        }
        private static string Hash(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
    }
}
