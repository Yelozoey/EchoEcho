using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext
{
    /// <summary>One world's stable atlas slots, independent of Unity's additive scene remapping.</summary>
    public sealed class RoomLightingRegistry:IDisposable
    {
        static RoomLightingRegistry active;
        readonly LightmapData[] previous,maps;readonly LightmapsMode previousMode;
        readonly RoomPackage[] rooms=new RoomPackage[3];bool disposed;
        public int Offset=>previous.Length;
        public RoomLightingRegistry()
        {
            RoomScenePump.RequireMainThread();if(active!=null)throw new InvalidOperationException("A room lightmap world already owns the global slots.");
            previous=LightmapSettings.lightmaps;previousMode=LightmapSettings.lightmapsMode;maps=new LightmapData[previous.Length+6];active=this;
            SceneManager.sceneLoaded+=Loaded;SceneManager.sceneUnloaded+=Unloaded;Apply();
        }
        void Loaded(Scene scene,LoadSceneMode mode)=>Apply();void Unloaded(Scene scene)=>Apply();
        public void Register(RoomPackage room)
        {
            RoomScenePump.RequireMainThread();if(disposed||!room||room.MapCount!=2)throw new InvalidOperationException("Two directional maps and a live registry required.");
            int index=room.Index;if(index<0||index>2||(rooms[index]&&rooms[index]!=room))throw new InvalidOperationException("Room slot already owned.");
            rooms[index]=room;Apply();
        }
        public void Unregister(RoomPackage room)
        {
            RoomScenePump.RequireMainThread();if(disposed||!room)return;int index=room.Index;if(index>=0&&index<3&&rooms[index]==room){rooms[index]=null;Apply();}
        }
        void Apply()
        {
            if(disposed||active!=this)return;Array.Copy(previous,maps,previous.Length);
            for(int index=0;index<3;index++)for(int atlas=0;atlas<2;atlas++){var room=rooms[index];maps[Offset+index*2+atlas]=room?new LightmapData{lightmapColor=room.ColorMap(atlas),lightmapDir=room.DirectionMap(atlas)}:new LightmapData();}
            LightmapSettings.lightmapsMode=LightmapsMode.CombinedDirectional;LightmapSettings.lightmaps=maps;
            foreach(var room in rooms)if(room)for(int r=0;r<room.RendererCount;r++){var renderer=room.Renderer(r);if(renderer){renderer.lightmapIndex=Offset+room.Index*2+room.Atlas(r);renderer.lightmapScaleOffset=new Vector4(1,1,0,0);}}
        }
        public void Dispose()
        {
            RoomScenePump.RequireMainThread();if(disposed)return;disposed=true;SceneManager.sceneLoaded-=Loaded;SceneManager.sceneUnloaded-=Unloaded;
            if(active==this){active=null;LightmapSettings.lightmaps=previous;LightmapSettings.lightmapsMode=previousMode;}Array.Clear(rooms,0,rooms.Length);Array.Clear(maps,0,maps.Length);
        }
    }
}
