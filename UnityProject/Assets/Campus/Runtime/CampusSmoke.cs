using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Campus.Navigation
{
    // Explicit command-line QA only; normal launches never run this sequence.
    public sealed class CampusSmoke : MonoBehaviour
    {
        private int errors;
        private void CaptureError(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        private IEnumerator Start()
        {
            var args=System.Environment.GetCommandLineArgs();if(!args.Contains("--campus-smoke"))yield break;
            Application.runInBackground=true;
            Application.logMessageReceived+=CaptureError;
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../qa"));Directory.CreateDirectory(output);
            yield return new WaitForSeconds(6);
            var app=GetComponent<CampusApp>();
            var ui=Object.FindFirstObjectByType<CampusUI>().GetComponent<UIDocument>().rootVisualElement;
            var search=ui.Q<TextField>("DestinationSearch");var rooms=ui.Q<ScrollView>("RoomResults");
            search.value="take me to Library";
            bool uiPassed=rooms.Query<Button>().ToList().Count(b=>b.text.Contains("Library"))==2;
            ui.Q<DropdownField>("BuildingFilter").value="A";
            uiPassed&=rooms.Query<Button>().ToList().Count(b=>b.text.Contains("Library"))==1;
            search.value="Classroom 4";ui.Q<DropdownField>("FloorFilter").value="Floor 1";
            uiPassed&=rooms.Query<Button>().ToList().Count(b=>b.text.Contains("Classroom 4"))==1;
            app.SelectRoom(app.Data.rooms.Single(r=>r.id=="A-F1-R1"));
            yield return new WaitForEndOfFrame();
            Screen.SetResolution(540,960,false);yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"portrait.png"));yield return new WaitForSeconds(1);
            Screen.SetResolution(1440,900,false);yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"desktop.png"));yield return new WaitForSeconds(1);
            Screen.SetResolution(360,800,false);yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"small-phone.png"));yield return new WaitForSeconds(1);
            app.Preview();yield return new WaitForSeconds(3);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"follow-walk.png"));yield return new WaitForSeconds(1);
            bool cameraPassed=app.FollowPerson&&!app.mapCamera.orthographic;
            float maxCameraHeight=0;
            bool stairCaptured=false;
            bool frontEntrance=false,rearDetour=false;
            Time.timeScale=10;
            float until=Time.realtimeSinceStartup+60;
            while(!app.Arrived&&Time.realtimeSinceStartup<until)
            {
                float cameraHeight=app.mapCamera.transform.position.y-app.Person.position.y;
                maxCameraHeight=Mathf.Max(maxCameraHeight,cameraHeight);
                cameraPassed&=cameraHeight<2.5f&&Vector3.Distance(app.mapCamera.transform.position,app.Person.position)<4;
                var p=app.UserPosition;
                rearDetour|=p.y<1&&p.x<-9.9f&&p.z>-15.6f&&p.z<-8;
                if(!frontEntrance&&p.y<.2f&&p.x>-3.2f&&p.x<-1.5f&&p.z>-7.85f&&p.z<-7)
                {frontEntrance=true;ScreenCapture.CaptureScreenshot(Path.Combine(output,"follow-office-front.png"));}
                if(!stairCaptured&&app.UserPosition.y>1.4f&&app.UserPosition.y<2.8f){stairCaptured=true;ScreenCapture.CaptureScreenshot(Path.Combine(output,"follow-stairs.png"));}
                yield return null;
            }
            Time.timeScale=1;
            yield return new WaitForSeconds(1);ScreenCapture.CaptureScreenshot(Path.Combine(output,"follow-arrival.png"));yield return new WaitForSeconds(1);
            app.SetFollowView(false);cameraPassed&=app.mapCamera.orthographic;app.SetFollowView(true);cameraPassed&=!app.mapCamera.orthographic;
            bool ok=app.Arrived&&Mathf.Abs(app.UserPosition.y-3.3f)<.2f&&uiPassed&&errors==0&&cameraPassed&&stairCaptured&&frontEntrance&&!rearDetour;
            app.mapCamera.GetComponent<CampusFollowCamera>().enabled=false;
            Object.FindFirstObjectByType<CampusUI>().enabled=false;
            ui.style.display=DisplayStyle.None;Screen.SetResolution(1000,700,false);
            app.mapCamera.rect=new Rect(0,0,1,1);
            foreach(string id in new[]{"A-F1-R1","A-F0-R6","A-F1-OFFICE","A-F2-OFFICE","B-F0-R1"})
            {
                var label=app.building.GetComponentsInChildren<Transform>().Single(t=>t.name=="RoomName_"+id+"_D1");
                float distance=id.EndsWith("-OFFICE")?.55f:1.1f;
                app.mapCamera.transform.SetPositionAndRotation(label.position-label.forward*distance,label.rotation);
                yield return new WaitForSeconds(.5f);
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"nameplate-"+id+".png"));yield return new WaitForSeconds(.5f);
            }
            ok&=errors==0;
            File.WriteAllText(Path.Combine(output,"runtime-smoke.txt"),"arrived="+app.Arrived+"\nposition="+app.UserPosition+"\nmode="+app.Mode+"\nsearchFilters="+uiPassed+"\nfollowCamera="+cameraPassed+"\nmaxCameraHeight="+maxCameraHeight+"\nstairCaptured="+stairCaptured+"\nfrontEntrance="+frontEntrance+"\nrearDetour="+rearDetour+"\nruntimeErrors="+errors+"\npassed="+ok);
            Application.logMessageReceived-=CaptureError;
            Application.Quit(ok?0:1);
        }
    }
}
