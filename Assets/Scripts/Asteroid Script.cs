using UnityEngine;
using System.Collections;

public class AsteroidScript : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 2f;
    public int size = 3;
    public int health;
    public int pointsValue = 100;
    public GameObject asteroidPrefab;

    [Header("Visuals")]
    public Sprite[] sizeSprites;
    private SpriteRenderer spriteRenderer;
    private bool canDespawn = false;
    private bool isDead = false;

    [Header("Indicator Settings")]
    public GameObject indicatorPrefab;

    private Rigidbody2D rb;
    [HideInInspector] public bool isAChild = false;

    private GameObject myIndicator;
    private bool hasEnteredScreen = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (size == 3) { health = 40; transform.localScale = new Vector3(70f, 70f, 1f); }
        else if (size == 2) { health = 9; transform.localScale = new Vector3(45f, 45f, 1f); }
        else { health = 2; transform.localScale = new Vector3(20f, 20f, 1f); }

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (sizeSprites.Length >= size)
            spriteRenderer.sprite = sizeSprites[size - 1];

        if (!isAChild)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector2 targetPos = player != null ? (Vector2)player.transform.position : Vector2.zero;
            Vector2 directionToPlayer = (targetPos - (Vector2)transform.position).normalized;

            float variance = Random.Range(-0.3f, 0.3f);
            Vector2 finalDirection = new Vector2(directionToPlayer.x + variance, directionToPlayer.y + variance).normalized;

            float globalSpeedMod = 1f;
            AsteroidSpawnerScript spawner = FindFirstObjectByType<AsteroidSpawnerScript>();
            if (spawner != null) globalSpeedMod = spawner.GetSpeedMultiplier();

            rb.linearVelocity = finalDirection * GetSpeedBySize() * globalSpeedMod;
        }

        Invoke("EnableDespawn", 1f);
        if (isAChild) CreateIndicator();
    }

    void Update()
    {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);

        bool isInsideScreen = screenPoint.z > 0 &&
                             screenPoint.x > 0 && screenPoint.x < 1 &&
                             screenPoint.y > 0 && screenPoint.y < 1;

        if (isInsideScreen && !hasEnteredScreen)
        {
            hasEnteredScreen = true;
            if (myIndicator != null)
            {
                Destroy(myIndicator);
            }
        }
    }

    float GetSpeedBySize()
    {
        if (size == 1) return speed * 12f;
        if (size == 2) return speed * 4f;
        return speed * 0.5f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Asteroid"))
        {
            return;
        }

        if (collision.gameObject.CompareTag("Bullet"))
        {
            TakeDamage(1);
            Destroy(collision.gameObject);
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerScript player = collision.gameObject.GetComponent<PlayerScript>();
            if (player != null) player.TakeDamage();

            Explode();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("Bullet"))
        {
            Destroy(other.gameObject);
            TakeDamage(1);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;
        health -= damageAmount;

        if (health <= 0)
        {
            if (ScoreManagerScript.Instance != null) ScoreManagerScript.Instance.AddScore(pointsValue);
            Explode();
        }
        else
        {
            // Only play hit sounds if it survives the impact damage
            if (AudioManagerScript.Instance != null)
            {
                AudioManagerScript.Instance.PlayAsteroidHit();
            }
            StartCoroutine(HitFlicker());
        }
    }

    void Explode()
    {
        if (isDead) return;
        isDead = true;

        if (AudioManagerScript.Instance != null)
        {
            AudioManagerScript.Instance.PlayAsteroidExplosion();
        }

        if (size > 1) Split();
        Destroy(gameObject);
    }

    IEnumerator HitFlicker()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    void Split()
    {
        Vector2 parentDirection = rb.linearVelocity.normalized;

        for (int i = 0; i < 2; i++)
        {
            Vector3 spawnOffset = Random.insideUnitCircle.normalized * 1.5f;

            GameObject child = Instantiate(asteroidPrefab, transform.position + spawnOffset, transform.rotation);

            AsteroidScript script = child.GetComponent<AsteroidScript>();
            Rigidbody2D childRb = child.GetComponent<Rigidbody2D>();

            if (script != null && childRb != null)
            {
                script.size = size - 1;
                script.isAChild = true;

                Vector2 splitDir = (parentDirection + (Vector2)spawnOffset.normalized).normalized;

                float globalSpeedMod = 1f;
                AsteroidSpawnerScript spawner = FindFirstObjectByType<AsteroidSpawnerScript>();
                if (spawner != null) globalSpeedMod = spawner.GetSpeedMultiplier();

                childRb.linearVelocity = splitDir * script.GetSpeedBySize() * globalSpeedMod;
            }
        }
    }

    void CreateIndicator()
    {
        if (myIndicator != null) return;

        if (GameManagerScript.Instance != null)
        {
            int trackedSizeIndex = 3 - this.size;
            myIndicator = GameManagerScript.Instance.SpawnIndicator(this.transform, trackedSizeIndex);
        }
    }

    void OnBecameVisible()
    {
        if (myIndicator != null)
        {
            Destroy(myIndicator);
        }
    }

    void OnBecameInvisible()
    {
        if (canDespawn) Destroy(gameObject);
    }

    void EnableDespawn() => canDespawn = true;
}
