using UnityEngine;

/// <summary>
/// Gently bobs a UI element up and down using a sine wave.
/// Makes the Start screen title logo feel animated and alive.
/// </summary>
public class UIFloat : MonoBehaviour
{
    [Tooltip("How high/low the element floats (in local position Y units).")]
    public float floatAmount = 15f;

    [Tooltip("How fast the element floats.")]
    public float speed = 2.5f;

    private Vector3 basePosition;
    private float floatTimeOffset;

    void Awake()
    {
        basePosition = transform.localPosition;
        floatTimeOffset = Random.Range(0f, 10f); // offset to prevent perfectly synchronized floats
    }

    void Update()
    {
        float offsetY = Mathf.Sin((Time.time + floatTimeOffset) * speed) * floatAmount;
        transform.localPosition = basePosition + new Vector3(0f, offsetY, 0f);
    }
}
