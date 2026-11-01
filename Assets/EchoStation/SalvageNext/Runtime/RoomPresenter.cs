using System;
using EchoStation.Rendering;
using UnityEngine;
namespace EchoStation.SalvageNext
{
    public sealed class RoomPresenter:IDisposable
    {
        readonly Camera view;readonly RoomLightingRegistry lighting;readonly RoomPackage[] rooms=new RoomPackage[3];bool disposed;
        public RoomPresenter(Camera camera){if(!camera)throw new ArgumentException("Player view required.");view=camera;lighting=new RoomLightingRegistry();}
        public int AtlasOffset=>lighting.Offset;
        public RoomPackage Room(int index)=>rooms[index];
        public void Show(int index,RoomPackage room)
        {
            if(disposed||!view||!room||room.Index!=index||rooms[index])throw new InvalidOperationException("Invalid room presentation lease.");
            rooms[index]=room;
            foreach(var mesh in room.GetComponentsInChildren<MergedRoomMesh>(true))mesh.SetExternalManaged(true);
            lighting.Register(room);
            var decor=room.GetComponent<InstancedRoomDecor>();if(decor)decor.BindView(view);
            var lamps=room.GetComponent<InstancedRoomLamps>();if(lamps)lamps.BindView(view);
            var shadow=room.GetComponent<DistanceShadowCaster>();if(shadow){var renderers=new Renderer[room.RendererCount];for(int i=0;i<renderers.Length;i++)renderers[i]=room.Renderer(i);shadow.Configure(view,renderers,30,2,.1f);}
            room.gameObject.SetActive(true);
        }
        public void Hide(int index,RoomPackage room)
        {
            if(disposed||!room||rooms[index]!=room)return;
            lighting.Unregister(room);room.gameObject.SetActive(false);
            var decor=room.GetComponent<InstancedRoomDecor>();if(decor)decor.BindView(null);
            var lamps=room.GetComponent<InstancedRoomLamps>();if(lamps)lamps.BindView(null);
            var shadow=room.GetComponent<DistanceShadowCaster>();if(shadow)shadow.Configure(null,null,30,2,.1f);
            for(int i=0;i<room.RendererCount;i++)if(room.Renderer(i))room.Renderer(i).lightmapIndex=-1;
            rooms[index]=null;
        }
        public void Dispose(){if(disposed)return;for(int i=0;i<3;i++)if(rooms[i])Hide(i,rooms[i]);lighting.Dispose();disposed=true;}
    }
}
