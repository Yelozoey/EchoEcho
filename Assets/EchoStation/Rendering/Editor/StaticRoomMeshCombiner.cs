using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace EchoStation.Rendering.Editor
{
    /// <summary>Offline explicit room/material group. Never merges interactive objects automatically.</summary>
    public static class StaticRoomMeshCombiner
    {
        public static MeshRenderer Combine(Transform room, MeshRenderer[] sources, string meshAssetPath=null)
        {
            if(!room || sources==null || sources.Length<2 || sources.Any(r=>!r) || sources.Distinct().Count()!=sources.Length)
                throw new ArgumentException("Require one room and at least two distinct renderers.");
            if(meshAssetPath!=null && (!meshAssetPath.StartsWith("Assets/EchoStation/",StringComparison.Ordinal) ||
                meshAssetPath.Contains("..") || meshAssetPath.Contains('\\') || !meshAssetPath.EndsWith(".asset",StringComparison.Ordinal) ||
                System.IO.File.Exists(meshAssetPath) || AssetDatabase.LoadMainAssetAtPath(meshAssetPath)))
                throw new ArgumentException("Mesh destination must be a new EchoStation .asset path.");
            var first=sources[0]; var baked=first.lightmapIndex>=0 && first.lightmapIndex<0xfffe;
            foreach(var renderer in sources)
            {
                var filter=renderer.GetComponent<MeshFilter>(); var matrix=room.worldToLocalMatrix*renderer.localToWorldMatrix;
                if(!renderer.transform.IsChildOf(room) || renderer.transform==room || !renderer.enabled ||
                    !GameObjectUtility.AreStaticEditorFlagsSet(renderer.gameObject,StaticEditorFlags.OccludeeStatic) ||
                    !filter || !filter.sharedMesh || !filter.sharedMesh.isReadable || filter.sharedMesh.subMeshCount!=1 ||
                    renderer.sharedMaterials.Length!=1 || !renderer.sharedMaterial || renderer.sharedMaterial!=first.sharedMaterial ||
                    renderer.lightmapIndex!=first.lightmapIndex || renderer.gameObject.layer!=first.gameObject.layer ||
                    renderer.receiveGI!=first.receiveGI || renderer.probeAnchor!=first.probeAnchor ||
                    renderer.renderingLayerMask!=first.renderingLayerMask || renderer.shadowCastingMode!=first.shadowCastingMode ||
                    renderer.receiveShadows!=first.receiveShadows || renderer.lightProbeUsage!=first.lightProbeUsage ||
                    renderer.reflectionProbeUsage!=first.reflectionProbeUsage || renderer.GetComponentInParent<LODGroup>() ||
                    renderer.HasPropertyBlock() || renderer.additionalVertexStreams || matrix.determinant<=.000001f ||
                    (baked && filter.sharedMesh.uv2.Length!=filter.sharedMesh.vertexCount))
                    throw new ArgumentException($"Incompatible static room/material group: {renderer.name}");
            }
            var mesh=new Mesh { name=room.name+"_Combined",indexFormat=sources.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.vertexCount)>65535 ? IndexFormat.UInt32:IndexFormat.UInt16 };
            GameObject output=null;
            try
            {
                var inputs=sources.Select(r=>new CombineInstance {mesh=r.GetComponent<MeshFilter>().sharedMesh,
                    transform=room.worldToLocalMatrix*r.localToWorldMatrix,lightmapScaleOffset=r.lightmapScaleOffset}).ToArray();
                mesh.CombineMeshes(inputs,true,true,baked); mesh.RecalculateBounds();
                if(meshAssetPath!=null)
                {
                    ForwardHdrBaselineSetup.EnsureFolder(System.IO.Path.GetDirectoryName(meshAssetPath).Replace('\\','/'));
                    AssetDatabase.CreateAsset(mesh,meshAssetPath);
                }
                output=new GameObject(room.name+"_Combined",typeof(MeshFilter),typeof(MeshRenderer));
                output.transform.SetParent(room,false); output.layer=first.gameObject.layer;
                output.GetComponent<MeshFilter>().sharedMesh=mesh; var result=output.GetComponent<MeshRenderer>();
                result.sharedMaterial=first.sharedMaterial; result.lightmapIndex=first.lightmapIndex;
                result.lightmapScaleOffset=new Vector4(1,1,0,0); result.shadowCastingMode=first.shadowCastingMode;
                result.receiveShadows=first.receiveShadows; result.renderingLayerMask=first.renderingLayerMask;
                result.lightProbeUsage=first.lightProbeUsage; result.reflectionProbeUsage=first.reflectionProbeUsage;
                result.receiveGI=first.receiveGI;
                GameObjectUtility.SetStaticEditorFlags(output,GameObjectUtility.GetStaticEditorFlags(first.gameObject)&~StaticEditorFlags.BatchingStatic);
                foreach(var source in sources) source.enabled=false;
                return result;
            }
            catch
            {
                if(output) Object.DestroyImmediate(output);
                if(mesh && !AssetDatabase.Contains(mesh)) Object.DestroyImmediate(mesh);
                throw;
            }
        }
    }
}
