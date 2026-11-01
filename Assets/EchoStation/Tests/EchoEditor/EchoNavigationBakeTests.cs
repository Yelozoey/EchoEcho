using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EchoStation.Echo.Editor;
using EchoStation.World.Editor;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace EchoStation.Tests.Echo
{
    public class EchoNavigationBakeTests
    {
        [Test]public void E02_A01_EchoSettingsAreDedicatedAndOriginalHumanoidAndAreasRemainUnchanged()
        {
            var id=EchoNavigationSetup.AgentTypeId;Assert.That(id,Is.Not.EqualTo(-1));Assert.That(id,Is.Not.EqualTo(0));var value=NavMesh.GetSettingsByID(id);
            Assert.That(value.agentRadius,Is.EqualTo(.54f));Assert.That(value.agentHeight,Is.EqualTo(.9f));Assert.That(value.agentClimb,Is.EqualTo(.15f));Assert.That(value.agentSlope,Is.EqualTo(35));
            var box=AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath).GetComponent<BoxCollider>();Assert.That(value.agentRadius,Is.GreaterThanOrEqualTo(new Vector2(box.size.x,box.size.z).magnitude*.5f));
            var before=File.ReadAllText(EchoNavigationSetup.Evidence+"/NavMeshAreas-Before.asset.txt");var after=File.ReadAllText("ProjectSettings/NavMeshAreas.asset");
            Assert.That(Regex.Match(after,@"  areas:[\s\S]*?  m_LastAgentTypeID").Value,Is.EqualTo(Regex.Match(before,@"  areas:[\s\S]*?  m_LastAgentTypeID").Value));
            var original=NavMesh.GetSettingsByID(0);Assert.That(original.agentRadius,Is.EqualTo(.5f));Assert.That(original.agentHeight,Is.EqualTo(2));Assert.That(original.agentClimb,Is.EqualTo(.75f));Assert.That(original.agentSlope,Is.EqualTo(45));
        }
        [Test]public void E02_A02_CabinHasPersistentColliderBasedSurfaceWithMeasuredVoxelSize()
        {
            var scene=EditorSceneManager.OpenPreviewScene(CabinGreyboxSetup.ScenePath);
            try
            {
                var surface=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox").GetComponent<NavMeshSurface>();Assert.That(surface,Is.Not.Null);
                Assert.That(surface.agentTypeID,Is.EqualTo(EchoNavigationSetup.AgentTypeId));Assert.That(surface.collectObjects,Is.EqualTo(CollectObjects.Children));Assert.That(surface.useGeometry,Is.EqualTo(NavMeshCollectGeometry.PhysicsColliders));
                Assert.That(surface.overrideVoxelSize&&surface.overrideTileSize&&surface.ignoreNavMeshAgent&&surface.ignoreNavMeshObstacle,Is.True);
                Assert.That(new SerializedObject(surface).FindProperty("m_GenerateLinks").boolValue,Is.False);
                Assert.That(surface.voxelSize,Is.EqualTo(.08f));Assert.That(surface.tileSize,Is.EqualTo(128));Assert.That(AssetDatabase.GetAssetPath(surface.navMeshData),Is.EqualTo(EchoNavigationSetup.DataPath));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test]public void E02_A03_ActualSavedNavMeshConnectsBothDoorways()
        {
            var scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                var filter=new NavMeshQueryFilter{agentTypeID=EchoNavigationSetup.AgentTypeId,areaMask=NavMesh.AllAreas};var points=new[]{new Vector3(0,0,-6),new Vector3(2,0,-1),new Vector3(7.8f,0,2.2f)};
                for(var i=0;i<points.Length-1;i++)
                {
                    Assert.That(NavMesh.SamplePosition(points[i],out var start,.5f,filter),Is.True);Assert.That(NavMesh.SamplePosition(points[i+1],out var end,.5f,filter),Is.True);
                    var path=new NavMeshPath();Assert.That(NavMesh.CalculatePath(start.position,end.position,filter,path),Is.True);Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete));Assert.That(path.corners.Length,Is.GreaterThanOrEqualTo(2));
                }
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
