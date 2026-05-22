using UnityEngine;

public class AsteroidSpawnerScript : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] asteroidPrefabs;

    [Header("Difficulty Settings")]
    public float initialSpawnRate = 1.5f;
    public float minimumSpawnRate = 0.6f;
    public int maxAsteroidsCount = 35;

    [Header("Micro-Scaling (Per Second)")]
    public float spawnRateShave = 0.008f;
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
        spawnTimer = currentSpawnRate;
    }

    void Update()
    {
        if (currentSpawnRate > minimumSpawnRate)
        {
            currentSpawnRate -= spawnRateShave * Time.deltaTime;
        }

        speedMultiplier += speedBoostPerSec * Time.deltaTime;
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            if (GameObject.FindGameObjectsWithTag("Asteroid").Length < maxAsteroidsCount)
            {
                SpawnAsteroid();
            }

            spawnTimer = currentSpawnRate;
        }
    }

    public float GetSpeedMultiplier() => speedMultiplier;

    void SpawnAsteroid()
    {
        if (asteroidPrefabs.Length < 3) return;

        float margin = 2f;
        float negativeMargin = -1.2f;

        Vector3 viewportPos = new Vector3(Random.value, Random.value, 10);
        int edge = Random.Range(0, 4);

        if (edge == 0) viewportPos.y = margin;
        else if (edge == 1) viewportPos.y = negativeMargin;
        else if (edge == 2) viewportPos.x = negativeMargin;
        else viewportPos.x = margin;

        Vector3 worldPos = Camera.main.ViewportToWorldPoint(viewportPos);
        worldPos.z = 0;

        worldPos += (Vector3)Random.insideUnitCircle * 3f;

        int selectedIndex = 0;
        float roll = Random.value;

        if (roll < 0.55f) selectedIndex = 2;
        else if (roll < 0.85f) selectedIndex = 1;
        else selectedIndex = 0;

        int asteroidLayer = LayerMask.GetMask("Asteroids");
        if (Physics2D.OverlapCircle(worldPos, 10f, asteroidLayer) != null)
        {
            return;
        }

        // Just spawn it! The asteroid's own Start() function will handle setting its speed
        GameObject newAsteroid = Instantiate(asteroidPrefabs[selectedIndex], worldPos, Quaternion.identity);

        if (GameManagerScript.Instance != null)
        {
            GameManagerScript.Instance.SpawnIndicator(newAsteroid.transform, 3 - selectedIndex);
        }
    }
}
