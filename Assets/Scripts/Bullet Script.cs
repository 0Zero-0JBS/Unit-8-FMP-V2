using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public float speed = 15f;
    public string targetTag = "Asteroid";
    public float lifetime = 4f;
    private Rigidbody2D rb;
    private bool hasHitSomething = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (rb.linearVelocity.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = transform.up * speed;
        }

        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            AsteroidScript asteroid = collision.gameObject.GetComponent<AsteroidScript>();
            if (asteroid != null)
            {
                asteroid.TakeDamage(1);
            }

            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            return;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHitSomething) return;

        if (other.CompareTag(targetTag))
        {
            hasHitSomething = true;

            AsteroidScript asteroid = other.GetComponent<AsteroidScript>();
            if (asteroid != null)
            {
                asteroid.TakeDamage(1);
            }

            Destroy(gameObject);
        }
        else if (other.CompareTag("Player"))
        {
            return;
        }
        else if (!other.CompareTag("Bullet"))
        {
            hasHitSomething = true;
            Destroy(gameObject);
        }
    }
}
