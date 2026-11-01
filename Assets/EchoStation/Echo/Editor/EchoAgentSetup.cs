using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEngine.AI;

namespace EchoStation.Echo.Editor
{
    public static class EchoAgentSetup
    {
        [MenuItem("Tools/EchoStation/Stage 1/E02/Configure Echo Navigation Agent")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var root=PrefabUtility.LoadPrefabContents(EchoModelSetup.PrefabPath);
            try
            {
                var agent=root.GetComponent<NavMeshAgent>();if(!agent)agent=root.AddComponent<NavMeshAgent>();agent.enabled=false;
                agent.agentTypeID=EchoNavigationSetup.AgentTypeId;agent.radius=.535f;agent.height=.86f;agent.baseOffset=0;agent.speed=2.2f;agent.acceleration=8;agent.angularSpeed=180;agent.stoppingDistance=.15f;
                agent.autoBraking=true;agent.autoRepath=true;agent.obstacleAvoidanceType=ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                if(!root.GetComponent<EchoNavigation>())root.AddComponent<EchoNavigation>();PrefabUtility.SaveAsPrefabAsset(root,EchoModelSetup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
