using UnityEngine;

/// <summary>
/// Shows the device camera feed as the game background.
/// Falls back to a solid color / placeholder image if no camera is available
/// (this always happens in the Unity Editor unless a webcam is attached).
/// Attach this to a GameObject with a Renderer (e.g. a Quad or SpriteRenderer)
/// OR to a RawImage inside the Canvas - both options are supported below.
/// </summary>
public class CameraBackgroundManager : MonoBehaviour
{
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

    private WebCamTexture webCamTexture;
    private bool cameraActive = false;

    private void Start()
    {
        StartCamera();
    }

    /// <summary>
    /// Attempts to start the device's back camera. If unavailable, shows fallback.
    /// </summary>
    public void StartCamera()
    {
#if UNITY_ANDROID
        // Request camera permission at runtime on Android
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
        }
#endif

        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            // No camera found (typical in Unity Editor on a desktop with no webcam)
            ShowFallback();
            return;
        }

        // Prefer the back-facing camera on mobile
        string deviceName = devices[0].name;
        foreach (WebCamDevice device in devices)
        {
            if (!device.isFrontFacing)
            {
                deviceName = device.name;
                break;
            }
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
