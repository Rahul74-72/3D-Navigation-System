using System;
using System.Linq;
using Campus.Navigation;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Campus.Editor
{
    public static class CampusNameplates
    {
        public static string Label(Room room)
        {
            if(room.IsWashroom)return room.name.Replace(" ","\n");
            bool provisional=room.building!="A" || (room.floor>1 && !(room.floor==2 && room.id.EndsWith("-OFFICE")));
            return room.name+(provisional?"\n<size=65%>Provisional</size>":"");
        }

        public static void Apply(Transform building,CampusData data)
        {
            const string fontPath="Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if(font==null)throw new InvalidOperationException("Nameplate font missing. Restore the bundled TextMesh Pro font resources.");
            var renderers=building.GetComponentsInChildren<Renderer>(true);
            foreach(var old in renderers.Where(r=>r.name.StartsWith("ROOM_")&&r.name.EndsWith("_Sign_GLB")))old.gameObject.SetActive(false);
            var plateMaterial=renderers.First(r=>r.name.EndsWith("_Sign_Plate")).sharedMaterial;
            foreach(var room in data.rooms)
            {
                string side=room.building=="A"?"Original":"Duplicate";
                string prefix="ROOM_"+side+"_F"+(room.floor+1)+"_C";
                if(room.IsWashroom)
                {
                    var plate=renderers.Single(r=>r.name=="WashroomNameplate_"+room.id);
                    Vector3 outward=room.building=="A"?Vector3.right:Vector3.left;
                    AddText(building,plate.bounds.center+outward*(plate.bounds.extents.x+.004f),outward,room,1,font);
                }
                else if(!room.id.EndsWith("-OFFICE"))
                {
                    string number=room.id.Substring(room.id.LastIndexOf('R')+1);
                    for(int door=1;door<=2;door++)
                    {
                        var plate=renderers.Single(r=>r.name==prefix+number+"_D"+door+"_Sign_Plate");
                        Vector3 outward=room.building=="A"?Vector3.right:Vector3.left;
                        AddText(plate.transform,plate.bounds.center+outward*(plate.bounds.extents.x+.004f),outward,room,door,font);
                    }
                }
                else
                {
                    var jamb=renderers.Single(r=>r.name=="OFFICE_"+side+"_F"+(room.floor+1)+"_Entry_Jamb2");
                    var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plate.name="OfficeNameplate_"+room.id;plate.transform.SetParent(building,true);
                    plate.transform.position=new Vector3(room.building=="A"?-3.48f:10.88f,room.floor*3.3f+1.55f,jamb.bounds.max.z+.01f);
                    plate.transform.localScale=new Vector3(.6f,.28f,.024f);
                    UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>());
                    plate.GetComponent<Renderer>().sharedMaterial=plateMaterial;
                    AddText(building,plate.transform.position+Vector3.forward*.017f,Vector3.forward,room,1,font);
                }
            }
        }

        private static void AddText(Transform parent,Vector3 position,Vector3 outward,Room room,int door,TMP_FontAsset font)
        {
            var go=new GameObject("RoomName_"+room.id+"_D"+door,typeof(RectTransform));
            go.transform.SetParent(parent,true);go.transform.SetPositionAndRotation(position,Quaternion.LookRotation(-outward,Vector3.up));
            var text=go.AddComponent<TextMeshPro>();text.font=font;text.text=Label(room);
            text.rectTransform.sizeDelta=new Vector2(.54f,.24f);text.alignment=TextAlignmentOptions.Center;
            text.color=Color.white;text.fontStyle=FontStyles.Bold;text.enableAutoSizing=true;text.fontSizeMin=.25f;text.fontSizeMax=1.6f;
            text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Overflow;
            text.ForceMeshUpdate();
        }
    }
}
