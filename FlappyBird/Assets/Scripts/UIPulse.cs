using UnityEngine;

/// <summary>
/// Gentle continuous scale pulse for UI elements (e.g. the "TAP" hint bubble),
/// so the Start screen feels alive instead of static.
/// </summary>
public class UIPulse : MonoBehaviour
{
    public float scaleAmount = 0.08f;
    public float speed = 3f;

    private Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * speed) * scaleAmount;
        transform.localScale = baseScale * s;
    }
}
