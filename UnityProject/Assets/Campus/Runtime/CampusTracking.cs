using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using ZXing.Common;

namespace Campus.Navigation
{
    public sealed class CampusTracking : MonoBehaviour
    {
        public ARSession session;
        public Camera arCamera;
        public ARCameraManager cameraManager;
        public ARCameraBackground background;
        public GameObject origin;
        public CampusApp app;
        public bool Active { get; private set; }
        public bool Scanning { get; private set; }
        public bool HasAR { get; private set; }
        public bool Starting { get; private set; }
        public Checkpoint Pending { get; private set; }
        public WebCamTexture FallbackCamera { get; private set; }
        public readonly PoseAlignment Alignment = new PoseAlignment();
        public float phoneHeight = 1.3f;
        private float nextScan;
        private readonly BarcodeReaderGeneric reader = new BarcodeReaderGeneric {
            AutoRotate = true, Options = new DecodingOptions { TryHarder = true, PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE } }
        };

        public void Begin()
        {
            if (Starting) return;
            Pending = null; Scanning = true;
            if (Active) return;
            StartCoroutine(StartCamera());
        }

        private IEnumerator StartCamera()
        {
            Starting = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                float until = Time.realtimeSinceStartup + 20;
                while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera) && Time.realtimeSinceStartup < until) yield return null;
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                { app.Status = "Camera permission denied. Map browsing is still available."; Starting = Scanning = false; yield break; }
            }
            yield return ARSession.CheckAvailability();
            if (ARSession.state == ARSessionState.NeedsInstall) yield return ARSession.Install();
            HasAR = ARSession.state == ARSessionState.Ready || ARSession.state == ARSessionState.SessionTracking;
#else
            HasAR = false;
            yield return null;
#endif
            if (HasAR)
            {
                origin.SetActive(true); session.gameObject.SetActive(true);
                arCamera.enabled = true; background.enabled = true;
                app.SetARView(true);
            }
            else
            {
                if (WebCamTexture.devices.Length == 0)
                { app.Status = "No camera available. Use Preview for an editor walkthrough."; Starting = Scanning = false; yield break; }
                string cameraName = WebCamTexture.devices[0].name;
                foreach (var device in WebCamTexture.devices) if (!device.isFrontFacing) { cameraName = device.name; break; }
                FallbackCamera = new WebCamTexture(cameraName, 640, 480, 15); FallbackCamera.Play();
                app.Status = "QR checkpoint mode. This device has no active AR tracking.";
            }
            Active = true; Starting = false;
        }

        private void Update()
        {
            if (!Active) return;
            if (HasAR && Alignment.Calibrated)
            {
                if (ARSession.state != ARSessionState.SessionTracking)
                { Alignment.Invalidate(); app.TrackingLost("Tracking lost. Scan a checkpoint again."); }
                else if (Alignment.Update(arCamera.transform.position, out var position)) app.SetTrackedPosition(position);
                else app.TrackingLost("Position needs correction. Scan a checkpoint.");
            }
            if (!Scanning || Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + .4f;
            if (HasAR) DecodeARFrame(); else DecodeWebcam();
        }

        private void DecodeARFrame()
        {
            if (!cameraManager.TryAcquireLatestCpuImage(out var image)) return;
            using (image)
            {
                int divisor = Mathf.Max(1, image.width / 640);
                var conversion = new XRCpuImage.ConversionParams {
                    inputRect = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(image.width / divisor, image.height / divisor),
                    outputFormat = TextureFormat.R8, transformation = XRCpuImage.Transformation.None
                };
                using (var data = new NativeArray<byte>(image.GetConvertedDataSize(conversion), Allocator.Temp))
                {
                    image.Convert(conversion, data);
                    Accept(reader.Decode(data.ToArray(), conversion.outputDimensions.x, conversion.outputDimensions.y, RGBLuminanceSource.BitmapFormat.Gray8));
                }
            }
        }

        private void DecodeWebcam()
        {
            if (FallbackCamera == null || !FallbackCamera.didUpdateThisFrame || FallbackCamera.width < 32) return;
            Color32[] pixels = FallbackCamera.GetPixels32(); byte[] rgb = new byte[pixels.Length * 3];
            for (int i = 0; i < pixels.Length; i++) { rgb[i * 3] = pixels[i].r; rgb[i * 3 + 1] = pixels[i].g; rgb[i * 3 + 2] = pixels[i].b; }
            Accept(reader.Decode(rgb, FallbackCamera.width, FallbackCamera.height, RGBLuminanceSource.BitmapFormat.RGB24));
        }

        private void Accept(Result result)
        {
            if (result == null) return;
            Pending = app.Data.Decode(result.Text);
            if (Pending == null) { app.Status = "Unrecognized campus QR code."; return; }
            Scanning = false;
            app.Status = Pending.label + (Pending.surveyed ? "" : " - provisional marker") + ". Confirm standing point and arrow direction.";
        }

        public void Confirm()
        {
            if (Pending == null) return;
            if (!Pending.surveyed) { app.Status = "Survey required: this checkpoint is not approved for real navigation."; return; }
            if (HasAR)
            {
                if (ARSession.state != ARSessionState.SessionTracking) { app.Status = "Waiting for stable AR tracking."; return; }
                var direction = Vector3.ProjectOnPlane(arCamera.transform.forward, Vector3.up);
                if (direction.magnitude < .4f) { app.Status = "Hold the phone facing along the checkpoint arrow."; return; }
                Alignment.Calibrate(Pending, arCamera.transform.position, arCamera.transform.eulerAngles.y, phoneHeight);
            }
            app.SetCheckpoint(Pending, HasAR);
            Pending = null;
            if (!HasAR) Stop();
        }

        public void Stop()
        {
            StopAllCoroutines(); Starting = Active = Scanning = false; Pending = null; Alignment.Invalidate();
            if (FallbackCamera != null) { FallbackCamera.Stop(); Destroy(FallbackCamera); FallbackCamera = null; }
            origin.SetActive(false); session.gameObject.SetActive(false); HasAR = false; app.SetARView(false);
        }
        private void OnApplicationPause(bool paused) { if (paused && Active) { Alignment.Invalidate(); app.TrackingLost("App resumed or interrupted. Scan a checkpoint."); } }
        private void OnDestroy() { if (FallbackCamera != null) { FallbackCamera.Stop(); Destroy(FallbackCamera); } }
    }
}
