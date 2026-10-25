using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Campus.Navigation
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class CampusUI : MonoBehaviour
    {
        public CampusApp app;
        private VisualElement root, panel, mapTools, calibration;
        private ScrollView results;
        private Label status, destination, mode, qrPrompt;
        private TextField search;
        private DropdownField floors, buildings, starts;
        private Button confirm, arButton;
        private Image cameraPreview;
        private bool portrait;
        private int width, height;
        private readonly Color ink=new Color(.12f,.17f,.19f),teal=new Color(.05f,.43f,.4f);

        private void Start()
        {
            var doc=GetComponent<UIDocument>();
            if(doc.panelSettings==null){doc.panelSettings=ScriptableObject.CreateInstance<PanelSettings>();doc.panelSettings.scaleMode=PanelScaleMode.ConstantPixelSize;}
            doc.panelSettings.scaleMode=PanelScaleMode.ConstantPixelSize;
            root=doc.rootVisualElement; root.style.flexGrow=1;
            root.style.fontSize=14;root.style.color=ink;
            if(!app.enabled){root.Add(new Label(app.Status));return;}
            root.pickingMode=PickingMode.Ignore;
            panel=new VisualElement();root.Add(panel);panel.style.position=Position.Absolute;
            panel.style.backgroundColor=new Color(.97f,.98f,.98f,.98f);panel.style.paddingLeft=18;panel.style.paddingRight=18;panel.style.paddingTop=14;panel.style.paddingBottom=14;
            var title=new Label("CAMPUS");title.style.fontSize=20;title.style.unityFontStyleAndWeight=FontStyle.Bold;title.style.color=ink;panel.Add(title);
            mode=new Label();mode.style.fontSize=12;mode.style.color=teal;panel.Add(mode);
            search=new TextField();search.label="Destination";search.style.marginTop=12;panel.Add(search);search.RegisterValueChangedCallback(_=>RefreshRooms());
            search.name="DestinationSearch";
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;panel.Add(row);
            buildings=new DropdownField(new System.Collections.Generic.List<string>{"All","A","B"},0);buildings.style.flexGrow=1;row.Add(buildings);
            floors=new DropdownField(new System.Collections.Generic.List<string>{"All floors","Ground","Floor 1","Floor 2","Floor 3"},0);floors.style.flexGrow=1;row.Add(floors);
            buildings.name="BuildingFilter";floors.name="FloorFilter";
            buildings.RegisterValueChangedCallback(_=>RefreshRooms());floors.RegisterValueChangedCallback(_=>{RefreshRooms();app.SetFloor(floors.index-1);});
            row.style.height=44;row.style.flexShrink=0;
            buildings.style.height=40;floors.style.height=40;
            buildings.style.minHeight=40;floors.style.minHeight=40;
            results=new ScrollView();results.style.flexGrow=1;results.style.minHeight=60;results.style.marginTop=6;panel.Add(results);
            results.name="RoomResults";
            destination=new Label("Choose a destination");destination.style.whiteSpace=WhiteSpace.Normal;destination.style.marginTop=8;destination.style.unityFontStyleAndWeight=FontStyle.Bold;panel.Add(destination);
            starts=new DropdownField("Preview start",app.Data.checkpoints.Select(c=>c.label).ToList(),0);panel.Add(starts);starts.RegisterValueChangedCallback(_=>app.SelectStart(starts.index));
            var actions=new VisualElement();actions.style.flexDirection=FlexDirection.Row;panel.Add(actions);
            actions.style.height=38;actions.style.flexShrink=0;
            Button(actions,"Preview",()=>app.Preview());Button(actions,"Scan QR",()=>app.StartScan());Button(actions,"Stop",()=>app.StopNavigation());
            foreach(var child in actions.Children())child.style.flexGrow=1;
            arButton=Button(panel,"AR view",()=>{if(!app.tracking.HasAR)app.StartScan();else app.SetARView(!app.ARView);});
            status=new Label();status.style.whiteSpace=WhiteSpace.Normal;status.style.fontSize=13;status.style.minHeight=48;status.style.marginTop=8;panel.Add(status);
            calibration=new VisualElement();panel.Add(calibration);
            qrPrompt=new Label("Stand on the checkpoint mark and face its arrow.");qrPrompt.style.whiteSpace=WhiteSpace.Normal;calibration.Add(qrPrompt);
            var phoneHeight=new Slider("Phone height (m)",.7f,2.2f);phoneHeight.value=1.3f;phoneHeight.showInputField=true;phoneHeight.RegisterValueChangedCallback(e=>app.tracking.phoneHeight=e.newValue);calibration.Add(phoneHeight);
            confirm=Button(calibration,"Confirm checkpoint",()=>app.tracking.Confirm());
            cameraPreview=new Image();cameraPreview.scaleMode=ScaleMode.ScaleToFit;cameraPreview.style.position=Position.Absolute;cameraPreview.style.left=0;cameraPreview.style.top=0;cameraPreview.style.width=Length.Percent(100);cameraPreview.style.height=Length.Percent(40);cameraPreview.pickingMode=PickingMode.Ignore;root.Insert(0,cameraPreview);
            mapTools=new VisualElement();root.Add(mapTools);mapTools.style.position=Position.Absolute;mapTools.style.top=16;mapTools.style.right=16;
            Button(mapTools,"+",()=>app.Zoom(.8f)).tooltip="Zoom in";Button(mapTools,"-",()=>app.Zoom(1.25f)).tooltip="Zoom out";
            Button(mapTools,"Home",()=>app.ResetView()).tooltip="Frame campus";
            Button(mapTools,"<",()=>app.Pan(Vector2.left)).tooltip="Pan left";Button(mapTools,">",()=>app.Pan(Vector2.right)).tooltip="Pan right";
            Button(mapTools,"^",()=>app.Pan(Vector2.up)).tooltip="Pan forward";Button(mapTools,"v",()=>app.Pan(Vector2.down)).tooltip="Pan back";
            mapTools.style.flexDirection=FlexDirection.Row;
            foreach(var child in panel.Children())if(child!=results)child.style.flexShrink=0;
            foreach(var child in mapTools.Children()){child.style.flexGrow=0;child.style.width=36;child.style.height=36;}
            mapTools[2].style.width=54;
            foreach(var field in new[]{buildings,floors,starts})
            {
                var input=field.Q<VisualElement>(className:"unity-base-field__input");if(input!=null){input.style.paddingTop=0;input.style.paddingBottom=0;input.style.alignItems=Align.Center;}
            }
            RefreshRooms(); Layout();
        }

        private Button Button(VisualElement parent,string label,System.Action action)
        {
            var button=new Button(action){text=label};button.style.height=34;button.style.minWidth=36;button.style.flexGrow=0;button.style.marginTop=4;button.style.color=ink;button.style.fontSize=14;
            button.style.borderTopLeftRadius=4;button.style.borderTopRightRadius=4;button.style.borderBottomLeftRadius=4;button.style.borderBottomRightRadius=4;parent.Add(button);return button;
        }
        public static string Query(string text)
        {
            text=(text??"").Trim();
            foreach(string prefix in new[]{"take me to ","navigate to ","go to ","find "})
                if(text.StartsWith(prefix,System.StringComparison.OrdinalIgnoreCase))return text.Substring(prefix.Length).Trim();
            return text;
        }
        private void RefreshRooms()
        {
            if(results==null)return;results.Clear();
            var rooms=app.Data.Search(Query(search.value),floors.index-1,buildings.value);
            if(rooms.Length==0){results.Add(new Label("No matching rooms"));return;}
            foreach(var room in rooms)
            {
                var button=Button(results,room.Label,()=>{app.SelectRoom(room);destination.text=room.Label;});
                button.style.whiteSpace=WhiteSpace.Normal;button.style.height=48;button.style.flexShrink=0;
            }
        }
        private void Layout()
        {
            width=Screen.width;height=Screen.height;portrait=height>width;
            var settings=GetComponent<UIDocument>().panelSettings;settings.scale=Mathf.Max(.7f,portrait?width/430f:height/900f);
            panel.style.left=0;panel.style.bottom=0;
            panel.style.width=portrait?Length.Percent(100):new Length(340);
            panel.style.height=portrait?Length.Percent(62):Length.Percent(100);
            app.mapCamera.rect=portrait?new Rect(0,.62f,1,.38f):new Rect(340*settings.scale/width,0,1-340*settings.scale/width,1);
        }
        private void Update()
        {
            if(root==null||mode==null)return;
            if(width!=Screen.width||height!=Screen.height)Layout();
            status.text=app.Status;mode.text=app.Mode==NavigationMode.Preview?"PREVIEW / NOT LIVE LOCATION":app.Mode==NavigationMode.Live?"LIVE / CALIBRATED":"MAP / CHECKPOINT POSITIONING";
            destination.text=app.Destination==null?"Choose a destination":app.Destination.Label;
            if(portrait)panel.style.height=Length.Percent(app.tracking.Pending!=null?79:62);
            arButton.text=app.ARView?"3D map":"AR view";
            calibration.style.display=app.tracking.Pending!=null?DisplayStyle.Flex:DisplayStyle.None;
            cameraPreview.image=app.tracking.FallbackCamera;
            cameraPreview.style.display=app.tracking.FallbackCamera!=null?DisplayStyle.Flex:DisplayStyle.None;
            mapTools.style.display=app.ARView?DisplayStyle.None:DisplayStyle.Flex;
            starts.SetEnabled(app.Mode!=NavigationMode.Live);
        }
    }
}
