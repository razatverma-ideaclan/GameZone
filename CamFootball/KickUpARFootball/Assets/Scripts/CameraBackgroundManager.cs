using System.Collections;
using UnityEngine;

/// <summary>
/// Shows the device camera feed as the game background.
/// Falls back to a solid color / placeholder image if no camera is available
/// (this always happens in the Unity Editor unless a webcam is attached).
/// Attach this to a GameObject with a Renderer (e.g. a Quad or SpriteRenderer)
/// OR to a RawImage inside the Canvas - both options are supported below.
///
/// Handles the Android/iOS runtime camera permission flow properly: permission
/// requests are asynchronous, so this waits for the user's actual answer before
/// trying to open the camera (fixes "camera doesn't show until app restart").
/// If permission is denied, it raises OnPermissionDenied so the UI can show a
/// "camera required" panel with a button to open the OS Settings screen, and
/// automatically retries when the app regains focus (e.g. after the user
/// enables the permission in Settings and switches back).
/// </summary>
public class CameraBackgroundManager : MonoBehaviour
{
    public enum PermissionState { Unknown, Requesting, Granted, Denied }

    [Header("Render Targets (assign ONE of these)")]
    [Tooltip("If using a world-space Quad/Plane for the background")]
    public Renderer backgroundRenderer;
    [Tooltip("If using a UI RawImage for the background")]
    public UnityEngine.UI.RawImage backgroundRawImage;

    [Header("Fallback")]
    [Tooltip("Color used when no camera is found (e.g. in the Editor)")]
    public Color fallbackColor = new Color(0.2f, 0.6f, 0.2f); // simple green "field" fallback
    [Tooltip("Optional placeholder sprite/texture shown instead of a flat color")]
    public Texture2D fallbackTexture;

    /// <summary>Raised when the user denies (or has not granted) camera permission.</summary>
    public System.Action OnPermissionDenied;
    /// <summary>Raised when the camera successfully starts playing.</summary>
    public System.Action OnPermissionGranted;

    private WebCamTexture webCamTexture;
    private bool cameraActive = false;
    private bool rotationApplied = false;
    private bool activeCameraIsFrontFacing = false;
    private PermissionState permissionState = PermissionState.Unknown;
    private bool startInProgress = false;

    /// <summary>Exposes the live webcam feed so other scripts (e.g. MotionKickInputManager) can read pixels from it.</summary>
    public WebCamTexture ActiveWebCamTexture => (cameraActive && webCamTexture != null && webCamTexture.isPlaying) ? webCamTexture : null;

    /// <summary>True if the active camera is front-facing (e.g. desktop webcam). Lets other
    /// scripts (like motion detection) apply the same mirroring correction as the display.</summary>
    public bool IsActiveCameraFrontFacing => activeCameraIsFrontFacing;

    public bool IsCameraActive => cameraActive;
    public PermissionState CurrentPermissionState => permissionState;

    private void Start()
    {
        StartCamera();
    }

    private void Update()
    {
        // Mobile camera feeds often arrive rotated/mirrored relative to the screen.
        // WebCamTexture only reports the correct rotation/mirroring once it has a
        // real frame (width becomes a real resolution, not the initial placeholder),
        // so we check every frame until we've successfully applied the correction once.
        if (!rotationApplied && cameraActive && webCamTexture != null && webCamTexture.width > 100)
        {
            ApplyCameraOrientation();
            rotationApplied = true;
        }
    }

    /// <summary>
    /// If the app regains focus (e.g. the user just came back from the OS Settings
    /// screen after enabling camera permission), automatically retry starting the
    /// camera instead of forcing a full app restart.
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !cameraActive && !startInProgress &&
            (permissionState == PermissionState.Denied || permissionState == PermissionState.Unknown))
        {
            StartCamera();
        }
    }

    /// <summary>
    /// Rotates, mirrors, and resizes the background RawImage so the camera feed
    /// completely fills the screen (cropping any overflow) regardless of the
    /// device's rotation - this is required on real Android/iOS devices, where a
    /// naive stretch-to-fit would otherwise show the feed as a small, letterboxed
    /// box with the fallback color bleeding through around it.
    /// </summary>
    private void ApplyCameraOrientation()
    {
        if (backgroundRawImage == null || webCamTexture == null) return;

        RectTransform rt = backgroundRawImage.rectTransform;
        RectTransform parentRt = rt.parent as RectTransform;

        int rotationAngle = webCamTexture.videoRotationAngle;
        bool swapDimensions = (rotationAngle == 90 || rotationAngle == 270);

        float screenW = parentRt != null ? parentRt.rect.width : Screen.width;
        float screenH = parentRt != null ? parentRt.rect.height : Screen.height;

        // Anchor/pivot to dead-center so rotation pivots correctly around the middle
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        // Size the image using the camera's native resolution (before rotation is
        // visually applied), then scale it up so it fully covers the screen -
        // this is a standard "aspect fill" calculation, cropping any overflow.
        float camW = webCamTexture.width;
        float camH = webCamTexture.height;
        rt.sizeDelta = new Vector2(camW, camH);

        float effectiveCamW = swapDimensions ? camH : camW;
        float effectiveCamH = swapDimensions ? camW : camH;
        float coverScale = Mathf.Max(screenW / effectiveCamW, screenH / effectiveCamH);

        // Flip vertically if the device reports the feed as mirrored
        float scaleY = webCamTexture.videoVerticallyMirrored ? -1f : 1f;

        // Front-facing cameras (typical on laptops/desktops used for Editor testing)
        // show a horizontally mirrored "selfie" image, which makes left/right kicks
        // feel backwards. Mobile builds use the back camera (not front-facing), so
        // this correction only ever applies during desktop testing, never on device.
        float scaleX = activeCameraIsFrontFacing ? -1f : 1f;

        rt.localEulerAngles = new Vector3(0f, 0f, -rotationAngle);
        rt.localScale = new Vector3(scaleX * coverScale, scaleY * coverScale, 1f);
    }

    /// <summary>
    /// Kicks off the (async) permission + camera-start flow. Safe to call again
    /// any time (e.g. from a "Retry" button after the user grants permission).
    /// </summary>
    public void StartCamera()
    {
        if (startInProgress) return;
        StartCoroutine(RequestPermissionAndStart());
    }

    private IEnumerator RequestPermissionAndStart()
    {
        startInProgress = true;
        permissionState = PermissionState.Requesting;
        rotationApplied = false;

        // Always yield at least once outside of any #if block, so this method is
        // correctly compiled as a coroutine (has a real yield) on every platform -
        // including the Unity Editor, where neither of the platform blocks below
        // are compiled in (UNITY_EDITOR is always defined there), which otherwise
        // leaves this method with zero yield statements and fails to compile
        // (CS0161: not all code paths return a value).
        yield return null;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            bool responded = false;
            bool granted = false;

            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted += (permission) => { granted = true; responded = true; };
            callbacks.PermissionDenied += (permission) => { granted = false; responded = true; };
            callbacks.PermissionDeniedAndDontAskAgain += (permission) => { granted = false; responded = true; };

            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera, callbacks);

            float timeout = 30f;
            while (!responded && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (!granted)
            {
                permissionState = PermissionState.Denied;
                startInProgress = false;
                ShowFallback();
                OnPermissionDenied?.Invoke();
                yield break;
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            permissionState = PermissionState.Denied;
            startInProgress = false;
            ShowFallback();
            OnPermissionDenied?.Invoke();
            yield break;
        }
#endif

        permissionState = PermissionState.Granted;
        StartCameraInternal();
        startInProgress = false;
    }

    /// <summary>
    /// Actually opens the device camera. Only called once permission is confirmed
    /// granted (or we're in the Editor, where no runtime permission is needed).
    /// </summary>
    private void StartCameraInternal()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            // No camera found (typical in Unity Editor on a desktop with no webcam)
            ShowFallback();
            return;
        }

        // Prefer the back-facing camera on mobile
        string deviceName = devices[0].name;
        activeCameraIsFrontFacing = devices[0].isFrontFacing;
        foreach (WebCamDevice device in devices)
        {
            if (!device.isFrontFacing)
            {
                deviceName = device.name;
                activeCameraIsFrontFacing = false;
                break;
            }
        }

        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }

        webCamTexture = new WebCamTexture(deviceName);
        webCamTexture.Play();
        cameraActive = true;

        if (backgroundRenderer != null)
        {
            backgroundRenderer.material.mainTexture = webCamTexture;
        }
        if (backgroundRawImage != null)
        {
            backgroundRawImage.texture = webCamTexture;
        }

        OnPermissionGranted?.Invoke();
    }

    /// <summary>
    /// Displays a fallback background when the camera cannot be used.
    /// This keeps the game testable in the Unity Editor.
    /// </summary>
    private void ShowFallback()
    {
        cameraActive = false;

        if (fallbackTexture != null)
        {
            if (backgroundRenderer != null) backgroundRenderer.material.mainTexture = fallbackTexture;
            if (backgroundRawImage != null) backgroundRawImage.texture = fallbackTexture;
        }
        else
        {
            // No texture provided: just tint using a flat-color texture generated at runtime
            Texture2D flat = new Texture2D(2, 2);
            Color[] pixels = { fallbackColor, fallbackColor, fallbackColor, fallbackColor };
            flat.SetPixels(pixels);
            flat.Apply();

            if (backgroundRenderer != null) backgroundRenderer.material.mainTexture = flat;
            if (backgroundRawImage != null) backgroundRawImage.texture = flat;
        }
    }

    /// <summary>
    /// Opens the OS-level app settings screen so the user can manually grant the
    /// camera permission after having denied it once (Android/iOS won't show the
    /// system prompt again automatically after a denial).
    /// </summary>
    public void OpenAppSettings()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                string packageName = currentActivity.Call<string>("getPackageName");
                AndroidJavaObject uriObject = new AndroidJavaClass("android.net.Uri")
                    .CallStatic<AndroidJavaObject>("fromParts", "package", packageName, null);
                AndroidJavaObject intentObject = new AndroidJavaObject(
                    "android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uriObject);
                intentObject.Call<AndroidJavaObject>("addCategory", "android.intent.category.DEFAULT");
                intentObject.Call<AndroidJavaObject>("setFlags", 0x10000000); // FLAG_ACTIVITY_NEW_TASK
                currentActivity.Call("startActivity", intentObject);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[CameraBackgroundManager] Could not open app settings: " + e.Message);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        Application.OpenURL("app-settings:");
#endif
    }

    public void StopCamera()
    {
        if (cameraActive && webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
        cameraActive = false;
    }

    private void OnDisable()
    {
        StopCamera();
    }

    private void OnDestroy()
    {
        StopCamera();
    }
}
