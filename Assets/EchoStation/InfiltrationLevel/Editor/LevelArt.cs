using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt;
using EchoStation.InfiltrationArt.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EchoStation.InfiltrationLevel.Editor
{
    public static class LevelArt
    {
        public static readonly Vector3[][] Patrols={
            new[]{LevelLayout.V(-3,1),LevelLayout.V(3,1),LevelLayout.V(3,-5),LevelLayout.V(-3,-5),LevelLayout.V(-3,1)},
            new[]{LevelLayout.V(16,-2),LevelLayout.V(12,-2),LevelLayout.V(12,6),LevelLayout.V(12,14),LevelLayout.V(8,14),LevelLayout.V(8,6),LevelLayout.V(12,6),LevelLayout.V(12,-2),LevelLayout.V(16,-2)}};
        static Material label;
        static Material Solid(string name,Color color,bool unlit=false)
        {
            string path=LevelBuilder.Generated+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);if(!unlit){m.SetFloat("_Smoothness",.32f);m.SetFloat("_Metallic",.25f);}AssetDatabase.CreateAsset(m,path);LevelBuilder.Save(m);}return m;
        }
        public static void Label(Transform root,string name,string text,Vector3 point,float yaw=0,Color? tint=null,float size=.026f,bool plate=true)
        {
            if(!label){label=AssetDatabase.LoadAssetAtPath<Material>(LevelBuilder.Generated+"/IL_Label.mat");if(!label){label=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<Material>(ArtSampleAssets.Generated+"/IA_WorldLabel.mat"));label.name="IL_Label";label.shader=Shader.Find("EchoStation/InfiltrationLevel/WorldLabel");AssetDatabase.CreateAsset(label,LevelBuilder.Generated+"/IL_Label.mat");LevelBuilder.Save(label);}}
            if(plate)
            {
                var p=point+Quaternion.Euler(0,yaw,0)*Vector3.forward*.04f-Vector3.up*.3f;
                foreach(float side in new[]{90f,270f}){var backing=LevelBuilder.Spawn("WallCap",name+"_Plate",p,new Vector3(1,.6f,1),yaw+side,false);foreach(var r in backing.GetComponentsInChildren<Renderer>())r.sharedMaterial=Solid("IL_SignBacking",new Color(.035f,.07f,.105f));}
            }
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=point;go.transform.rotation=Quaternion.Euler(0,yaw,0);var t=go.AddComponent<TextMesh>();t.font=ArtSampleScene.ResolveFont();t.fontSize=80;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.text=text;t.color=tint??new Color(.8f,.9f,1);go.GetComponent<Renderer>().sharedMaterial=label;
        }
        static void Furniture(string role,string name,float x,float z,float yaw=0)=>LevelBuilder.Spawn(role,name,new Vector3(x,0,z),Vector3.one,yaw);
        static void Post(Transform root,string id,bool patrol,Vector3 point,float yaw)
        {
            var go=LevelBuilder.Spawn("Guard","Guard_"+id,point,Vector3.one,yaw,isStatic:false);var post=go.AddComponent<LevelPost>();post.Id=id;post.FuturePatrol=patrol;
            Label(root,"PostLabel_"+id,(patrol?"PATROL / ":"CHECK / ")+id+"\n岗位展示 · AI未启用",point+Vector3.up*2.45f,yaw-180,size:.015f,plate:false);
        }
        static void Route(Transform root,string id,Vector3[] points,Color color,float width)
        {
            var go=new GameObject("Route_"+id);go.transform.SetParent(root,false);var preview=go.AddComponent<LevelRoutePreview>();preview.Id=id;preview.Points=points;
            var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=points.Length;line.SetPositions(points);line.widthMultiplier=width;line.sharedMaterial=Solid("IL_Line_"+id,color,true);line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.numCornerVertices=2;line.numCapVertices=2;
        }
        static void Window(Transform root)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>t.name=="PartitionZ_4_7"||t.name=="PartitionZ_4_7_Reverse").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach(float yaw in new[]{90f,270f})
            {
                LevelBuilder.Spawn("WallCap","Window_Lower",new Vector3(0,0,12),new Vector3(1,1.1f,1),yaw,collide:yaw==90);
                LevelBuilder.Spawn("WallCap","Window_Upper",new Vector3(0,2.5f,12),new Vector3(1,2,1),yaw,collide:yaw==90);
            }
            var mesh=new Mesh{name="IL_WindowPane"};mesh.vertices=new[]{new Vector3(-1.94f,-.7f,0),new Vector3(1.94f,-.7f,0),new Vector3(1.94f,.7f,0),new Vector3(-1.94f,.7f,0)};mesh.triangles=new[]{0,2,1,0,3,2};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,LevelBuilder.Generated+"/IL_WindowPane.asset");LevelBuilder.Save(mesh);
            var glass=Solid("IL_Glass",new Color(.38f,.7f,.79f,.04f));glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetFloat("_BlendModePreserveSpecular",0);glass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.SetInt("_Cull",0);glass.SetFloat("_Smoothness",.5f);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;LevelBuilder.Save(glass);
            var go=new GameObject("ObservationWindow_Glass");go.transform.SetParent(root,false);go.transform.position=new Vector3(0,1.8f,12);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=glass;go.AddComponent<LevelSolid>();go.AddComponent<BoxCollider>().size=new Vector3(3.88f,1.4f,.06f);
        }
        public static void Dress()
        {
            if(File.Exists(LevelBuilder.Evidence+"/Dressed_A.json"))throw new InvalidOperationException("Preserve dressing receipt");
            LevelBuilder.Edit(root=>
            {
                Furniture("Desk","Reception_CheckDesk",-3.1f,-10.5f,180);Furniture("Terminal","Reception_VisitorTerminal",2.5f,-10,180);LevelBuilder.Spawn("Toolbox","Reception_MaintenanceTools",new Vector3(-3.1f,.75f,-10.5f),Vector3.one);
                foreach(float x in new[]{-16f,-14.5f,14.5f,16f})Furniture("Chair","Reception_Waiting",x,-14,0);
                foreach(float x in new[]{-16f,16f}){Furniture("EquipmentCase","Reception_ServiceCabinet",x,-10.5f,180);Furniture("Terminal","Reception_Information",x,-12,90);}
                foreach(float x in new[]{-16f,-8.2f})foreach(float z in new[]{-5.5f,1f}){Furniture("Desk","Admin_Workstation",x,z,x<-12?90:270);Furniture("Chair","Admin_Chair",x+(x<-12?1:-1),z,x<-12?270:90);Furniture("Terminal","Admin_DataTerminal",x,z+1.55f,180);}
                foreach(float z in new[]{-2f,3f})foreach(float yaw in new[]{90f,270f})LevelBuilder.Spawn("Wall","Admin_PrivacyScreen",new Vector3(-16,0,z),new Vector3(1,.75f,1),yaw,collide:yaw==90);
                foreach(float x in new[]{9f,15f})foreach(float z in new[]{-5.3f,1.4f})LevelBuilder.Spawn("EquipmentCase","Cargo_TallCover",new Vector3(x,0,z),new Vector3(1.8f,2.1f,1.6f));
                foreach(float x in new[]{-16f,16f})foreach(float z in new[]{6f,10f}){LevelBuilder.Spawn("EquipmentCase","Maintenance_Equipment",new Vector3(x,0,z),new Vector3(1.35f,1.7f,1));Furniture("Column","Maintenance_Support",x+Mathf.Sign(x)*1.5f,z+1.5f,0);}
                foreach(float x in new[]{-3.6f,3.6f})foreach(float z in new[]{6f,11f})LevelBuilder.Spawn("EquipmentCase","Core_ServerEnclosure",new Vector3(x,0,z),new Vector3(1.25f,2.3f,.7f));
                Furniture("Terminal","Core_DataTerminal",0,11,180);Furniture("Desk","Core_SecurityDesk",3.3f,8.3f,270);Furniture("Chair","Core_Chair",4.7f,8.3f,90);
                Furniture("Terminal","Atrium_Information",-4.6f,1.5f,90);Furniture("EquipmentCase","Atrium_Cover_01",-4.6f,-4.8f,90);Furniture("EquipmentCase","Atrium_Cover_02",4.6f,1.4f,90);
                Post(root,"G01",false,new Vector3(-3.1f,0,-9),180);Post(root,"G02",false,new Vector3(1.5f,0,5.7f),180);Post(root,"P01",true,new Vector3(-2.8f,0,1.2f),180);Post(root,"P02",true,new Vector3(16.4f,0,-1.6f),270);
                LevelBuilder.Spawn("Echo","Echo_VisualOnly",new Vector3(-1.4f,0,-12.1f),Vector3.one*1.12f,155,isStatic:false);
                Window(root);
                Label(root,"Entry_Top","ECHO STATION / VISITOR HALL\n接待大厅 · 三路可自由走通",new Vector3(0,3.8f,-15.6f),180);
                Label(root,"Route_Main","CHECKPOINT / 01\n正面检查路线 · 机制待接入",new Vector3(0,3.6f,-8.32f),tint:new Color(.4f,.72f,1));
                Label(root,"Route_Admin","ADMIN / 02\n行政办公 · 员工路径预留",new Vector3(-12,3.6f,-8.32f),tint:new Color(1,.64f,.32f));
                Label(root,"Route_Cargo","LOGISTICS / 03\n物流仓区 · 检修绕行",new Vector3(12,3.6f,-8.32f),tint:new Color(.3f,.95f,.82f));
                Label(root,"Atrium_Title","CENTRAL ATRIUM\n中央中庭",new Vector3(-3.6f,3.3f,3.7f),size:.023f);
                Label(root,"Core_Title","DATA / SECURITY\n数据核心前区",new Vector3(0,3.65f,3.7f));
                Label(root,"Rear_Observation","OBSERVATION / RETURN LOOP\n观察窗口 · 后侧回环",new Vector3(0,3.3f,12.2f),180);
                Label(root,"Rear_Left","SERVICE LOOP / WEST\n西侧检修区",new Vector3(-12,3.3f,15.7f));Label(root,"Rear_Right","SERVICE LOOP / EAST\n东侧检修区",new Vector3(12,3.3f,15.7f));
                Label(root,"Fork_Left","← 行政路线",new Vector3(-4.3f,2.9f,-11),size:.018f,plate:false,tint:new Color(1,.64f,.32f));Label(root,"Fork_Right","物流路线 →",new Vector3(4.3f,2.9f,-11),size:.018f,plate:false,tint:new Color(.3f,.95f,.82f));
                for(int i=0;i<3;i++)Route(root,"Player_"+i,LevelLayout.Routes[i],i==0?new Color(.18f,.55f,.9f):i==1?new Color(.9f,.42f,.13f):new Color(.13f,.72f,.58f),.035f);
                for(int i=0;i<2;i++)Route(root,"Patrol_"+i,Patrols[i],new Color(.7f,.58f,.18f),.025f);
                LevelBuilder.BuildNav(root,"B");
                File.WriteAllText(LevelBuilder.Evidence+"/Dressed_A.json",new JObject{{"netArea",LevelLayout.ConservativeNetArea(root)},{"posts",root.GetComponentsInChildren<LevelPost>().Length},{"patrolPreviews",2},{"aiEnabled",false},{"models",root.GetComponentsInChildren<ArtSampleModel>().Length}}.ToString());
            });
        }
        public static void Refine()
        {
            if(File.Exists(LevelBuilder.Evidence+"/ArtRefinement_B.json"))throw new InvalidOperationException("Preserve refinement");
            LevelBuilder.Edit(root=>
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>(LevelBuilder.Generated+"/IL_Label.mat");m.shader=Shader.Find("EchoStation/InfiltrationLevel/WorldLabel");LevelBuilder.Save(m);
                var glass=AssetDatabase.LoadAssetAtPath<Material>(LevelBuilder.Generated+"/IL_Glass.mat");var c=glass.GetColor("_BaseColor");c.a=.04f;glass.SetColor("_BaseColor",c);glass.SetFloat("_BlendModePreserveSpecular",0);glass.SetInt("_SrcBlend",5);glass.SetInt("_DstBlend",10);glass.DisableKeyword("_ALPHAPREMULTIPLY_ON");glass.SetFloat("_Smoothness",.5f);LevelBuilder.Save(glass);
                root.Find("PostLabel_P02").rotation=Quaternion.Euler(0,90,0);
                foreach(float x in new[]{-6f,6f})foreach(float z in new[]{-6f,-2f,2f})foreach(float yaw in new[]{0f,180f})LevelBuilder.Spawn("WallCap","Atrium_UpperSide",new Vector3(x,4.5f,z),new Vector3(1,1.503f,1),yaw,false);
                foreach(float z in new[]{-8f,4f})foreach(float x in new[]{-4f,0f,4f})foreach(float yaw in new[]{90f,270f})LevelBuilder.Spawn("WallCap","Atrium_UpperCross",new Vector3(x,4.5f,z),new Vector3(1,1.503f,1),yaw,false);
                File.WriteAllText(LevelBuilder.Evidence+"/ArtRefinement_B.json",new JObject{{"textBackfaceCulled",true},{"glassPreserveSpecular",false},{"glassAlpha",.04},{"atriumUpperRingAdded",true},{"ceilingStripeCause","Not yet isolated; inspect shadow and geometry separately in C"}}.ToString());
            });
        }
        public static void FixDoubleSidedBands()
        {
            if(File.Exists(LevelBuilder.Evidence+"/CullingFix_C.json"))throw new InvalidOperationException("Preserve culling receipt");
            var rows=new JArray();foreach(string path in AssetDatabase.FindAssets("t:Material",new[]{LevelBuilder.Generated}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material.shader.name!="Universal Render Pipeline/Lit"||material.name=="IL_Glass")continue;
                float before=material.GetFloat("_Cull");if(before!=2){material.SetFloat("_Cull",2);LevelBuilder.Save(material);rows.Add(new JObject{{"path",path},{"before",before},{"after",2}});}
            }
            File.WriteAllText(LevelBuilder.Evidence+"/CullingFix_C.json",rows.ToString());
        }
    }
}
