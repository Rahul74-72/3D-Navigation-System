using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Campus.Editor
{
    public static class CampusOfficeRoute
    {
        public static void Apply(Transform building, NavMeshData navigation)
        {
            foreach(string side in new[]{"Original","Duplicate"})
            {
                var hinge=building.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="OFFICE_"+side+"_F1_CorridorDoor_Hinge");
                hinge.localRotation=Quaternion.identity;
                var door=hinge.GetComponentsInChildren<MeshFilter>().Single(m=>m.name.EndsWith("_CorridorDoor"));
                if(door.GetComponent<MeshCollider>()==null)door.gameObject.AddComponent<MeshCollider>().sharedMesh=door.sharedMesh;
            }
            Physics.SyncTransforms();
            var sources=new List<NavMeshBuildSource>();
            // The imported collision set omits corridor slabs and stair treads. Bake actual meshes.
            foreach(var filter in building.GetComponentsInChildren<MeshFilter>())
            {
                string name=filter.name.ToUpperInvariant();
                if(filter.sharedMesh==null || name.StartsWith("NAV") || name.StartsWith("ANIM") || name.Contains("LABEL") || name.Contains("QR_") || name.Contains("_SIGN"))continue;
                sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=filter.sharedMesh,transform=filter.transform.localToWorldMatrix,area=0});
            }
            Debug.Log("OFFICE_BAKE_SOURCES "+sources.Count);
            var settings=NavMesh.GetSettingsByIndex(0);
            settings.agentRadius=.18f;settings.agentHeight=1.65f;settings.agentSlope=45;settings.agentClimb=.2f;
            settings.minRegionArea=.1f;settings.overrideVoxelSize=true;settings.voxelSize=.04f;
            if(!NavMeshBuilder.UpdateNavMeshData(navigation,settings,sources,new Bounds(new Vector3(3.7f,6,9),new Vector3(90,30,80))))
                throw new System.InvalidOperationException("Office route NavMesh rebuild failed.");
            EditorUtility.SetDirty(navigation);
        }

    }
}
