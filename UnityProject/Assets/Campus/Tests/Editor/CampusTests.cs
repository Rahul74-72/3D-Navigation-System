using System;
using System.Linq;
using Campus.Navigation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using ZXing;

public sealed class CampusTests
{
    [TestCase(false)] [TestCase(true)]
    public void BuildingARoutesUseOfficeFrontApproach(bool reverse)
    {
        var mesh=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/Navigation/CampusFirstFloorWalkable.asset");var handle=NavMesh.AddNavMeshData(mesh);
        try
        {
            var data=JsonUtility.FromJson<CampusData>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Campus/Data/campus.json").text);
            foreach(var room in data.rooms.Where(r=>r.building=="A"))
            {
                var gate=data.checkpoints[0].position;
                Assert.IsTrue(CampusRouting.TryRoute(reverse?room.position:gate,reverse?gate:room.position,out var path,out var error),room.id+": "+error);
                bool front=false;
                for(float distance=0;distance<=CampusRouting.Length(path.corners)+.1f;distance+=.1f)
                {
                    var p=CampusRouting.Ahead(path.corners,distance);
                    Assert.IsFalse(p.y<1 && p.x<-9.9f && p.z>-15.6f && p.z<-8,room.id+" detours behind office at "+p);
                    front|=p.y<.2f && p.x>-3.2f && p.x<-1.5f && p.z>-7.85f && p.z<-7;
                }
                Assert.IsTrue(front,room.id+" misses the front entrance");
            }
        }
        finally{NavMesh.RemoveNavMeshData(handle);}
    }
    [TestCase("A",-2.5f)] [TestCase("B",9.9f)]
    public void ClosedOfficeDestinationStopsOutsideDoor(string building,float x)
    {
        var data=JsonUtility.FromJson<CampusData>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Campus/Data/campus.json").text);
        var room=data.rooms.Single(r=>r.id==building+"-F0-OFFICE");
        Assert.Less(Vector3.Distance(new Vector3(x,.07f,-7.5f),room.position),.001f);
        var mesh=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/Navigation/CampusFirstFloorWalkable.asset");var handle=NavMesh.AddNavMeshData(mesh);
        try
        {
            Assert.IsTrue(NavMesh.SamplePosition(room.position,out var start,.15f,NavMesh.AllAreas));
            Assert.IsTrue(NavMesh.Raycast(start.position,new Vector3(x,.01f,-8.7f),out _,NavMesh.AllAreas),"Closed office door must block traversal");
        }
        finally{NavMesh.RemoveNavMeshData(handle);}
    }
    [Test] public void GroundOfficeDoorsAreClosedInPreparedScene()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(Campus.Editor.CampusBuild.ScenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            var building=scene.GetRootGameObjects().Single(g=>g.name=="Campus_Main_Gates_Model");
            foreach(string side in new[]{"Original","Duplicate"})
            {
                var door=building.GetComponentsInChildren<Renderer>().Single(r=>r.name=="OFFICE_"+side+"_F1_CorridorDoor");
                Assert.Less(door.bounds.size.z,.06f);Assert.Less(door.bounds.max.z,-7.8f);Assert.NotNull(door.GetComponent<MeshCollider>());
            }
        }
        finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
    }
    [Test] public void FollowCameraStaysBehindPerson(){var p=CampusFollowCamera.DesiredPosition(Vector3.zero,Vector3.forward,2.4f);Assert.Less(p.z,0);Assert.AreEqual(1.5f,p.y,.001);}
    [Test] public void FollowCameraUsesPersonsFloor(){var p=CampusFollowCamera.DesiredPosition(new Vector3(0,6.6f,0),Vector3.right,2.4f);Assert.AreEqual(8.1f,p.y,.001);Assert.Less(p.x,0);}
    [Test] public void FollowCameraIgnoresFacingPitch(){var p=CampusFollowCamera.DesiredPosition(Vector3.zero,new Vector3(0,1,1),2.4f);Assert.AreEqual(1.5f,p.y,.001);Assert.AreEqual(-2.4f,p.z,.001);}
    [Test] public void CatalogueHasStableUniqueIds(){var d=CampusData.Provisional();Assert.IsNull(d.Validate());Assert.AreEqual(56,d.rooms.Length);Assert.AreEqual(10,d.checkpoints.Length);}
    [Test] public void ConfirmedGroundRooms(){var d=CampusData.Provisional();Assert.AreEqual(1,d.Search("Language Lab",0,"A").Length);Assert.AreEqual(1,d.Search("IoT Lab",0,"A").Length);}
    [Test] public void OfficesStackCorrectly(){var d=CampusData.Provisional();Assert.AreEqual(1,d.Search("Environment Lab",1,"A").Length);Assert.AreEqual(1,d.Search("Library",2,"A").Length);}
    [Test] public void Classroom4IsFirstAboveOffice(){var r=CampusData.Provisional().rooms.Single(x=>x.id=="A-F1-R1");Assert.AreEqual("Classroom 4",r.name);Assert.AreEqual(3.31f,r.position.y,.001);}
    [TestCase("  iot lab  ")] [TestCase("IoT LAB")] public void SearchIsTrimmedCaseInsensitive(string text){Assert.AreEqual(2,CampusData.Provisional().Search(text).Length);}
    [Test] public void UnknownSearchIsEmpty(){Assert.IsEmpty(CampusData.Provisional().Search("nonexistent"));}
    [Test] public void DuplicateRoomRejected(){var d=CampusData.Provisional();d.rooms[1].id=d.rooms[0].id;Assert.IsNotNull(d.Validate());}
    [Test] public void NullRoomRejected(){var d=CampusData.Provisional();d.rooms[0]=null;Assert.IsNotNull(d.Validate());}
    [Test] public void NullCheckpointRejected(){var d=CampusData.Provisional();d.checkpoints[0]=null;Assert.IsNotNull(d.Validate());}
    [Test] public void InvalidCoordinatesRejected(){var d=CampusData.Provisional();d.rooms[0].position=new Vector3(float.NaN,0,0);Assert.IsNotNull(d.Validate());}
    [Test] public void UnsupportedSchemaRejected(){var d=CampusData.Provisional();d.schemaVersion=2;Assert.IsNotNull(d.Validate());}
    [Test] public void DuplicateCheckpointRejected(){var d=CampusData.Provisional();d.checkpoints[1].id=d.checkpoints[0].id;Assert.IsNotNull(d.Validate());}
    [TestCase(null)] [TestCase("")] [TestCase("https://evil.example/")] [TestCase("campus:v2:gate-a")] [TestCase("campus:v1:unknown")] [TestCase("CAMPUS:v1:gate-a")]
    public void UntrustedQrRejected(string code){Assert.IsNull(CampusData.Provisional().Decode(code));}
    [Test] public void OversizedQrRejected(){Assert.IsNull(CampusData.Provisional().Decode("campus:v1:"+new string('x',200)));}
    [Test] public void KnownQrAccepted(){Assert.AreEqual("gate-a",CampusData.Provisional().Decode("campus:v1:gate-a").id);}
    [Test] public void AllMarkersRequireSurvey(){Assert.IsTrue(CampusData.Provisional().checkpoints.All(c=>!c.surveyed));}
    [Test] public void CalibrationMapsPhoneToGround(){var a=new PoseAlignment();var c=new Checkpoint{floor=1,position=new Vector3(10,3.3f,20),facingYaw=90};a.Calibrate(c,new Vector3(2,1.3f,4),0,1.3f);Assert.Less(Vector3.Distance(c.position,a.MapPosition(new Vector3(2,1.3f,4))),.001);}
    [Test] public void CalibrationRotatesDisplacement(){var a=new PoseAlignment();a.Calibrate(new Checkpoint{facingYaw=90},Vector3.up*1.3f,0,1.3f);Assert.Less(Vector3.Distance(Vector3.right,a.MapPosition(new Vector3(0,1.3f,1))),.001);}
    [Test] public void MapArRoundTrip(){var a=new PoseAlignment();a.Calibrate(new Checkpoint{position=new Vector3(4,3.3f,2),facingYaw=150},new Vector3(2,1,4),23,1.2f);var p=new Vector3(8,3.3f,10);Assert.Less(Vector3.Distance(p,a.Rotation*a.SessionPosition(p)+a.Offset),.001);}
    [Test] public void TrackingJumpInvalidates(){var a=new PoseAlignment();a.Calibrate(new Checkpoint(),Vector3.up,0,1);Assert.IsFalse(a.Update(new Vector3(10,1,0),out _));Assert.IsFalse(a.Calibrated);}
    [Test] public void DriftBudgetRequiresCorrection(){var a=new PoseAlignment();a.Calibrate(new Checkpoint(),Vector3.up,0,1);for(int i=1;i<=36;i++)a.Update(new Vector3(i,1,0),out _);Assert.IsFalse(a.Calibrated);}
    [Test] public void RecalibrationResetsDistance(){var a=new PoseAlignment();a.Calibrate(new Checkpoint(),Vector3.up,0,1);a.Update(new Vector3(1,1,0),out _);a.Calibrate(new Checkpoint(),Vector3.up,0,1);Assert.AreEqual(0,a.Travel);}
    [Test] public void BadPhoneHeightRejected(){Assert.Throws<ArgumentException>(()=>new PoseAlignment().Calibrate(new Checkpoint(),Vector3.zero,0,4));}
    [Test] public void NaNPhoneHeightRejected(){Assert.Throws<ArgumentException>(()=>new PoseAlignment().Calibrate(new Checkpoint(),Vector3.zero,0,float.NaN));}
    [Test] public void InvalidMarkerPoseRejected(){Assert.Throws<ArgumentException>(()=>new PoseAlignment().Calibrate(new Checkpoint{position=new Vector3(float.NaN,0,0)},Vector3.zero,0,1));}
    [Test] public void InvalidPoseInvalidatesTracking(){var a=new PoseAlignment();a.Calibrate(new Checkpoint(),Vector3.up,0,1);Assert.IsFalse(a.Update(new Vector3(float.NaN,1,0),out _));Assert.IsFalse(a.Calibrated);}
    [Test] public void RouteLengthAndGuideLookahead(){var p=new[]{Vector3.zero,Vector3.right*3,new Vector3(3,0,4)};Assert.AreEqual(7,CampusRouting.Length(p));Assert.AreEqual(new Vector3(3,0,2),CampusRouting.Ahead(p,5));Assert.AreEqual(p[2],CampusRouting.Ahead(p,50));}
    [TestCase("take me to Library","Library")] [TestCase("navigate to IoT Lab","IoT Lab")] [TestCase("find Classroom 4","Classroom 4")]
    public void OfflineCommands(string input,string expected){Assert.AreEqual(expected,CampusUI.Query(input));}
    [Test] public void QrEncoderDecoderRoundTrip()
    {
        var matrix=new ZXing.QrCode.QRCodeWriter().encode("campus:v1:gate-a",BarcodeFormat.QR_CODE,256,256);var pixels=new byte[256*256];for(int y=0;y<256;y++)for(int x=0;x<256;x++)pixels[y*256+x]=(byte)(matrix[x,y]?0:255);
        var result=new BarcodeReaderGeneric().Decode(pixels,256,256,RGBLuminanceSource.BitmapFormat.Gray8);Assert.AreEqual("campus:v1:gate-a",result.Text);
    }
    [Test] public void FirstFloorRouteUsesIntermediateStairHeights()
    {
        var mesh=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/Navigation/CampusFirstFloorWalkable.asset");Assert.NotNull(mesh);var handle=NavMesh.AddNavMeshData(mesh);
        try {var d=CampusData.Provisional();Assert.IsTrue(CampusRouting.TryRoute(d.checkpoints[0].position,d.rooms.Single(r=>r.id=="A-F1-R1").position,out var path,out var error),error);Assert.IsTrue(path.corners.Any(p=>p.y>1.2f&&p.y<3));Assert.Less(Mathf.Abs(path.corners.Last().y-3.3f),.1f);}
        finally {NavMesh.RemoveNavMeshData(handle);}
    }
    [Test] public void OffMapDestinationNeverGetsStraightLineRoute()
    {
        var mesh=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/Navigation/CampusFirstFloorWalkable.asset");var handle=NavMesh.AddNavMeshData(mesh);
        try{Assert.IsFalse(CampusRouting.TryRoute(CampusData.Provisional().checkpoints[0].position,Vector3.one*10000,out _,out var error));Assert.IsNotEmpty(error);}finally{NavMesh.RemoveNavMeshData(handle);}
    }
}
