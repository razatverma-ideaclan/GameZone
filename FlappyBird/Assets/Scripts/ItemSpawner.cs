using UnityEngine;
using System.Collections;

/// <summary>
/// Spawns coins/power-ups in the open corridor between pipe pairs — independent
/// of any pipe's gap, so collecting one is a separate up/down decision from
/// threading the gap. Modeled on PipeSpawner's timing pattern, offset by half
/// an interval so items land roughly halfway between consecutive pipe spawns.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [Header("Timing (mirrors PipeSpawner, offset to land between pipes)")]
    public float spawnInterval = 1.9f;
    public float initialDelay = 0.95f;
    public float spawnXPosition = 10f;

    [Header("Vertical Range")]
    [Tooltip("Wide range across the whole play area — not tied to any pipe gap.")]
    public float minY = -3.5f;
    public float maxY = 3.5f;

    [Header("Sprites")]
    public Sprite coinSprite;
    public Sprite magnetSprite;
    public Sprite boostSprite;
    public Sprite doubleSprite;
    public Sprite hammerSprite;

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
        float firstDelay = initialDelay;
        if (GameManager.Instance != null) firstDelay /= GameManager.Instance.GetCurrentSpeedMultiplier();
        yield return new WaitForSeconds(firstDelay);

        while (true)
        {
            RollSpawn();
            float currentInterval = spawnInterval;
            if (GameManager.Instance != null) currentInterval /= GameManager.Instance.GetCurrentSpeedMultiplier();
            yield return new WaitForSeconds(currentInterval);
        }
    }

    private void RollSpawn()
    {
        float roll = Random.value;
        if (roll < 0.5f) return; // 50% nothing
        else if (roll < 0.9f) Spawn(CollectableItem.ItemType.Coin, coinSprite);
        else SpawnRandomPowerup();
    }

    private void SpawnRandomPowerup()
    {
        int pick = Random.Range(0, 4);
        switch (pick)
        {
            case 0: Spawn(CollectableItem.ItemType.Magnet, magnetSprite); break;
            case 1: Spawn(CollectableItem.ItemType.Boost, boostSprite); break;
            case 2: Spawn(CollectableItem.ItemType.Double, doubleSprite); break;
            default: Spawn(CollectableItem.ItemType.Hammer, hammerSprite); break;
        }
    }

    private void Spawn(CollectableItem.ItemType type, Sprite sprite)
    {
        if (sprite == null) return;

        GameObject item = new GameObject(type.ToString() + "Item");
        float y = Random.Range(minY, maxY);
        item.transform.position = new Vector3(spawnXPosition, y, 0f);
        item.transform.localScale = Vector3.one * 0.5f;

        SpriteRenderer sr = item.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 8;

        CircleCollider2D col = item.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        CollectableItem collectable = item.AddComponent<CollectableItem>();
        collectable.itemType = type;
    }
}
