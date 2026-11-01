using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EchoStation.ParkourArt.Editor
{
    public static class VectorCityBuilder
    {
        public const string Root = "Assets/EchoStation/ParkourArt";
        public const string ScenePath = Root + "/Scenes/ES_VectorCity_Parkour.unity";
        public static Transform City { get; private set; }
        static Material white, chalk, red, cyan, grey, glass;
        public static void Folders()
        {
            foreach (var folder in new[]{"Scenes", "Materials", "Settings", "Reflections"})
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
        }
        static Material Solid(string name, Color color, float smoothness)
        {
            var path=Root+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
            m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",smoothness);
            EditorUtility.SetDirty(m); return m;
        }
        public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collision=true)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name;
            g.transform.SetParent(parent,false);g.transform.position=position;g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=material;
            g.GetComponent<Renderer>().reflectionProbeUsage=ReflectionProbeUsage.BlendProbesAndSkybox;
            if(!collision) Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }
        static Transform Group(string name,Transform parent)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
        static Transform Roof(string name,Vector3 top,Vector2 size,Material facade,Material floor)
        {
            var t=Group(name,City);
            Box("Building_Mass",t,new Vector3(top.x,top.y/2-6,top.z),new Vector3(size.x,top.y+12,size.y),facade);
            Box("Roof_Cap",t,top-new Vector3(0,.20f,0),new Vector3(size.x+.45f,.4f,size.y+.45f),floor);
            // One broad inset is a visual zone, never a dirt or detail texture.
            Box("Facade_ShadowBand",t,new Vector3(top.x,top.y-1.05f,top.z),new Vector3(size.x+.06f,.34f,size.y+.06f),grey,false);
            return t;
        }
        static void Rail(Transform parent,Vector3 a,Vector3 b)
        {
            var delta=b-a; var count=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/4));
            for(int i=0;i<=count;i++)Box("Rail_Post",parent,Vector3.Lerp(a,b,(float)i/count)+Vector3.up*.55f,new Vector3(.1f,1.1f,.1f),white);
            var beam=Box("Rail_Top",parent,(a+b)/2+Vector3.up*1.08f,new Vector3(.11f,.11f,delta.magnitude+.1f),white);
            beam.transform.rotation=Quaternion.LookRotation(delta);
            var knee=Box("Rail_Mid",parent,(a+b)/2+Vector3.up*.52f,new Vector3(.07f,.07f,delta.magnitude),white);
            knee.transform.rotation=Quaternion.LookRotation(delta);
        }
        static void Stripe(Transform p,Vector3 a,Vector3 b,float width,Material m)
        {
            var g=Box("Route_Ribbon",p,(a+b)/2,new Vector3(width,.018f,(b-a).magnitude),m,false);
            g.transform.rotation=Quaternion.LookRotation(b-a);
        }
        static void Arrow(Transform p,Vector3 at,float yaw,Material m)
        {
            var t=Group("Vector_Arrow",p);t.position=at;t.rotation=Quaternion.Euler(0,yaw,0);
            var stem=Box("Stem",t,at,new Vector3(.22f,.021f,1.2f),m,false);stem.transform.localPosition=new Vector3(0,0,-.25f);stem.transform.localRotation=Quaternion.identity;
            for(int i=-1;i<=1;i+=2){var wing=Box("Wing",t,at,new Vector3(.2f,.021f,.9f),m,false);wing.transform.localPosition=new Vector3(i*.27f,0,.2f);wing.transform.localRotation=Quaternion.Euler(0,-i*43,0);}
        }
        static void Ramp(string name,Transform p,Vector3 a,Vector3 b,float width,Material m)
        {
            var d=b-a;var flat=new Vector3(d.x,0,d.z);
            var rotation=Quaternion.LookRotation(flat)*Quaternion.Euler(-Mathf.Atan2(d.y,flat.magnitude)*Mathf.Rad2Deg,0,0);
            var g=Box(name,p,(a+b)/2-rotation*Vector3.up*.2f,new Vector3(width,.4f,d.magnitude),m);
            g.transform.rotation=rotation;
        }
        static void Digit(Transform p,int digit,Vector3 at,float size,Material material)
        {
            string[] masks={"1111110","0110000","1101101","1111001","0110011","1011011","1011111","1110000","1111111","1111011"};
            var points=new[]{new Vector2(0,1),new Vector2(.5f,.5f),new Vector2(.5f,-.5f),new Vector2(0,-1),new Vector2(-.5f,-.5f),new Vector2(-.5f,.5f),new Vector2(0,0)};
            for(int i=0;i<7;i++)if(masks[digit][i]=='1')
                Box("Vector_Digit_"+digit,p,at+new Vector3(points[i].x*size,0,points[i].y*size),i==0||i==3||i==6?new Vector3(size,.018f,size*.13f):new Vector3(size*.13f,.018f,size),material,false);
        }
        [MenuItem("Tools/EchoStation/Parkour Art/01 Generate Geometry")]
        public static void Generate()
        {
            SceneSafetyGuard.RequireClean();Folders();
            if(File.Exists(ScenePath))throw new InvalidOperationException("Owned scene exists; edit it explicitly instead of regenerating over changes.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            City=new GameObject("VECTOR CITY — Rooftop Run").transform;
            white=Solid("White_Porcelain",new Color(.86f,.89f,.9f),.38f);
            chalk=Solid("White_Chalk",new Color(.72f,.78f,.81f),.28f);
            red=Solid("Runner_Red",new Color(.8f,.018f,.032f),.5f);
            cyan=Solid("Atrium_Cyan",new Color(.015f,.51f,.62f),.52f);
            grey=Solid("Structural_Grey",new Color(.29f,.36f,.4f),.36f);
            glass=Solid("Glass_Cyan",new Color(.08f,.32f,.41f),.93f);
            var a=Roof("01 — WHITE / Departure",new Vector3(-22,12,-20),new Vector2(24,24),chalk,white);
            var b=Roof("02 — RED / Momentum",new Vector3(-22,13,0),new Vector2(18,14),white,red);
            var c=Roof("03 — CYAN / Atrium",new Vector3(1,15,5),new Vector2(22,24),cyan,white);
            var d=Roof("04 — WHITE / Ascent",new Vector3(8,18,28),new Vector2(22,16),chalk,white);
            var e=Roof("05 — WHITE / Arrival",new Vector3(-12,20,39),new Vector2(16,20),white,white);
            Box("Departure_Portal_Left",a,new Vector3(-29,15,-27),new Vector3(.55f,6,.7f),white);
            Box("Departure_Portal_Right",a,new Vector3(-15,15,-27),new Vector3(.55f,6,.7f),white);
            Box("Departure_Portal_Lintel",a,new Vector3(-22,18,-27),new Vector3(14.55f,.55f,.7f),white);
            Box("Portal_Red_Header",a,new Vector3(-22,17.55f,-27.39f),new Vector3(8,.20f,.04f),red,false);
            Box("Start_Block",a,new Vector3(-30,12.4f,-20),new Vector3(2,.8f,3.5f),white);
            Stripe(a,new Vector3(-22,12.025f,-26),new Vector3(-22,12.025f,-8.2f),.8f,red);
            Arrow(a,new Vector3(-22,12.04f,-12),0,white);Digit(a,1,new Vector3(-30,12.04f,-28),1.2f,grey);
            Rail(a,new Vector3(-33,12,-31),new Vector3(-33,12,-9));Rail(a,new Vector3(-11,12,-31),new Vector3(-11,12,-9));
            Rail(a,new Vector3(-33,12,-31),new Vector3(-11,12,-31));
            Ramp("Red_Rise_Ramp",b,new Vector3(-25,13,3),new Vector3(-16,15,3),4,red);
            Box("Red_Launch_Deck",b,new Vector3(-14.5f,14.8f,3),new Vector3(3,.4f,4),red);
            Arrow(b,new Vector3(-22,13.025f,-3),0,white);Digit(b,2,new Vector3(-28,13.03f,1),.9f,white);
            Rail(b,new Vector3(-30,13,-6),new Vector3(-30,13,6));Rail(b,new Vector3(-30,13,6),new Vector3(-20,13,6));
            Box("Planar_Atrium_Floor",c,new Vector3(1,15.035f,1),new Vector3(19,.055f,12),glass);
            Box("Atrium_Cyan_Canopy",c,new Vector3(1,24,12),new Vector3(20,.5f,3),cyan);
            for(int i=0;i<3;i++)Box("Atrium_Colonnade",c,new Vector3(-7+i*8,19.5f,12.4f),new Vector3(.5f,9,.6f),white);
            Box("Planar_Glass_Wall",c,new Vector3(11.35f,19.5f,2),new Vector3(.12f,9,17),glass);
            Box("Glass_Upper_Frame",c,new Vector3(11.35f,24,2),new Vector3(.24f,.24f,17.2f),white);
            Box("Glass_Lower_Frame",c,new Vector3(11.35f,15.15f,2),new Vector3(.24f,.24f,17.2f),white);
            Ramp("Atrium_Rise_Ramp",c,new Vector3(5,15,5),new Vector3(5,18,14),4,white);
            Box("Atrium_Launch_Deck",c,new Vector3(5,17.8f,15.5f),new Vector3(4,.4f,3),red);
            Stripe(c,new Vector3(-9,15.09f,3),new Vector3(2,15.09f,3),.7f,red);Arrow(c,new Vector3(-4,15.11f,3),90,white);
            Rail(c,new Vector3(-9.5f,15,-6.5f),new Vector3(10.5f,15,-6.5f));Rail(c,new Vector3(-9.5f,15,7),new Vector3(-9.5f,15,16));
            Digit(c,3,new Vector3(-6,15.09f,-3),1,white);
            Box("Ascent_Landing",d,new Vector3(5,18.025f,22),new Vector3(4,.05f,3.5f),red);
            Ramp("Ascent_Ramp",d,new Vector3(9,18,31),new Vector3(0,20,31),4,white);
            Box("Final_Launch_Deck",d,new Vector3(-1.4f,19.8f,31),new Vector3(2.8f,.4f,4),red);
            Box("Ascent_Wall",d,new Vector3(17,21,28),new Vector3(.5f,6,14),white);
            Box("Ascent_Cyan_Inset",d,new Vector3(16.72f,21,28),new Vector3(.035f,3.8f,10),cyan,false);
            Rail(d,new Vector3(0,18,35.5f),new Vector3(18,18,35.5f));Digit(d,4,new Vector3(12,18.07f,23),1,grey);
            Stripe(e,new Vector3(-6,20.025f,31),new Vector3(-12,20.025f,31),.8f,red);
            Stripe(e,new Vector3(-12,20.025f,31),new Vector3(-12,20.025f,44),.8f,red);
            Arrow(e,new Vector3(-12,20.05f,38),0,white);Digit(e,5,new Vector3(-17,20.06f,34),1,grey);
            Box("Finish_Portal_Left",e,new Vector3(-17,23,44),new Vector3(.55f,6,.65f),white);
            Box("Finish_Portal_Right",e,new Vector3(-7,23,44),new Vector3(.55f,6,.65f),white);
            Box("Finish_Portal_Red",e,new Vector3(-12,26,44),new Vector3(10.55f,.55f,.65f),red);
            Rail(e,new Vector3(-19.5f,20,30),new Vector3(-19.5f,20,48));Rail(e,new Vector3(-19.5f,20,48),new Vector3(-4.5f,20,48));
            var city=Group("06 — Skyline / Quiet architecture",City);
            var positions=new[]{new Vector3(-46,18,13),new Vector3(-45,26,37),new Vector3(-33,32,58),new Vector3(-8,24,67),new Vector3(18,34,63),new Vector3(34,25,40),new Vector3(35,30,12),new Vector3(28,20,-15),new Vector3(9,25,-33),new Vector3(-5,18,-42)};
            for(int i=0;i<positions.Length;i++){
                var v=positions[i];var size=new Vector3(i%2==0?12:9,v.y+12,i%3==0?13:10);v.y=(v.y-12)/2;
                var tower=Box("Silhouette_Tower_"+i,city,v,size,i%3==0?chalk:white);
                var edge=tower.transform.position+new Vector3(0,size.y*.23f,-size.z*.5f-.035f);
                Box("Broad_Glass_Band",city,edge,new Vector3(size.x*.84f,size.y*.16f,.06f),i%3==1?cyan:glass,false);
                Box("Tower_Crown",city,tower.transform.position+Vector3.up*(size.y/2+.15f),new Vector3(size.x+.4f,.3f,size.z+.4f),white,false);
            }
            Box("City_Underlay",city,new Vector3(0,-12.7f,10),new Vector3(145,1,145),chalk);
            var route=Group("07 — Route / Jump landing markers",City);
            var waypoints=new[]{new Vector3(-22,12,-24),new Vector3(-22,12,-8),new Vector3(-22,13,-6),new Vector3(-25,13,3),new Vector3(-16,15,3),new Vector3(-13.1f,15,3),new Vector3(-9.8f,15.065f,3),new Vector3(5,15,5),new Vector3(5,18,14),new Vector3(5,18,17),new Vector3(5,18,20.1f),new Vector3(9,18,31),new Vector3(0,20,31),new Vector3(-2.8f,20,31),new Vector3(-4.1f,20,31),new Vector3(-12,20,44)};
            for(int i=0;i<waypoints.Length;i++){var t=Group("Waypoint_"+i.ToString("00"),route);t.position=waypoints[i];}
            var rig=Group("08 — Presentation",City);
            var cam=new GameObject("Camera_Hero",typeof(Camera));cam.transform.SetParent(rig,false);cam.tag="MainCamera";
            cam.transform.position=new Vector3(-55,39,-55);cam.transform.LookAt(new Vector3(-5,13,10));
            var camera=cam.GetComponent<Camera>();camera.fieldOfView=49;camera.nearClipPlane=.1f;camera.farClipPlane=280;camera.allowHDR=true;camera.allowMSAA=true;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.57f,.77f,.85f);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var light=new GameObject("Sun_Daylight",typeof(Light));light.transform.SetParent(rig,false);light.transform.rotation=Quaternion.Euler(48,-32,0);
            var sun=light.GetComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.color=new Color(1,.97f,.92f);sun.shadows=LightShadows.Soft;
            RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.66f,.78f,.85f);RenderSettings.ambientEquatorColor=new Color(.53f,.62f,.67f);RenderSettings.ambientGroundColor=new Color(.4f,.47f,.51f);RenderSettings.ambientIntensity=1;RenderSettings.fog=false;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
            var view=SceneView.lastActiveSceneView;if(view)view.LookAt(new Vector3(-5,16,8),cam.transform.rotation,75);
            Selection.activeGameObject=City.gameObject;
        }
    }
}
