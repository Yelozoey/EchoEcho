using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt.Editor;
using EchoStation.InfiltrationLevel;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoStation.InfiltrationRefinement.Editor
{
    public static class RefinementDressing
    {
        static Transform parent;
        static void Place(string kind,int zone,Vector3 point,float yaw=0,bool obstacle=true)
        {
            if(kind=="Cable")
            {
                point.x=zone==2?(point.x< -12?-17.82f:-6.18f):zone==3?17.82f:zone==4?Mathf.Sign(point.x)*17.82f:Mathf.Sign(point.x)*5.82f;
                yaw=(zone==2&&point.x> -12)||point.x>0?0:180;
            }
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(RefinementAssets.Prefab(kind));var go=(GameObject)PrefabUtility.InstantiatePrefab(source,parent.gameObject.scene);go.name="Detail_"+zone+"_"+kind;go.transform.SetParent(parent,false);go.transform.position=point;go.transform.rotation=Quaternion.Euler(0,yaw,0);
            if(kind=="Fan"||kind=="Vent")go.transform.rotation=Quaternion.Euler(90,yaw,0);
            var marker=go.AddComponent<RefinementDecoration>();marker.Kind=kind;marker.Zone=zone;marker.FloorObstacle=obstacle;
            foreach(var t in go.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.ReflectionProbeStatic|StaticEditorFlags.OccludeeStatic);
            foreach(var r in go.GetComponentsInChildren<MeshRenderer>()){r.receiveGI=ReceiveGI.Lightmaps;r.scaleInLightmap=kind=="Mug"?.2f:.6f;r.shadowCastingMode=kind=="Mug"||kind=="Cable"||kind=="Fan"||kind=="Vent"?ShadowCastingMode.Off:ShadowCastingMode.On;}
            if(obstacle){go.AddComponent<LevelSolid>();var b=ArtSampleAssets.LocalBoundsOf(go);var c=go.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;}
        }
        static void Floor(string kind,int zone,float x,float z,float yaw=0)=>Place(kind,zone,new Vector3(x,0,z),yaw);
        static void Mount(string kind,int zone,float x,float y,float z,float yaw=0)=>Place(kind,zone,new Vector3(x,y,z),yaw,false);
        public static void RefreshProps(string version)
        {
            string receipt=RefinementScene.Evidence+"/Refreshed_"+version+".json";if(File.Exists(receipt))throw new InvalidOperationException("Preserve refresh receipt");
            RefinementScene.Edit(root=>
            {
                parent=root.Find("DR_FunctionalDetails");var old=parent.GetComponentsInChildren<RefinementDecoration>();
                foreach(var item in old){var p=item.transform.position;var rotation=item.transform.rotation;Place(item.Kind,item.Zone,p,rotation.eulerAngles.y,item.FloorObstacle);if(item.Kind!="Cable")parent.GetChild(parent.childCount-1).rotation=rotation;UnityEngine.Object.DestroyImmediate(item.gameObject);}
                RefinementScene.BuildNav(root,version);File.WriteAllText(receipt,new JObject{{"count",parent.GetComponentsInChildren<RefinementDecoration>().Length},{"freshOwnedPrefabInstances",true},{"netArea",LevelLayout.ConservativeNetArea(root)}}.ToString());parent=null;
            });
        }
        public static void Apply()
        {
            if(File.Exists(RefinementScene.Evidence+"/Dressing_A.json"))throw new InvalidOperationException("Preserve dressing receipt");
            RefinementScene.Edit(root=>
            {
                parent=new GameObject("DR_FunctionalDetails").transform;parent.SetParent(root,false);
                // Reception: clustered waiting, a staffed check desk and wall storage.
                foreach(float x in new[]{-10f,-7f,7f,10f})Floor("Seat",0,x,-14.6f);
                foreach(float x in new[]{-10f,10f})Floor("Locker",0,x,-15.4f);
                Mount("Mug",0,-4f,.746f,-10.5f);Mount("Mug",0,-2.25f,.746f,-10.35f);
                foreach(float x in new[]{-16.7f,16.7f})Floor("TarpCrate",0,x,-12.8f);
                Mount("Vent",0,-17.7f,3,-10,90);Mount("Vent",0,17.7f,3,-10,-90);
                // Office: existing workstations gain real desktop objects; tall props stay at the edges.
                foreach(float z in new[]{-6.6f,-3.5f,.5f,2.8f})Floor("Locker",2,-17.2f,z,90);
                Floor("Shelf",2,-9,3.4f,180);Floor("Shelf",2,-14.8f,3.4f,180);
                foreach(float x in new[]{-16f,-8.2f})foreach(float z in new[]{-5.5f,1f}){Mount("Mug",2,x+.25f,.746f,z+.15f);Mount("Computer",2,x,.746f,z-.25f,x<-12?90:270);}
                foreach(float x in new[]{-15f,-9f}){Mount("Cable",2,x,4.1f,2.5f);Mount("Vent",2,x,3.1f,3.72f,180);}
                // Logistics: a center lane remains free between two groups of existing cover.
                foreach(float z in new[]{-5.8f,1f})Floor("Shelf",3,17,z,270);
                foreach(float x in new[]{7.2f,16.7f})foreach(float z in new[]{-7.1f,3f})Floor("TarpCrate",3,x,z);
                foreach(var p in new[]{new Vector2(7.1f,-4),new Vector2(17,-2.7f),new Vector2(7.1f,1.2f),new Vector2(17,3.1f)})Floor("Barrel",3,p.x,p.y);
                foreach(float z in new[]{-3f,1f}){Mount("Cable",3,7.1f,4.1f,z);Mount("Vent",3,17.7f,2.8f,z,-90);}
                // U-shaped service loop: ceiling/wall services, not knee-height trip hazards.
                foreach(float x in new[]{-12f,12f})foreach(float z in new[]{6f,10f,14f})Mount("Cable",4,x,4.1f,z);
                foreach(float x in new[]{-17.7f,17.7f})foreach(float z in new[]{7.8f,14f})Mount("Vent",4,x,2.9f,z,x<0?90:-90);
                foreach(float x in new[]{-17.7f,17.7f})Mount("Fan",4,x,2.8f,10.5f,x<0?90:-90);
                foreach(float x in new[]{-8f,8f}){Floor("TarpCrate",4,x,14.8f);Floor("Locker",4,x,7.3f,x<0?270:90);Floor("Barrel",4,Mathf.Sign(x)*17,14.8f);}
                // Atrium stays open; details frame the high central space.
                foreach(float x in new[]{-4.9f,4.9f})foreach(float z in new[]{-5f,1f})Mount("Cable",1,x,5.55f,z);
                foreach(float x in new[]{-5.7f,5.7f}){Mount("Vent",1,x,3.7f,1.5f,x<0?90:-90);Floor("Locker",1,x,-6.5f,x<0?90:270);}
                // Core: compact equipment accents preserve the rear observation window.
                Mount("Mug",5,3.3f,.746f,8.5f);Mount("Mug",5,3.3f,.746f,8.1f);
                foreach(float x in new[]{-4.8f,4.8f}){Mount("Cable",5,x,4.1f,7f);Mount("Vent",5,x,3.7f,4.15f);Floor("Locker",5,Mathf.Sign(x)*5.4f,7.7f,x<0?90:270);}
                RefinementScene.BuildNav(root,"D01");var details=root.GetComponentsInChildren<RefinementDecoration>();
                File.WriteAllText(RefinementScene.Evidence+"/Dressing_A.json",new JObject{{"count",details.Length},{"kinds",new JArray(details.Select(d=>d.Kind).Distinct())},{"zones",new JArray(details.GroupBy(d=>d.Zone).OrderBy(g=>g.Key).Select(g=>new JObject{{"zone",g.Key},{"count",g.Count()}}))},{"netArea",LevelLayout.ConservativeNetArea(root)},{"inheritsA01BakeForGeometryReviewOnly",true},{"newAiOrInteraction",false}}.ToString());parent=null;
            });
        }
    }
}
