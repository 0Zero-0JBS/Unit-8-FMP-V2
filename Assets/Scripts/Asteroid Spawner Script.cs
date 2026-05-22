using UnityEngine;

public class AsteroidSpawnerScript : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] asteroidPrefabs;

    [Header("Difficulty Settings")]
    public float initialSpawnRate = 2.0f;  // Start faster (2 seconds) so it's not boring
    public float minimumSpawnRate = 0.8f;
    public int maxAsteroidsCount = 30; 

    [Header("Micro-Scaling (Per Second)")]
    public float spawnRateShave = 0.005f;
    public float speedBoostPerSec = 0.002f;

    private float currentSpawnRate;
    private float spawnTimer;
    private float speedMultiplier = 1.0f;

    private Camera mainCamera;
    private int asteroidLayer;

    void Awake()
    {
        mainCamera = Camera.main;
        asteroidLayer = LayerMask.GetMask("Asteroids");
    }

    void Start()
    {
        currentSpawnRate = initialSpawnRate;
        spawnTimer = currentSpawnRate; // Set initial timer
    }

    void Update()
    {
        // 1. Dynamic Difficulty Scaling Calculations
        if (currentSpawnRate > minimumSpawnRate)
        {
            currentSpawnRate -= spawnRateShave * Time.deltaTime;
        }

        // Creep difficulty velocity forces upward across global frame ticks
        speedMultiplier += speedBoostPerSec * Time.deltaTime;

        // 2. Spawn Delta Timer Evaluation Loop
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            // Check active counts to prevent memory bloat over deep map spaces
            if (GameObject.FindGameObjectsWithTag("Asteroid").Length < maxAsteroidsCount)
            {
                SpawnAsteroid();
            }

            // Reset execution clock parameter metrics back to current evaluation speeds
            spawnTimer = currentSpawnRate;
        }
    }

    public float GetSpeedMultiplier() => speedMultiplier;

    void SpawnAsteroid()
    {
        if (asteroidPrefabs.Length < 3) return;

        float margin = 2.3f;
        float negativeMargin = -1.5f;

        Vector3 viewportPos = new Vector3(Random.value, Random.value, 10);
        int edge = Random.Range(0, 4);

        if (edge == 0) viewportPos.y = margin;              // Top
        else if (edge == 1) viewportPos.y = negativeMargin; // Bottom
        else if (edge == 2) viewportPos.x = negativeMargin; // Left
        else viewportPos.x = margin;                        // Right

        Vector3 worldPos = Camera.main.ViewportToWorldPoint(viewportPos);
        worldPos.z = 0;

        worldPos += (Vector3)Random.insideUnitCircle * 5f;

        int selectedIndex = 0;
        float roll = Random.value;

        if (roll < 0.55f)
        {
            selectedIndex = 2;
        }
        else if (roll < 0.85f)
        {
            selectedIndex = 1;
        }
        else
        {
            selectedIndex = 0;
        }

        // This prevents the background from blocking the spawn.
        int asteroidLayer = LayerMask.GetMask("Asteroids");
        if (Physics2D.OverlapCircle(worldPos, 10f, asteroidLayer) != null)
        {
            // If an asteroid is already there, skip this frame to avoid merging
            return;
        }

        GameObject newAsteroid = Instantiate(asteroidPrefabs[selectedIndex], worldPos, Quaternion.identity);

        if (GameManagerScript.Instance != null)
        {
            GameManagerScript.Instance.SpawnIndicator(newAsteroid.transform, 3 - selectedIndex);
        }

        Rigidbody2D rb = newAsteroid.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity *= speedMultiplier; // 6
        }
    }
}
