using System.Collections.Generic;
using UnityEngine;

// A clothed, rounded character built around the existing animated joint transforms.
public sealed class CampusManAppearance : MonoBehaviour
{
    private bool built;
    private readonly List<Object> ownedAssets = new List<Object>();
    private readonly List<Transform> knees = new List<Transform>();
    private readonly List<Transform> elbows = new List<Transform>();
    private readonly List<Transform> legRoots = new List<Transform>();
    private readonly List<Transform> armRoots = new List<Transform>();

    public void Build()
    {
        if (built) return;
        built = true;
        var shirt = Material("Teal cotton shirt", new Color(0.08f, 0.38f, 0.4f));
        var jeans = Material("Charcoal trousers", new Color(0.13f, 0.17f, 0.23f));
        var skin = Material("Warm skin", new Color(0.55f, 0.32f, 0.20f));
        var hair = Material("Dark hair", new Color(0.045f, 0.03f, 0.025f));
        var shoes = Material("Leather sneakers", new Color(0.055f, 0.06f, 0.07f));
        var white = Material("Eyes and sneaker soles", new Color(0.85f, 0.86f, 0.83f));
        var detail = Material("Shirt buttons", new Color(0.5f, 0.63f, 0.6f));
        foreach (Transform part in transform)
        {
            var renderer = part.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
        }
        var body = transform.Find("ANIM_Companion_body");
        Shape("Tailored shirt", body, Vector3.zero,
            new[] { -0.32f, -0.25f, 0.02f, 0.22f, 0.29f, 0.32f },
            new[] { 0.14f, 0.15f, 0.18f, 0.235f, 0.19f, 0.09f },
            new[] { 0.105f, 0.115f, 0.13f, 0.125f, 0.105f, 0.075f }, shirt);
        Ellipsoid("Trouser hips", body, new Vector3(0,-0.34f,0), new Vector3(0.31f,0.2f,0.22f), jeans);
        Ellipsoid("Neck", body, new Vector3(0,0.355f,0), new Vector3(0.13f,0.18f,0.13f), skin);
        for (int i=0;i<4;i++)
            Ellipsoid("Button "+i, body, new Vector3(0,0.2f-i*0.11f,0.13f), Vector3.one*0.014f, detail);
        for (int s=-1;s<=1;s+=2)
            Ellipsoid("Collar", body, new Vector3(s*0.065f,0.285f,0.09f), new Vector3(0.09f,0.09f,0.035f), shirt);

        var head = transform.Find("ANIM_Companion_head");
        Shape("Face and jaw", head, Vector3.zero,
            new[] {-0.19f,-0.14f,-0.02f,0.12f,0.20f},
            new[] {0.055f,0.095f,0.125f,0.13f,0.075f},
            new[] {0.065f,0.095f,0.11f,0.11f,0.065f},skin);
        Ellipsoid("Hair",head,new Vector3(0,0.14f,-0.018f),new Vector3(0.27f,0.17f,0.24f),hair);
        Ellipsoid("Nose",head,new Vector3(0,-0.005f,0.12f),new Vector3(0.042f,0.07f,0.06f),skin);
        Ellipsoid("Mouth",head,new Vector3(0,-0.105f,0.095f),new Vector3(0.065f,0.012f,0.015f),hair);
        for(int s=-1;s<=1;s+=2)
        {
            Ellipsoid("Ear",head,new Vector3(s*0.13f,-0.015f,0),new Vector3(0.04f,0.085f,0.048f),skin);
            Ellipsoid("Eye",head,new Vector3(s*0.049f,0.044f,0.101f),new Vector3(0.043f,0.025f,0.022f),white);
            Ellipsoid("Pupil",head,new Vector3(s*0.049f,0.044f,0.113f),new Vector3(0.016f,0.018f,0.008f),hair);
            Ellipsoid("Eyebrow",head,new Vector3(s*0.049f,0.075f,0.1f),new Vector3(0.054f,0.012f,0.018f),hair);
        }
        foreach(var side in new[]{"L","R"})
        {
            var arm=transform.Find("ANIM_Companion_arm_"+side);
            armRoots.Add(arm);
            Shape("Shirt sleeve",arm,Vector3.zero,new[]{0.06f,0.25f,0.32f},new[]{0.07f,0.08f,0.045f},new[]{0.07f,0.08f,0.045f},shirt);
            var elbow=new GameObject("Elbow").transform;
            elbow.SetParent(arm,false); elbow.localPosition=new Vector3(0,0.03f,0);
            elbows.Add(elbow);
            Ellipsoid("Elbow joint",elbow,Vector3.zero,Vector3.one*0.105f,skin);
            Shape("Forearm",elbow,Vector3.zero,new[]{-0.25f,-0.15f,0.015f},new[]{0.035f,0.046f,0.053f},new[]{0.035f,0.046f,0.053f},skin);
            Ellipsoid("Hand",elbow,new Vector3(0,-0.28f,0),new Vector3(0.08f,0.115f,0.055f),skin);
            Ellipsoid("Thumb",elbow,new Vector3(0.045f,-0.265f,0.01f),new Vector3(0.032f,0.065f,0.035f),skin);
            var leg=transform.Find("ANIM_Companion_leg_"+side);
            legRoots.Add(leg);
            Shape("Trouser thigh",leg,Vector3.zero,new[]{0.015f,0.18f,0.34f},new[]{0.067f,0.085f,0.09f},new[]{0.065f,0.08f,0.085f},jeans);
            var knee=new GameObject("Knee").transform;
            knee.SetParent(leg,false);knee.localPosition=new Vector3(0,0.015f,0);
            knees.Add(knee);
            Ellipsoid("Knee joint",knee,Vector3.zero,Vector3.one*0.135f,jeans);
            Shape("Trouser shin",knee,Vector3.zero,new[]{-0.29f,-0.15f,0.02f},new[]{0.05f,0.06f,0.068f},new[]{0.05f,0.06f,0.066f},jeans);
            Ellipsoid("Shoe",knee,new Vector3(0,-0.30f,0.035f),new Vector3(0.135f,0.105f,0.24f),shoes);
            Ellipsoid("Sole",knee,new Vector3(0,-0.338f,0.035f),new Vector3(0.14f,0.03f,0.245f),white);
        }
    }

    private void LateUpdate()
    {
        for(int i=0;i<knees.Count;i++)
        {
            float angle=Mathf.DeltaAngle(0,legRoots[i].localEulerAngles.x);
            knees[i].localRotation=Quaternion.Euler(-Mathf.Max(0,angle)*1.1f,0,0);
            elbows[i].localRotation=Quaternion.Euler(-12f-Mathf.Abs(Mathf.DeltaAngle(0,armRoots[i].localEulerAngles.x))*0.3f,0,0);
        }
    }

    private Material Material(string name, Color color)
    {
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name=name;material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",0.18f);
        ownedAssets.Add(material);return material;
    }

    private void Ellipsoid(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;
        obj.GetComponent<Renderer>().sharedMaterial=material;
        Destroy(obj.GetComponent<Collider>());
    }

    private void Shape(string name, Transform parent, Vector3 position, float[] heights, float[] widths, float[] depths, Material material)
    {
        const int sides=24;
        var vertices=new List<Vector3>();
        var triangles=new List<int>();
        for(int ring=0;ring<heights.Length;ring++)
            for(int j=0;j<sides;j++)
            {
                float a=j*Mathf.PI*2f/sides;
                vertices.Add(new Vector3(Mathf.Cos(a)*widths[ring],heights[ring],Mathf.Sin(a)*depths[ring]));
            }
        for(int r=0;r<heights.Length-1;r++)
            for(int j=0;j<sides;j++)
            {
                int a=r*sides+j,b=r*sides+(j+1)%sides,c=a+sides,d=b+sides;
                triangles.AddRange(new[]{a,c,b,b,c,d});
            }
        for(int end=0;end<2;end++)
        {
            int ring=end==0?0:heights.Length-1,center=vertices.Count;
            vertices.Add(new Vector3(0,heights[ring],0));
            for(int j=0;j<sides;j++)
            {
                int a=ring*sides+j,b=ring*sides+(j+1)%sides;
                triangles.AddRange(end==0?new[]{center,a,b}:new[]{center,b,a});
            }
        }
        var mesh=new Mesh {name=name};
        mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();ownedAssets.Add(mesh);
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
        obj.transform.SetParent(parent,false);obj.transform.localPosition=position;
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;
    }

    private void OnDestroy()
    {
        foreach(var asset in ownedAssets) if(asset!=null) Destroy(asset);
    }
}
