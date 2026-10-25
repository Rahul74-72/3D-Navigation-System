using System;
using System.IO;
using System.Linq;
using System.Text;
using Campus.Navigation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.ARCore;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using ZXing;

namespace Campus.Editor
{
    public static class CampusBuild
    {
        public const string ScenePath="Assets/Campus/Scenes/Campus.unity";
        private static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        [MenuItem("Campus/Prepare final scene")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/Campus/Scenes");Directory.CreateDirectory("Assets/Campus/Data");Directory.CreateDirectory("Assets/Campus/Materials");Directory.CreateDirectory(Path.Combine(Root,"qr"));
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Campus_Main_Gates_Preview.unity");
            var old=GameObject.Find("Gate to First Floor Classroom 4");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
            var dataPath="Assets/Campus/Data/campus.json";
            if(!File.Exists(dataPath))File.WriteAllText(dataPath,JsonUtility.ToJson(CampusData.Provisional(),true));
            AssetDatabase.Refresh();
            var root=GameObject.Find("Campus_Main_Gates_Model");
            var camera=GameObject.Find("Campus Preview Camera").GetComponent<Camera>();camera.name="Map Camera";camera.cullingMask=~(1<<30);camera.overrideSceneCullingMask=0;camera.orthographic=true;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.85f,.89f,.91f);
            camera.orthographic=false;camera.fieldOfView=60;camera.transform.position=new Vector3(-11.5f,2.285f,-25.8f);camera.transform.rotation=Quaternion.LookRotation(Vector3.forward-Vector3.up*.16f);camera.nearClipPlane=.05f;camera.farClipPlane=250;
            var app=new GameObject("Campus Navigation").AddComponent<CampusApp>();
            app.gameObject.AddComponent<CampusSmoke>();
            app.catalogue=AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);app.navigation=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/Navigation/CampusFirstFloorWalkable.asset");app.building=root.transform;app.mapCamera=camera;
            CampusOfficeRoute.Apply(root.transform,app.navigation);
            app.routeMaterial=Material("Route",new Color(.04f,.65f,.4f));app.positionMaterial=Material("Position",new Color(.05f,.4f,.85f));
            var neutral=AssetDatabase.LoadAssetAtPath<Material>("Assets/Navigation/CampusNeutralConcrete.mat");
            foreach(var rr in root.GetComponentsInChildren<Renderer>(true))
            {var ms=rr.sharedMaterials;for(int i=0;i<ms.Length;i++)if(ms[i]==null)ms[i]=neutral;rr.sharedMaterials=ms;if(rr.name.StartsWith("Nav")||rr.name.StartsWith("NAV")||rr.name.Contains("QR_"))rr.enabled=false;}
            var tracking=app.gameObject.AddComponent<CampusTracking>();app.tracking=tracking;tracking.app=app;
            var session=new GameObject("AR Session").AddComponent<ARSession>();session.gameObject.AddComponent<ARInputManager>();tracking.session=session;
            var origin=new GameObject("XR Origin").AddComponent<XROrigin>();var offset=new GameObject("Camera Offset");offset.transform.SetParent(origin.transform,false);origin.CameraFloorOffsetObject=offset;origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Device;origin.CameraYOffset=0;
            var arCam=new GameObject("AR Camera").AddComponent<Camera>();arCam.transform.SetParent(offset.transform,false);arCam.nearClipPlane=.05f;arCam.farClipPlane=60;arCam.cullingMask=1<<30;arCam.clearFlags=CameraClearFlags.SolidColor;arCam.backgroundColor=Color.black;
            origin.Camera=arCam;tracking.origin=origin.gameObject;tracking.arCamera=arCam;tracking.cameraManager=arCam.gameObject.AddComponent<ARCameraManager>();tracking.background=arCam.gameObject.AddComponent<ARCameraBackground>();
            var driver=arCam.gameObject.AddComponent<TrackedPoseDriver>();
            driver.positionInput=new InputActionProperty(new InputAction("Position",InputActionType.Value,"<XRHMD>/centerEyePosition"));driver.rotationInput=new InputActionProperty(new InputAction("Rotation",InputActionType.Value,"<XRHMD>/centerEyeRotation"));driver.trackingStateInput=new InputActionProperty(new InputAction("Tracking State",InputActionType.Value,"<XRHMD>/trackingState"));
            origin.gameObject.SetActive(false);session.gameObject.SetActive(false);
            foreach(string guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                if(renderer.rendererFeatures.Any(f=>f is ARBackgroundRendererFeature))continue;
                var feature=ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();feature.name="AR Camera Background";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);renderer.SetDirty();EditorUtility.SetDirty(renderer);
            }
            var ui=new GameObject("Campus UI").AddComponent<CampusUI>();ui.app=app;
            var settings=AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Campus/Data/CampusPanel.asset");
            if(settings==null){settings=ScriptableObject.CreateInstance<PanelSettings>();settings.name="Campus Panel";AssetDatabase.CreateAsset(settings,"Assets/Campus/Data/CampusPanel.asset");}
            settings.themeStyleSheet=AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/Campus/Data/CampusTheme.tss");
            EditorUtility.SetDirty(settings);ui.GetComponent<UIDocument>().panelSettings=settings;
            ConfigureAndroid();EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();
            ExportMarkers(JsonUtility.FromJson<CampusData>(app.catalogue.text));ValidateRoutes();Debug.Log("CAMPUS_PREPARE_OK");
        }
        private static Material Material(string name,Color color)
        {
            string path="Assets/Campus/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(m,path);return m;
        }
        private static void ConfigureAndroid()
        {
            PlayerSettings.companyName="Campus Project";PlayerSettings.productName="Campus Navigator";PlayerSettings.bundleVersion="0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"org.campus.navigation");PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;PlayerSettings.runInBackground=false;PlayerSettings.Android.forceInternetPermission=false;
            var path="Assets/Campus/Data/XRSettings.asset";var per=AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
            if(per==null){per=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(per,path);}
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,per,true);
            if(!per.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))per.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            var settings=per.SettingsForBuildTarget(BuildTargetGroup.Android);settings.InitManagerOnStart=true;
            if(!XRPackageMetadataStore.AssignLoader(settings.Manager,"UnityEngine.XR.ARCore.ARCoreLoader",BuildTargetGroup.Android))throw new Exception("ARCore loader assignment failed");
            var ar=ARCoreSettings.GetOrCreateSettings();ar.requirement=ARCoreSettings.Requirement.Optional;ar.depth=ARCoreSettings.Requirement.Optional;
            if(!AssetDatabase.Contains(ar))AssetDatabase.CreateAsset(ar,"Assets/Campus/Data/ARCoreSettings.asset");ARCoreSettings.currentSettings=ar;EditorUtility.SetDirty(per);EditorUtility.SetDirty(settings);
        }
        private static void ExportMarkers(CampusData data)
        {
            foreach(var checkpoint in data.checkpoints)
            {
                var matrix=new ZXing.QrCode.QRCodeWriter().encode(checkpoint.Payload,BarcodeFormat.QR_CODE,512,512);
                var texture=new Texture2D(512,512,TextureFormat.RGB24,false);var colors=new Color32[512*512];
                for(int y=0;y<512;y++)for(int x=0;x<512;x++)colors[y*512+x]=matrix[x,511-y]?new Color32(0,0,0,255):new Color32(255,255,255,255);
                texture.SetPixels32(colors);texture.Apply();File.WriteAllBytes(Path.Combine(Root,"qr",checkpoint.id+".png"),texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            }
        }
        [MenuItem("Campus/Validate routes")]
        public static void ValidateRoutes()
        {
            var data=JsonUtility.FromJson<CampusData>(File.ReadAllText("Assets/Campus/Data/campus.json"));var mesh=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/Navigation/CampusFirstFloorWalkable.asset");var instance=NavMesh.AddNavMeshData(mesh);
            var output=new StringBuilder("room_id,label,status,length_m,reason\n");int passed=0;
            try {foreach(var room in data.rooms){bool ok=CampusRouting.TryRoute(data.checkpoints[room.building=="A"?0:1].position,room.position,out var path,out var error);if(ok)passed++;output.AppendLine(room.id+",\""+room.Label+"\","+(ok?"CONNECTED":"BLOCKED")+","+(ok?CampusRouting.Length(path.corners).ToString("F2",System.Globalization.CultureInfo.InvariantCulture):"0")+",\""+error+"\"");}}
            finally{NavMesh.RemoveNavMeshData(instance);}
            Directory.CreateDirectory(Path.Combine(Root,"qa"));File.WriteAllText(Path.Combine(Root,"qa/route-audit.csv"),output.ToString());Debug.Log("ROUTE_AUDIT: "+passed+"/"+data.rooms.Length+" connected from matching gate. See qa/route-audit.csv");
        }
        [MenuItem("Campus/Validate every room pair")]
        public static void ValidateRoomPairs()
        {
            var data=JsonUtility.FromJson<CampusData>(File.ReadAllText("Assets/Campus/Data/campus.json"));
            var mesh=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/Navigation/CampusFirstFloorWalkable.asset");var instance=NavMesh.AddNavMeshData(mesh);
            var output=new StringBuilder("from_id,to_id,status,reason\n");int connected=0,total=0;
            try
            {
                foreach(var from in data.rooms)foreach(var to in data.rooms)
                {
                    if(from.id==to.id)continue;total++;
                    bool ok=CampusRouting.TryRoute(from.position,to.position,out _,out var error);if(ok)connected++;
                    output.AppendLine(from.id+","+to.id+","+(ok?"CONNECTED":"BLOCKED")+",\""+error+"\"");
                }
            }
            finally{NavMesh.RemoveNavMeshData(instance);}
            File.WriteAllText(Path.Combine(Root,"qa/room-pairs.csv"),output.ToString());Debug.Log("ROOM_PAIR_AUDIT: "+connected+"/"+total+" connected");
        }
        [MenuItem("Campus/Build Android APK")]
        public static void Android()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new BuildFailedException("Install Android Build Support, SDK/NDK and OpenJDK through Unity Hub first.");
            Directory.CreateDirectory(Path.Combine(Root,"Builds"));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(Root,"Builds/CampusNavigator.apk"),target=BuildTarget.Android,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException(report.summary.result.ToString());
        }
        [MenuItem("Campus/Build Windows preview")]
        public static void PrepareAndBuildWindows(){Prepare();Windows();}
        public static void Windows()
        {
            Directory.CreateDirectory(Path.Combine(Root,"Builds/Windows"));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(Root,"Builds/Windows/CampusNavigator.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException(report.summary.result.ToString());
        }
    }
}
