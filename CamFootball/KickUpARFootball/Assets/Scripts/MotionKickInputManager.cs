using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Detects motion (your leg, hand, or anything moving) in the camera feed near the
/// ball's on-screen position, and triggers a kick - without needing screen taps.
///
/// This is a lightweight interim step toward real leg detection: it uses simple
/// frame-differencing (comparing brightness/color changes between camera frames)
/// instead of a full AI pose-detection model. It's fast, needs no extra packages
/// or downloads, and works immediately with your existing webcam setup.
///
/// It calls the exact same BallController.Kick() method that KickInputManager uses,
/// so it can run side-by-side with tap/swipe, or you can disable the
/// KickInputManager component on the Managers object if you want camera-only input.
///
/// Later, this can be swapped for a real pose/skeleton-based FootDetectionInputManager
/// (see the project setup guide's "Future Foot Detection Plan") without changing
/// BallController, GameManager, or anything else.
/// </summary>
public class MotionKickInputManager : MonoBehaviour
{
    [Header("References")]
    public BallController ballController;
    public CameraBackgroundManager cameraBackgroundManager;
    [Tooltip("Optional - if assigned, shows a live 'Motion: X / Threshold: Y' readout so you can tune sensitivity in real time instead of guessing blind.")]
    public Text debugText;

    [Header("Detection Settings")]
    [Tooltip("Half-size (in camera pixels) of the square region scanned around the ball for motion. Bigger = easier to trigger with a whole leg/foot, but costs a little more performance.")]
    public int sampleResolution = 40;
    [Tooltip("How much average pixel change (0 to 1) counts as motion. Lower = more sensitive (triggers more easily, but more false positives).")]
    public float motionThreshold = 0.10f;
    [Tooltip("How many consecutive checks in a row must show motion before triggering a kick. Higher = fewer false positives from camera noise.")]
    public int consecutiveFramesRequired = 1;
    [Tooltip("Minimum seconds between triggered kicks, so held/continuous motion doesn't spam kicks.")]
    public float kickCooldown = 0.35f;
    [Tooltip("Every N frames to check for motion (2-3 keeps performance smooth on mobile).")]
    public int checkEveryNFrames = 2;
    [Tooltip("Only detect motion while the ball is in the lower portion of the screen (closer to leg height). 1 = whole screen, 0.5 = bottom half only.")]
    [Range(0.3f, 1f)]
    public float detectionHeightLimit = 0.7f;

    private Camera mainCamera;
    private Color32[] previousPixels;
    private float cooldownTimer = 0f;
    private int frameCounter = 0;
    private int consecutiveMotionFrames = 0;
    private float lastAverageDiff = 0f;
    private bool lastCheckWasValid = false;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            UpdateDebugText(0f, false);
            return;
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        frameCounter++;
        if (frameCounter % Mathf.Max(1, checkEveryNFrames) != 0)
            return;

        if (ballController == null || mainCamera == null || cameraBackgroundManager == null)
            return;

        WebCamTexture webCam = cameraBackgroundManager.ActiveWebCamTexture;
        if (webCam == null || !webCam.isPlaying)
            return;

        DetectMotionNearBall(webCam);
        UpdateDebugText(lastAverageDiff, lastCheckWasValid);
    }

    private void UpdateDebugText(float averageDiff, bool valid)
    {
        if (debugText == null) return;
        debugText.text = valid
            ? $"Motion: {averageDiff:0.000} / Threshold: {motionThreshold:0.000}"
            : "Motion: -- (ball out of range or camera not ready)";
    }

    /// <summary>
    /// Converts the ball's world position into a pixel coordinate on the RAW camera
    /// texture, accounting for whatever rotation/mirroring CameraBackgroundManager
    /// applied to display it. Without this, the sampled region would not actually
    /// correspond to where the ball visually appears on a rotated/mirrored phone
    /// camera feed, causing effectively random (glitchy, constantly-triggering) results.
    /// </summary>
    private bool TryGetCameraPixelPosition(WebCamTexture webCam, out int pixelX, out int pixelY)
    {
        pixelX = 0;
        pixelY = 0;

        RawImage rawImage = cameraBackgroundManager.backgroundRawImage;
        if (rawImage == null) return false;

        RectTransform rawImageRect = rawImage.rectTransform;
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(ballController.transform.position);

        if (screenPoint.z < 0f) return false; // behind the camera, shouldn't happen but guard anyway

        Vector2 localPoint;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImageRect, screenPoint, mainCamera, out localPoint);

        if (!success) return false;

        // sizeDelta was set to the camera's native width/height (see CameraBackgroundManager),
        // and the pivot is centered (0.5, 0.5), so localPoint is already in raw camera pixel
        // space, centered at zero - just re-center it to a standard 0..width / 0..height range.
        float camW = webCam.width;
        float camH = webCam.height;

        float rawX = localPoint.x + camW / 2f;
        float rawY = localPoint.y + camH / 2f;

        if (rawX < 0f || rawX >= camW || rawY < 0f || rawY >= camH)
            return false; // maps outside the actual camera frame

        pixelX = Mathf.Clamp((int)rawX, sampleResolution, webCam.width - sampleResolution - 1);
        pixelY = Mathf.Clamp((int)rawY, sampleResolution, webCam.height - sampleResolution - 1);
        return true;
    }

    private void DetectMotionNearBall(WebCamTexture webCam)
    {
        lastCheckWasValid = false;

        // Ball must currently be visible on screen for this check to make sense
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(ballController.transform.position);
        if (viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f)
        {
            consecutiveMotionFrames = 0;
            return;
        }

        // Skip detection while the ball is high up on screen - a real leg/foot can't
        // reach up there anyway, and this avoids false triggers from head/hand motion.
        if (viewportPos.y > detectionHeightLimit)
        {
            consecutiveMotionFrames = 0;
            return;
        }

        int centerX, centerY;
        if (!TryGetCameraPixelPosition(webCam, out centerX, out centerY))
        {
            consecutiveMotionFrames = 0;
            return;
        }

        Color32[] currentPixels = webCam.GetPixels32();
        if (currentPixels == null || currentPixels.Length == 0) return;

        if (previousPixels == null || previousPixels.Length != currentPixels.Length)
        {
            // First frame captured - nothing to compare against yet
            previousPixels = currentPixels;
            return;
        }

        float totalDiff = 0f;
        int sampleCount = 0;
        int step = Mathf.Max(1, sampleResolution / 10); // sparse sampling keeps this cheap

        float leftDiff = 0f;
        float rightDiff = 0f;

        for (int y = centerY - sampleResolution; y < centerY + sampleResolution; y += step)
        {
            for (int x = centerX - sampleResolution; x < centerX + sampleResolution; x += step)
            {
                if (x < 0 || x >= webCam.width || y < 0 || y >= webCam.height) continue;

                int index = y * webCam.width + x;
                if (index < 0 || index >= currentPixels.Length) continue;

                Color32 a = currentPixels[index];
                Color32 b = previousPixels[index];
                float diff = (Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b)) / (3f * 255f);

                totalDiff += diff;
                sampleCount++;

                if (x < centerX) leftDiff += diff; else rightDiff += diff;
            }
        }

        previousPixels = currentPixels;

        if (sampleCount == 0) return;

        float averageDiff = totalDiff / sampleCount;
        lastAverageDiff = averageDiff;
        lastCheckWasValid = true;

        if (averageDiff >= motionThreshold)
        {
            consecutiveMotionFrames++;
        }
        else
        {
            consecutiveMotionFrames = 0;
        }

        if (consecutiveMotionFrames >= consecutiveFramesRequired)
        {
            // Roughly steer the kick based on which side had more motion
            float horizontalDirection = Mathf.Clamp((rightDiff - leftDiff) * 4f, -1f, 1f);
            ballController.Kick(new Vector2(horizontalDirection, 0f), 1f);
            cooldownTimer = kickCooldown;
            consecutiveMotionFrames = 0;

            // Clear the stored frame so that once the cooldown ends, the very next
            // check just captures a fresh baseline instead of comparing against a
            // frame from half a second ago (which is usually a huge, meaningless
            // difference and made detection unreliable right after the first kick).
            previousPixels = null;
        }
    }
}
