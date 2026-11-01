using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EchoStation.InfiltrationLevel
{
    public static class LevelLayout
    {
        public const int Columns=9,Rows=8;
        public const float Width=36,Depth=32,MinimumNetArea=864;
        public static Vector3 Cell(int x,int z)=>new Vector3(-16+4*x,0,-14+4*z);
        public static int Zone(int x,int z)
        {
            if(z<2)return 0;if(z==7)return 4;
            if(z>=5)return x>=3&&x<=5?5:4;
            return x<3?2:x>5?3:1;
        }
        public static readonly string[] Names={"01 接待大厅","02 中央中庭","03 行政办公","04 物流仓区","05 检修侧廊","06 数据核心"};
        public static bool Opening(bool vertical,int boundary,int along)
        {
            if(vertical)return (boundary==3||boundary==6)&&(along==3||along==6);
            return (boundary==2||boundary==5)&&(along==1||along==4||along==7);
        }
        public static readonly Vector3 Start=new Vector3(0,.06f,-14);
        public static Vector3 V(float x,float z)=>new Vector3(x,.04f,z);
        public static readonly Vector3[][] Routes={
            new[]{V(0,-14),V(0,-6),V(0,-2),V(0,2),V(0,6),V(0,9)},
            new[]{V(0,-14),V(-12,-12),V(-12,-6),V(-12,-2),V(-12,6),V(-12,10),V(-4,10),V(0,9)},
            new[]{V(0,-14),V(12,-12),V(12,-6),V(12,-2),V(12,6),V(12,10),V(4,10),V(0,9)}};
        public static readonly Vector3[][] Loops={
            new[]{V(0,-12),V(-12,-12),V(-12,-6),V(-12,-2),V(-4,-2),V(0,-2),V(0,-6),V(0,-12)},
            new[]{V(-12,-2),V(-12,6),V(-12,14),V(12,14),V(12,6),V(12,-2),V(0,-2),V(-12,-2)}};
        public static float UnionArea(IEnumerable<Rect> values)
        {
            var rectangles=values.Where(r=>r.width>0&&r.height>0).ToArray();
            var xs=rectangles.SelectMany(r=>new[]{r.xMin,r.xMax}).Distinct().OrderBy(x=>x).ToArray();float area=0;
            for(int i=1;i<xs.Length;i++)
            {
                float mid=(xs[i-1]+xs[i])*.5f;
                var intervals=rectangles.Where(r=>r.xMin<mid&&r.xMax>mid).OrderBy(r=>r.yMin).ToArray();
                float covered=0,lo=0,hi=0;bool first=true;
                foreach(var r in intervals){if(first){lo=r.yMin;hi=r.yMax;first=false;}else if(r.yMin<=hi)hi=Mathf.Max(hi,r.yMax);else{covered+=hi-lo;lo=r.yMin;hi=r.yMax;}}
                if(!first)covered+=hi-lo;area+=(xs[i]-xs[i-1])*covered;
            }
            return area;
        }
        public static float ConservativeNetArea(Transform root)
        {
            var clip=new Rect(-18,-16,36,32);var rectangles=new List<Rect>();
            foreach(var marker in root.GetComponentsInChildren<LevelSolid>())
            foreach(var c in marker.GetComponentsInChildren<Collider>())
            {
                if(!c.enabled||c.isTrigger)continue;var b=c.bounds;if(b.min.y>1.9f||b.max.y<.1f)continue;
                float x0=Mathf.Max(clip.xMin,b.min.x),x1=Mathf.Min(clip.xMax,b.max.x),z0=Mathf.Max(clip.yMin,b.min.z),z1=Mathf.Min(clip.yMax,b.max.z);
                if(x1>x0&&z1>z0)rectangles.Add(Rect.MinMaxRect(x0,z0,x1,z1));
            }
            return Width*Depth-UnionArea(rectangles);
        }
    }
}
