using UnityEngine;

/// <summary>
/// Keeps a fixed horizontal view width no matter what screen aspect ratio the
/// device has, so pipe gaps and bird position play the same on every phone.
/// Extra screen height (tall phones) just reveals more sky/ground.
/// Attach to the Main Camera (must be Orthographic).
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFitWidth : MonoBehaviour
{
    [Tooltip("Half of the constant world width that should always be visible.")]
    public float targetHalfWidth = 9f;

    [Tooltip("Caps how far the camera can zoom out on very tall phone screens. " +
             "Once hit, extra screen width beyond targetHalfWidth is simply cropped " +
             "at the left/right edges instead of shrinking everything.")]
    public float maxOrthographicSize = 7f;

    [Tooltip("Floors how far the camera can zoom IN on wide/landscape screens (desktop " +
             "browsers, WebGL windows, tablets). Without this, targetHalfWidth / aspect can " +
             "shrink the vertical view enough that the ground (which sits at a fixed Y below " +
             "the camera) falls completely outside the frustum and disappears. Once hit, extra " +
             "height beyond what's needed just reveals more sky/side margin instead.")]
    public float minOrthographicSize = 6.5f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    // Recalculated every frame (cheap) so it stays correct if the screen
    // resolution/aspect ratio changes at runtime — e.g. testing in the Unity
    // Editor by switching the Game view's aspect dropdown, resizing a
    // desktop window, or an Android device entering split-screen/multi-window.
    void Update()
    {
        Apply();
    }

    private void Apply()
    {
        if (cam != null && cam.orthographic && cam.aspect > 0f)
        {
            float desiredSize = targetHalfWidth / cam.aspect;
            cam.orthographicSize = Mathf.Clamp(desiredSize, minOrthographicSize, maxOrthographicSize);
        }
    }
}
