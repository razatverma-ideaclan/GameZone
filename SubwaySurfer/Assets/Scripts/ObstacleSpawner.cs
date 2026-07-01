using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns obstacle rows ahead of the player at a fixed distance interval,
/// each in a randomly chosen lane, and cleans up obstacles that fall behind.
/// Attach to an empty GameObject named "ObstacleSpawner".
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Obstacle Prefabs")]
    [Tooltip("Barrier (blocks the lane, must switch lanes), Low (must jump), High (must slide).")]
    public GameObject[] obstaclePrefabs;

    [Header("Spawn Settings")]
    public float laneDistance = 3f;
    public float spawnLookahead = 40f;
    public float spawnGap = 14f;
    public float destroyBehindDistance = 10f;

    private float nextSpawnZ;
    private bool spawning = false;
    private readonly List<GameObject> activeObstacles = new List<GameObject>();

    public void StartSpawning()
    {
        nextSpawnZ = player.position.z + spawnLookahead;
        spawning = true;
    }

    public void StopSpawning()
    {
        spawning = false;
    }

    public void ClearObstacles()
    {
        foreach (GameObject obstacle in activeObstacles)
        {
            if (obstacle != null) Destroy(obstacle);
        }
        activeObstacles.Clear();
    }

    void Update()
    {
        if (!spawning || player == null) return;

        if (player.position.z + spawnLookahead >= nextSpawnZ)
        {
            SpawnRow();
            nextSpawnZ += spawnGap;
        }

        CleanUpBehindPlayer();
    }

    private void SpawnRow()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        int lane = Random.Range(0, 3); // 0 = left, 1 = middle, 2 = right
        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

        float x = (lane - 1) * laneDistance;
        Vector3 spawnPos = new Vector3(x, 0f, nextSpawnZ);

        GameObject obstacle = Instantiate(prefab, spawnPos, Quaternion.identity);
        activeObstacles.Add(obstacle);
    }

    private void CleanUpBehindPlayer()
    {
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject obstacle = activeObstacles[i];
            if (obstacle == null)
            {
                activeObstacles.RemoveAt(i);
                continue;
            }

            if (obstacle.transform.position.z < player.position.z - destroyBehindDistance)
            {
                Destroy(obstacle);
                activeObstacles.RemoveAt(i);
            }
        }
    }
}
