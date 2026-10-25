using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformSpawner : MonoBehaviour
{
    [Header("Platform Settings")]
    [Tooltip("The Platform prefab to spawn.")]
    public GameObject platformPrefab;

    [Tooltip("Vertical height added for each new platform.")]
    public float heightStep = 1.0f;

    [Tooltip("Horizontal forward distance between consecutive platforms.")]
    public float forwardDistance = 2.0f;

    [Tooltip("Maximum random horizontal offset to the sides (left/right).")]
    public float sideSpread = 1.0f;

    [Tooltip("How many platforms ahead should be pre-spawned/maintained.")]
    public int platformsAhead = 5;

    [Header("Platform Clearance & Spacing")]
    [Tooltip("Minimum horizontal clearance distance from the player to prevent any platform from ever spawning directly overhead.")]
    public float minOverheadClearance = 1.8f;

    [Header("Timer & Destruction Settings")]
    [Tooltip("Number of jumps player must make before platforms start getting timed destruction (e.g. 3).")]
    public int jumpsBeforeDestroy = 3;

    [Tooltip("Seconds before the previous platform is destroyed once timed.")]
    public float destroyDelay = 5.0f;

    [Header("Starting Ground / Plane")]
    [Tooltip("Reference to the starting Plane/Ground to destroy when player jumps off. Auto-finds 'Plane' if null.")]
    public GameObject startingGround;

    [Tooltip("Delay in seconds before the starting ground is destroyed after player first jumps.")]
    public float startingGroundDestroyDelay = 3.0f;

    [Header("Spawn Direction Mode")]
    [Tooltip("If true, new platforms generate in the direction the player is facing/moving; otherwise along a fixed direction.")]
    public bool followPlayerDirection = true;

    [Tooltip("Fixed direction used when followPlayerDirection is false.")]
    public Vector3 fixedDirection = Vector3.forward;

    [Header("First Platform Reference (Optional)")]
    [Tooltip("Assign an existing platform in the scene as the starting point, or leave empty to auto-detect/spawn relative to player.")]
    public Transform initialPlatform;

    // Track platforms and their destruction states
    private readonly List<GameObject> spawnedPlatforms = new List<GameObject>();
    private readonly HashSet<GameObject> scheduledDestructions = new HashSet<GameObject>();

    private Vector3 lastPlatformPosition;
    private Vector3 lastPlatformForward = Vector3.forward;
    private int totalSpawnedCount = 0;
    private int jumpCount = 0;

    private void Start()
    {
        // Auto-locate starting Plane if not set
        if (startingGround == null)
        {
            startingGround = GameObject.Find("Plane");
        }

        // Safe check for initialPlatform
        if (initialPlatform != null)
        {
            lastPlatformPosition = initialPlatform.position;
            lastPlatformForward = initialPlatform.forward;
            spawnedPlatforms.Add(initialPlatform.gameObject);
        }
        else
        {
            GameObject existing = GameObject.Find("Platform");
            if (existing != null)
            {
                initialPlatform = existing.transform;
                lastPlatformPosition = existing.transform.position;
                lastPlatformForward = existing.transform.forward;
                spawnedPlatforms.Add(existing);
            }
            else
            {
                lastPlatformPosition = transform.position + (transform.forward * forwardDistance) + (Vector3.up * heightStep);
                lastPlatformForward = transform.forward;
            }
        }

        // Pre-spawn platforms ahead so player has initial platforms to jump onto
        for (int i = 0; i < platformsAhead; i++)
        {
            SpawnNextPlatform();
        }
    }

    /// <summary>
    /// Called when the player jumps.
    /// Spawns the next platform, destroys the starting Plane, and starts destruction of previous platforms.
    /// </summary>
    public void OnPlayerJump()
    {
        jumpCount++;

        // 1. Destroy starting Plane/Ground on first jump after a short delay
        // (Ensure we never accidentally destroy a camera or audio listener)
        if (jumpCount == 1 && startingGround != null && startingGround.GetComponent<Camera>() == null)
        {
            StartCoroutine(DestroyPlatformAfterDelay(startingGround, startingGroundDestroyDelay));
        }

        // 2. Spawn a new platform 1 unit above
        SpawnNextPlatform();

        // 3. For the first 3 jumps, platforms are kept safe so the player can jump on them.
        // Once jumpCount >= 4: the previous platform (the one jumped from) gets a 5-second timer.
        // Earlier platforms that were protected (platforms 0, 1, 2) also receive their destruction countdown once passed.
        if (jumpCount >= jumpsBeforeDestroy + 1)
        {
            // Destroy previous platform(s) that are now behind the player
            int previousPlatformIndex = jumpCount - 2; // Index of the platform just departed

            for (int i = 0; i <= previousPlatformIndex; i++)
            {
                if (i >= 0 && i < spawnedPlatforms.Count)
                {
                    GameObject p = spawnedPlatforms[i];
                    if (p != null && !scheduledDestructions.Contains(p))
                    {
                        scheduledDestructions.Add(p);
                        StartCoroutine(DestroyPlatformAfterDelay(p, destroyDelay));
                    }
                }
            }
        }
    }

    private IEnumerator DestroyPlatformAfterDelay(GameObject obj, float delay)
    {
        if (obj == null) yield break;

        // Visual warning: start blinking in the last 2 seconds
        float blinkStartTime = Mathf.Max(0f, delay - 2f);
        yield return new WaitForSeconds(blinkStartTime);

        float remainingTime = delay - blinkStartTime;
        Renderer rend = obj != null ? obj.GetComponentInChildren<Renderer>() : null;
        Color originalColor = rend != null && rend.material != null ? rend.material.color : Color.white;

        float timer = 0f;
        while (timer < remainingTime)
        {
            if (obj == null) yield break;
            timer += Time.deltaTime;

            if (rend != null && rend.material != null)
            {
                // Flashing red effect to warn the player it's about to collapse/vanish
                float flash = Mathf.PingPong(timer * 8f, 1f);
                rend.material.color = Color.Lerp(originalColor, Color.red, flash);
            }

            yield return null;
        }

        if (obj != null)
        {
            Destroy(obj);
        }
    }

    public void SpawnNextPlatform()
    {
        if (platformPrefab == null)
        {
            platformPrefab = Resources.Load<GameObject>("Platform");
            if (platformPrefab == null)
            {
                Debug.LogWarning("[PlatformSpawner] No platformPrefab assigned!");
                return;
            }
        }

        Vector3 forwardDir;
        if (followPlayerDirection && transform.forward.sqrMagnitude > 0.01f)
        {
            forwardDir = transform.forward;
            forwardDir.y = 0f;
            forwardDir.Normalize();
        }
        else if (lastPlatformForward.sqrMagnitude > 0.01f)
        {
            forwardDir = lastPlatformForward;
        }
        else
        {
            forwardDir = fixedDirection.normalized;
        }

        if (forwardDir == Vector3.zero)
        {
            forwardDir = Vector3.forward;
        }

        Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
        float randomSide = Random.Range(-sideSpread, sideSpread);

        Vector3 nextPosition = lastPlatformPosition
                               + (forwardDir * forwardDistance)
                               + (rightDir * randomSide)
                               + (Vector3.up * heightStep);

        // --- Overhead Collision Prevention ---
        // Ensure new platforms are never placed directly above the player's current head space.
        // If the platform would be directly above the player (within minOverheadClearance horizontally),
        // push it forward so the player has full clear jump headroom.
        Vector2 playerFlatPos = new Vector2(transform.position.x, transform.position.z);
        Vector2 nextFlatPos = new Vector2(nextPosition.x, nextPosition.z);
        float flatDistToPlayer = Vector2.Distance(playerFlatPos, nextFlatPos);

        if (flatDistToPlayer < minOverheadClearance)
        {
            float pushNeeded = minOverheadClearance - flatDistToPlayer + 0.5f;
            nextPosition += forwardDir * pushNeeded;
        }

        Quaternion nextRotation = Quaternion.LookRotation(forwardDir, Vector3.up);

        GameObject newPlatform = Instantiate(platformPrefab, nextPosition, nextRotation);
        newPlatform.name = $"Platform_{++totalSpawnedCount}";

        // Attach Word letter item if eligible
        if (WordCollectManager.Instance != null)
        {
            if (WordCollectManager.Instance.TryGetNextLetterToSpawn(totalSpawnedCount, out char letter, out int letterIndex))
            {
                WordCollectManager.Instance.AttachLetterToPlatform(newPlatform, letter, letterIndex);
            }
        }

        spawnedPlatforms.Add(newPlatform);
        lastPlatformPosition = nextPosition;
        lastPlatformForward = forwardDir;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (spawnedPlatforms.Count > 0 && spawnedPlatforms[spawnedPlatforms.Count - 1] != null)
        {
            Vector3 lastPos = spawnedPlatforms[spawnedPlatforms.Count - 1].transform.position;
            Vector3 predictedNext = lastPos + (transform.forward * forwardDistance) + (Vector3.up * heightStep);
            Gizmos.DrawWireCube(predictedNext, new Vector3(1f, 0.2f, 2f));
            Gizmos.DrawLine(lastPos, predictedNext);
        }
    }
}
