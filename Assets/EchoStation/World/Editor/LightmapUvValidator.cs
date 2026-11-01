using System;
using UnityEngine;

namespace EchoStation.World.Editor
{
    public static class LightmapUvValidator
    {
        public static bool IsValidNonOverlapping(Mesh mesh,out string error)
        {
            error=null;var uv=mesh.uv2;var indices=mesh.triangles;
            if(uv.Length!=mesh.vertexCount){error="Missing UV2 vertices.";return false;}
            foreach(var value in uv)if(!float.IsFinite(value.x)||!float.IsFinite(value.y)||value.x<-.0001f||value.y<-.0001f||value.x>1.0001f||value.y>1.0001f)
            {error="UV2 outside finite unit atlas.";return false;}
            for(var i=0;i<indices.Length;i+=3)
            {
                var a=new[]{uv[indices[i]],uv[indices[i+1]],uv[indices[i+2]]};
                if(Mathf.Abs(Cross(a[1]-a[0],a[2]-a[0]))<1e-12f){error="Degenerate UV2 triangle.";return false;}
                for(var j=i+3;j<indices.Length;j+=3)
                {
                    var b=new[]{uv[indices[j]],uv[indices[j+1]],uv[indices[j+2]]};
                    if(Overlap(a,b)){error=$"UV2 triangle interiors overlap: {i/3}/{j/3}";return false;}
                }
            }
            return true;
        }
        private static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        private static bool Overlap(Vector2[] a,Vector2[] b)
        {
            foreach(var triangle in new[]{a,b})for(var edge=0;edge<3;edge++)
            {
                var delta=triangle[(edge+1)%3]-triangle[edge];var axis=new Vector2(-delta.y,delta.x).normalized;
                var aMin=float.PositiveInfinity;var aMax=float.NegativeInfinity;var bMin=aMin;var bMax=aMax;
                for(var i=0;i<3;i++){var av=Vector2.Dot(axis,a[i]);var bv=Vector2.Dot(axis,b[i]);aMin=Math.Min(aMin,av);aMax=Math.Max(aMax,av);bMin=Math.Min(bMin,bv);bMax=Math.Max(bMax,bv);}
                if(aMax<=bMin+1e-7f||bMax<=aMin+1e-7f)return false;
            }
            return true;
        }
    }
}
