using System;
using EchoStation.Player;
using NUnit.Framework;
using UnityEngine;

namespace EchoStation.Tests.Player
{
    public class SaveCodecTests
    {
        public static SavePayload Fixture()=>new SavePayload{sceneId="cabin_s1",playerPosition=new Vector3(1,.05f,-2),playerYaw=120,playerPitch=-15,echoPosition=new Vector3(0,0,1),echoYaw=90,echoMode="wait",flashlightOn=true,journalIds=new[]{"note_maintenance_01","scan_mark_01"},scanIds=new[]{"scan_mark_01"}};
        [Test]public void P07_A01_JsonRoundTripPreservesAllFieldsAndIsIndependent()
        {
            var data=Fixture();var json=SaveCodec.Encode(data);var read=SaveCodec.Decode(json);Assert.That(read.Status,Is.EqualTo(SaveStatus.Loaded));Assert.That(JsonUtility.ToJson(read.Data),Is.EqualTo(JsonUtility.ToJson(data)));data.journalIds[0]="changed_id";Assert.That(read.Data.journalIds[0],Is.EqualTo("note_maintenance_01"));Assert.That(json,Does.Not.Contain("FullText"));Assert.That(json,Does.Not.Contain("UseLLM"));
        }
        [Test]public void P07_A02_CorruptionEmptyOversizeAndFutureVersionAreRejected()
        {
            var json=SaveCodec.Encode(Fixture());foreach(var invalid in new[]{"","{", "null",new string('x',65537),json.Replace("note_maintenance_01","note_maintenance_02"),json.Replace("\"version\": 1","\"version\": 0")})Assert.That(SaveCodec.Decode(invalid).Success,Is.False,invalid.Length.ToString());Assert.That(SaveCodec.Decode(json.Replace("\"version\": 1","\"version\": 99")).Status,Is.EqualTo(SaveStatus.UnsupportedVersion));
        }
        [Test]public void P07_A03_NonfiniteOutOfBoundsWrongSceneTransientModeAndDuplicateIdsReject()
        {
            Action<SavePayload>[] mutations={p=>p.playerPosition=new Vector3(float.NaN,0,0),p=>p.echoPosition=Vector3.one*101,p=>p.playerYaw=360,p=>p.playerPitch=81,p=>p.echoYaw=float.PositiveInfinity,p=>p.sceneId="other",p=>p.echoMode="investigate",p=>p.journalIds=null,p=>p.scanIds=new[]{"same_id","same_id"},p=>p.journalIds=new[]{"BAD ID"},p=>p.journalIds=new string[129]};foreach(var mutate in mutations){var data=Fixture();mutate(data);Assert.Throws<ArgumentException>(()=>SaveCodec.Encode(data));}Assert.Throws<ArgumentNullException>(()=>SaveCodec.Encode(null));
        }
    }
}
