using System;
using UnityEngine;
using UnityEngine.AI;

namespace Campus.Navigation
{
    public enum NavigationMode { Browse, Preview, Checkpoint, Live }

    public sealed class CampusApp : MonoBehaviour
    {
        public TextAsset catalogue;
        public NavMeshData navigation;
        public Transform building;
        public Camera mapCamera;
        public CampusTracking tracking;
        public Material routeMaterial, positionMaterial;
        public CampusData Data { get; private set; }
        public string Status { get; set; } = "Model coordinates are provisional.";
        public Room Destination { get; private set; }
        public NavigationMode Mode { get; private set; }
        public Vector3 UserPosition { get; private set; }
        public float Remaining { get; private set; }
        public int Floor { get; private set; }
        public bool ARView { get; private set; }
        public bool Arrived { get; private set; }
        public bool FollowPerson { get; private set; } = true;
        public Transform Person => guide;
        public bool HasRoute => route != null && route.corners.Length > 1;
        private NavMeshDataInstance meshInstance;
        private NavMeshPath route;
        private LineRenderer mapLine, arLine;
        private Transform marker, guide, arGuide;
        private NavMeshAgent agent;
        private Renderer[] modelRenderers;
        private float nextRouteUpdate, phase;
        private Vector3 previousGuide;
        private bool followFloor;
        private Transform[] limbs;
        private readonly Transform[] arrows=new Transform[8];
        private Mesh arrowMesh;
        private CampusFollowCamera followCamera;

        private void Awake()
        {
            try { Data = catalogue == null ? null : JsonUtility.FromJson<CampusData>(catalogue.text); }
            catch(Exception e) { Status="Campus data could not be loaded: "+e.Message;enabled=false;return; }
            string error = Data==null?"Campus catalogue missing.":Data.Validate();
            if (error != null || navigation == null) { Status = error ?? "Navigation asset missing."; enabled = false; return; }
            meshInstance = NavMesh.AddNavMeshData(navigation);
            UserPosition = Data.checkpoints[0].position;
            modelRenderers = building.GetComponentsInChildren<Renderer>(true);
            marker = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            marker.name = "User position"; marker.localScale = new Vector3(.36f,.18f,.36f);
            Destroy(marker.GetComponent<Collider>()); marker.GetComponent<Renderer>().sharedMaterial = positionMaterial;
            mapLine = MakeLine("Map route", 0); arLine = MakeLine("AR route", 30);
            arrowMesh=new Mesh{name="Navigation arrow"};
            arrowMesh.vertices=new[]{new Vector3(-.11f,0,-.24f),new Vector3(.11f,0,-.24f),new Vector3(.11f,0,.04f),new Vector3(.27f,0,.04f),new Vector3(0,0,.36f),new Vector3(-.27f,0,.04f),new Vector3(-.11f,0,.04f)};
            arrowMesh.triangles=new[]{0,2,1,0,6,2,5,4,3,0,1,2,0,2,6,5,3,4};arrowMesh.RecalculateNormals();arrowMesh.RecalculateBounds();
            for(int i=0;i<arrows.Length;i++){var go=new GameObject("AR arrow "+i,typeof(MeshFilter),typeof(MeshRenderer));go.layer=30;go.transform.SetParent(transform);go.GetComponent<MeshFilter>().sharedMesh=arrowMesh;go.GetComponent<MeshRenderer>().sharedMaterial=routeMaterial;arrows[i]=go.transform;go.SetActive(false);}
            guide = MakeGuide("Preview guide", 0); arGuide = MakeGuide("AR guide", 30);
            limbs = new[] {guide.Find("ANIM_Companion_arm_L"), guide.Find("ANIM_Companion_arm_R"), guide.Find("ANIM_Companion_leg_L"), guide.Find("ANIM_Companion_leg_R")};
            agent = guide.gameObject.AddComponent<NavMeshAgent>();
            agent.radius=.18f; agent.height=1.65f; agent.speed=1.15f; agent.acceleration=2; agent.angularSpeed=150;
            agent.stoppingDistance=.15f; agent.enabled=false; guide.position=UserPosition; arGuide.gameObject.SetActive(false);
            followCamera=mapCamera.gameObject.AddComponent<CampusFollowCamera>();followCamera.target=guide;followCamera.obstacles=building;
            mapCamera.orthographic=false;mapCamera.fieldOfView=60;
            Floor = -2; SetFloor(0); ResetView();
        }

        private LineRenderer MakeLine(string name, int layer)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(transform); line.gameObject.layer=layer;
            line.sharedMaterial=routeMaterial; line.widthMultiplier=.07f; line.numCornerVertices=3; line.useWorldSpace=true; return line;
        }
        private Transform MakeGuide(string name, int layer)
        {
            var t=new GameObject(name).transform; t.SetParent(transform); t.localScale=Vector3.one*.85f;
            string[] names={"body","head","arm_L","arm_R","leg_L","leg_R"};
            Vector3[] p={new Vector3(0,1.12f,0),new Vector3(0,1.68f,0),new Vector3(-.25f,1.12f,0),new Vector3(.25f,1.12f,0),new Vector3(-.095f,.36f,0),new Vector3(.095f,.36f,0)};
            for(int i=0;i<names.Length;i++){var j=new GameObject("ANIM_Companion_"+names[i]).transform;j.SetParent(t,false);j.localPosition=p[i];}
            t.gameObject.AddComponent<GuideAppearance>().Build();
            foreach(var child in t.GetComponentsInChildren<Transform>()) child.gameObject.layer=layer;
            return t;
        }

        public void SelectRoom(Room room)
        {
            if(room==null)return;
            StopPreview(); Destination=room; Arrived=false; ComputeRoute(); SetFloor(room.floor);
        }
        public void SelectStart(int index)
        {
            if(index<0||index>=Data.checkpoints.Length)return;
            StopNavigation(); UserPosition=Data.checkpoints[index].position;
            SetFloor(Data.checkpoints[index].floor); Status="Preview start: "+Data.checkpoints[index].label;
            if(Destination!=null)ComputeRoute();
        }
        private bool ComputeRoute()
        {
            ClearRoute();
            if(Destination==null)return false;
            if(!CampusRouting.TryRoute(UserPosition,Destination.position,out route,out var error)) { route=null; Status=error; return false; }
            Remaining=CampusRouting.Length(route.corners);
            mapLine.positionCount=route.corners.Length;
            for(int i=0;i<route.corners.Length;i++)mapLine.SetPosition(i,route.corners[i]+Vector3.up*.09f);
            Status=Destination.Label+" - "+Mathf.CeilToInt(Remaining)+" m";
            return true;
        }
        private void ClearRoute(){route=null;Remaining=0;mapLine.positionCount=0;arLine.positionCount=0;arGuide.gameObject.SetActive(false);}

        public void Preview()
        {
            tracking.Stop(); StopPreview(); Mode=NavigationMode.Preview; Arrived=false;
            if(!ComputeRoute()){Mode=NavigationMode.Browse;return;}
            guide.gameObject.SetActive(true); agent.enabled=true;
            if(!agent.Warp(route.corners[0])||!agent.SetPath(route)){Status="Preview could not start.";StopPreview();return;}
            agent.isStopped=false; previousGuide=guide.position; followFloor=true; Status="PREVIEW - "+Destination.Label;
            if(route.corners.Length>1)guide.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(route.corners[1]-route.corners[0],Vector3.up));
            SetFollowView(true);
        }
        private void StopPreview(){if(agent!=null)agent.enabled=false;if(Mode==NavigationMode.Preview)Mode=NavigationMode.Browse;followFloor=false;}
        public void StopNavigation(){StopPreview();tracking.Stop();Mode=NavigationMode.Browse;Arrived=false;ClearRoute();Status="Navigation stopped.";}
        public void StartScan(){StopPreview();ClearRoute();Mode=NavigationMode.Checkpoint;tracking.Alignment.Invalidate();tracking.Begin();}
        public void SetCheckpoint(Checkpoint checkpoint,bool live)
        {
            UserPosition=checkpoint.position;Mode=live?NavigationMode.Live:NavigationMode.Checkpoint;Arrived=false;
            SetFloor(checkpoint.floor);if(Destination!=null)ComputeRoute();
            Status=live?"Tracking - "+checkpoint.label:"Checkpoint location only - "+checkpoint.label;
        }
        public void TrackingLost(string message)
        { Mode=NavigationMode.Checkpoint;ClearRoute();Status=message; }
        public void SetTrackedPosition(Vector3 raw)
        {
            if(Mode!=NavigationMode.Live)return;
            int candidate=Mathf.Clamp(Mathf.RoundToInt(raw.y/3.3f),0,3);
            if(candidate!=tracking.Alignment.ConfirmedFloor && Mathf.Abs(raw.y-candidate*3.3f)<.35f)
            {tracking.Alignment.Invalidate();TrackingLost("Floor change detected. Confirm at this floor's QR checkpoint.");return;}
            if(!NavMesh.SamplePosition(raw,out var sample,.6f,NavMesh.AllAreas)||Mathf.Abs(sample.position.y-raw.y)>.4f)
            {tracking.Alignment.Invalidate();TrackingLost("Position no longer matches the map. Scan a checkpoint.");return;}
            UserPosition=sample.position;
            if(Time.unscaledTime>=nextRouteUpdate&&Destination!=null){nextRouteUpdate=Time.unscaledTime+1;ComputeRoute();}
            if(HasRoute&&Remaining<.7f&&Destination.floor==tracking.Alignment.ConfirmedFloor){Arrived=true;Status="Arrived - "+Destination.Label;ClearRoute();}
        }

        public void SetARView(bool enabled)
        {
            ARView=enabled&&tracking.HasAR;mapCamera.enabled=!ARView;
            if(tracking.arCamera!=null)tracking.arCamera.enabled=ARView;
            if(tracking.background!=null)tracking.background.enabled=ARView;
        }
        public void SetFloor(int floor)
        {
            floor=FollowPerson?-1:Mathf.Clamp(floor,-1,3);if(Floor==floor||modelRenderers==null)return;Floor=floor;
            foreach(var rr in modelRenderers)
            {
                if(rr.name.StartsWith("Nav")||rr.name.StartsWith("NAV")||rr.name.Contains("QR_")||rr.name.Contains("_Sign_GLB")){rr.enabled=false;continue;}
                float min=rr.bounds.min.y,max=rr.bounds.max.y;
                rr.enabled=floor<0||(min<(floor*3.3f+2.2f)&&max>floor*3.3f-.15f);
                if(floor>=0 && (rr.name.StartsWith("ROOF_")||rr.name.Contains("Roof_")) && min>floor*3.3f+1)rr.enabled=false;
            }
        }
        public void ResetView()
        {
            mapCamera.orthographic=!FollowPerson;
            if(FollowPerson){followCamera.Snap();return;}
            mapCamera.orthographicSize=32;mapCamera.transform.position=new Vector3(3.7f,55,-24);mapCamera.transform.LookAt(new Vector3(3.7f,1,4));
        }
        public void SetFollowView(bool follow)
        {
            FollowPerson=follow;followCamera.enabled=follow;SetARView(false);SetFloor(follow?-1:Mathf.Clamp(Mathf.RoundToInt(UserPosition.y/3.3f),0,3));ResetView();
        }
        public void Zoom(float multiplier){if(FollowPerson){followCamera.distance=Mathf.Clamp(followCamera.distance*multiplier,1.3f,3.5f);return;}mapCamera.orthographicSize=Mathf.Clamp(mapCamera.orthographicSize*multiplier,4,70);}
        public void Pan(Vector2 amount){if(FollowPerson)return;var right=mapCamera.transform.right;var forward=Vector3.ProjectOnPlane(mapCamera.transform.up,Vector3.up).normalized;mapCamera.transform.position+=(right*amount.x+forward*amount.y)*mapCamera.orthographicSize*.15f;}

        private void Update()
        {
            if(marker==null)return;
            if(Mode!=NavigationMode.Preview)guide.position=UserPosition;
            guide.gameObject.SetActive(FollowPerson || Mode==NavigationMode.Preview);
            marker.position=UserPosition+Vector3.up*.18f;
            marker.gameObject.SetActive(Mode!=NavigationMode.Browse&&Mode!=NavigationMode.Preview);
            if(Mode==NavigationMode.Preview&&agent.enabled&&agent.isOnNavMesh)
            {
                UserPosition=guide.position;Remaining=agent.remainingDistance;
                phase+=Vector3.Distance(previousGuide,guide.position)*6.6f;previousGuide=guide.position;
                for(int i=0;i<limbs.Length;i++)limbs[i].localRotation=Quaternion.Euler(Mathf.Sin(phase)*(i==0||i==3?1:-1)*22,0,0);
                if(followFloor)SetFloor(Mathf.Clamp(Mathf.FloorToInt((guide.position.y+.2f)/3.3f),0,3));
                if(!agent.pathPending&&agent.remainingDistance<=.2f){Arrived=true;agent.isStopped=true;Status="Preview arrived - "+Destination.Label;}
            }
            bool arRoute=ARView&&Mode==NavigationMode.Live&&tracking.Alignment.Calibrated&&HasRoute;
            arGuide.gameObject.SetActive(arRoute);arLine.enabled=arRoute;
            for(int i=0;i<arrows.Length;i++)arrows[i].gameObject.SetActive(arRoute&&(i+1)*1.5f<Remaining);
            if(arRoute)
            {
                arLine.positionCount=route.corners.Length;
                for(int i=0;i<route.corners.Length;i++)arLine.SetPosition(i,tracking.Alignment.SessionPosition(route.corners[i]+Vector3.up*.08f));
                var p=CampusRouting.Ahead(route.corners,2);var q=CampusRouting.Ahead(route.corners,2.5f);
                arGuide.position=tracking.Alignment.SessionPosition(p);
                var direction=Quaternion.Inverse(tracking.Alignment.Rotation)*(q-p);
                if(direction.sqrMagnitude>.001f)arGuide.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(direction,Vector3.up));
                for(int i=0;i<arrows.Length;i++)
                {
                    var point=CampusRouting.Ahead(route.corners,(i+1)*1.5f);var next=CampusRouting.Ahead(route.corners,(i+1)*1.5f+.3f);
                    arrows[i].position=tracking.Alignment.SessionPosition(point+Vector3.up*.1f);
                    var tangent=Quaternion.Inverse(tracking.Alignment.Rotation)*(next-point);
                    if(tangent.sqrMagnitude>.001f)arrows[i].rotation=Quaternion.LookRotation(tangent,Vector3.up);
                }
            }
        }
        private void OnDestroy(){if(meshInstance.valid)NavMesh.RemoveNavMeshData(meshInstance);if(marker!=null)Destroy(marker.gameObject);if(arrowMesh!=null)Destroy(arrowMesh);}
    }
}
