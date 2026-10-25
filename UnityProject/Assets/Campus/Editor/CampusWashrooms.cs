using System.Linq;
using UnityEngine;

namespace Campus.Editor
{
    public static class CampusWashrooms
    {
        public static void Apply(Transform building)
        {
            var renderers=building.GetComponentsInChildren<Renderer>(true);
            var frame=renderers.First(r=>r.name.EndsWith("_Sign_Plate")).sharedMaterial;
            var doorMaterial=renderers.First(r=>r.name=="OFFICE_Original_F1_CorridorDoor").sharedMaterial;
            foreach(string side in new[]{"Original","Duplicate"})for(int floor=0;floor<4;floor++)
            {
                bool original=side=="Original";
                string suffix=(original?"":"_duplicate_opposite_side")+(floor==0?"":"_floor"+(floor+1));
                var wall=renderers.Single(r=>r.name=="MEASURED_washroom_left_wall_common_gate_at_divider"+suffix);
                var divider=renderers.Single(r=>r.name=="MEASURED_washroom_two_section_divider_between_gates"+suffix);
                var bounds=wall.bounds;var partition=divider.bounds;var material=wall.sharedMaterial;
                wall.gameObject.SetActive(false);divider.gameObject.SetActive(false);
                var group=new GameObject("Washrooms_"+side+"_F"+floor).transform;group.SetParent(building,false);
                // The old gallery railing runs through the washroom facade; preserve only its outside spans.
                var gallery=renderers.Single(r=>r.name=="MEASURED_gallery_outer_boundary_wall"+suffix);
                var rail=gallery.bounds;gallery.gameObject.SetActive(false);
                Box(group,"GalleryBoundary_Before",new Vector3(rail.center.x,rail.center.y,(rail.min.z+bounds.min.z)*.5f),
                    new Vector3(rail.size.x,rail.size.y,bounds.min.z-rail.min.z),gallery.sharedMaterial);
                Box(group,"GalleryBoundary_After",new Vector3(rail.center.x,rail.center.y,(bounds.max.z+rail.max.z)*.5f),
                    new Vector3(rail.size.x,rail.size.y,rail.max.z-bounds.max.z),gallery.sharedMaterial);
                float x=bounds.center.x,y=floor*3.3f;
                float[] openings={5.375f,10.125f};
                float start=bounds.min.z;
                for(int i=0;i<2;i++)
                {
                    float z=openings[i];
                    Box(group,"FrontPier_"+i,new Vector3(x,y+1.65f,(start+z-.5f)*.5f),new Vector3(.09f,3.3f,z-.5f-start),material);
                    Box(group,"DoorHeader_"+i,new Vector3(x,y+2.75f,z),new Vector3(.09f,1.1f,1),material);
                    start=z+.5f;
                    bool female=original?i==0:i==1;
                    string id=(original?"A":"B")+"-F"+floor+"-WC-"+(female?"F":"M");
                    Box(group,"WashroomDoor_"+id,new Vector3(x,y+1.07f,z),new Vector3(.05f,2.1f,.9f),doorMaterial);
                    foreach(float dz in new[]{-.475f,.475f})Box(group,"DoorJamb_"+id+"_"+dz,new Vector3(x,y+1.1f,z+dz),new Vector3(.14f,2.2f,.05f),frame);
                    Box(group,"DoorFrameTop_"+id,new Vector3(x,y+2.175f,z),new Vector3(.14f,.05f,1),frame);
                    float outward=original?1:-1;
                    Box(group,"DoorHandle_"+id,new Vector3(x+outward*.065f,y+1.05f,z+outward*.34f),new Vector3(.08f,.12f,.035f),frame);
                    Box(group,"WashroomNameplate_"+id,new Vector3(x+outward*.065f,y+2.46f,z),new Vector3(.024f,.28f,.6f),frame,false);
                }
                Box(group,"FrontPier_End",new Vector3(x,y+1.65f,(start+bounds.max.z)*.5f),new Vector3(.09f,3.3f,bounds.max.z-start),material);
                Box(group,"PrivacyDivider",partition.center,partition.size,material);
            }
            foreach(var label in renderers.Where(r=>r.name.StartsWith("MEASURED_LABEL_Washroom")))label.gameObject.SetActive(false);
        }

        private static void Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool solid=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,true);
            go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());
        }
    }
}
