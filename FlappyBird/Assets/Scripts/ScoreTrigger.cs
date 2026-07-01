using UnityEngine;

/// <summary>
/// Sits in the gap between the top and bottom pipe. When the bird passes through
/// it, the score increases by 1. Attach to a child GameObject of the PipePair
/// (a thin BoxCollider2D set to "Is Trigger" placed in the gap works well).
/// </summary>
public class ScoreTrigger : MonoBehaviour
{
    private bool scored = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (scored) return;

        if (other.CompareTag("Bird"))
        {
            scored = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(1);
            }
        }
    }
}
