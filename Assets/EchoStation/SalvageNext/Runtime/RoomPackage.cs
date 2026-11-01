using System;
using UnityEngine;
namespace EchoStation.SalvageNext
{
    public sealed class RoomPackage:MonoBehaviour
    {
        [SerializeField]int index;
        [SerializeField]MeshRenderer[] renderers=Array.Empty<MeshRenderer>();
        [SerializeField]int[] localAtlases=Array.Empty<int>();
        [SerializeField]Texture2D[] colors=Array.Empty<Texture2D>(),directions=Array.Empty<Texture2D>();
        public int Index=>index;public int RendererCount=>renderers.Length;public int MapCount=>colors.Length;
        public MeshRenderer Renderer(int at)=>renderers[at];public int Atlas(int at)=>localAtlases[at];
        public Texture2D ColorMap(int at)=>colors[at];public Texture2D DirectionMap(int at)=>directions[at];
        public void Configure(int value,MeshRenderer[] meshes,int[] atlas,Texture2D[] color,Texture2D[] direction)
        {
            if(value<0||value>2||meshes==null||atlas==null||meshes.Length!=atlas.Length||color==null||direction==null||color.Length!=direction.Length||Array.Exists(atlas,i=>i<0||i>=color.Length)||Array.Exists(meshes,m=>!m)||Array.Exists(color,t=>!t)||Array.Exists(direction,t=>!t))throw new ArgumentException("Invalid room lightmaps.");index=value;renderers=(MeshRenderer[])meshes.Clone();localAtlases=(int[])atlas.Clone();colors=(Texture2D[])color.Clone();directions=(Texture2D[])direction.Clone();
        }
    }
}
