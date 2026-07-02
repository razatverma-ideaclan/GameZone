using UnityEngine;
using System.Collections;

/// <summary>
/// Spawns pipe pairs at a fixed interval on the right edge of the screen,
/// each with a randomized vertical gap position.
/// Attach this to an empty GameObject named "PipeSpawner".
/// </summary>
public class PipeSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("The PipePair prefab: a parent object with a top pipe, bottom pipe, and a ScoreTrigger child.")]
    public GameObject pipePairPrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 1.5f;
    public float spawnXPosition = 10f;

    [Header("Gap Randomization")]
    [Tooltip("Vertical center of the gap will be randomized between these Y values.")]
    public float minGapY = -2f;
    public float maxGapY = 2f;

    private Coroutine spawnRoutine;

    public void StartSpawning()
    {
        StopSpawning(); // avoid double coroutines if called twice
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnPipe();
            float currentInterval = spawnInterval;
            if (GameManager.Instance != null)
            {
                currentInterval /= GameManager.Instance.GetCurrentSpeedMultiplier();
            }
            yield return new WaitForSeconds(currentInterval);
        }
    }

    private void SpawnPipe()
    {
        if (pipePairPrefab == null)
        {
            Debug.LogWarning("PipeSpawner: pipePairPrefab is not assigned.");
            return;
        }

        float gapY = Random.Range(minGapY, maxGapY);
        Vector3 spawnPos = new Vector3(spawnXPosition, gapY, 0f);
        Instantiate(pipePairPrefab, spawnPos, Quaternion.identity);
    }
}
