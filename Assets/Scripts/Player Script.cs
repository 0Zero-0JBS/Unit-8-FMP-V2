using System.Collections;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [Header("Movement Settings")]
    public float rotationSpeed = 250f;
    public float thrustForce = 8f;
    public float reverseForce = 4f;
    public float strafeForce = 6f;

    [Header("Combat Settings")]
    public GameObject bulletPrefab;
    public Transform leftFirePoint;
    public Transform rightFirePoint;
    public float bulletSpeed = 12f;
    public float fireRate = 0.25f;

    public int maxAmmo = 30;
    public float reloadTime = 0.75f;
    private int currentAmmo;
    private bool isReloading = false;

    private float nextFireTime = 0f;
    private bool useLeftCannon = true;
    private Rigidbody2D rb;
    private bool wasMovingLastFrame;

    [Header("Lives & Respawn")]
    public int lives = 3;
    public float respawnTime = 2f;
    private bool isInvulnerable = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        currentAmmo = maxAmmo;
    }

    void Start()
    {
        rb.gravityScale = 0f;
        rb.angularDamping = 2f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (ScoreManagerScript.Instance != null)
        {
            ScoreManagerScript.Instance.UpdateHUD(lives, currentAmmo, maxAmmo, false);
        }
        else
        {
            Debug.LogWarning("PlayerScript couldn't find ScoreManagerScript during Start!");
        }
    }

    void Update()
    {
        // Manual Reload trigger
        if ((Input.GetKeyDown(KeyCode.R) || Input.GetButtonDown("Fire2")) && currentAmmo < maxAmmo && !isReloading)
        {
            StartCoroutine(Reload());
        }

        float rightTrigger = Input.GetAxis("RightTriggerFire");

        // Only shoot if not reloading
        if ((Input.GetMouseButton(0) || Input.GetButton("Fire1") || rightTrigger > 0.1f) && !isReloading)
        {
            if (Time.time >= nextFireTime)
            {
                if (currentAmmo > 0)
                {
                    Shoot();
                    nextFireTime = Time.time + fireRate;
                }
                else
                {
                    StartCoroutine(Reload());
                }
            }
        }

        if (Input.GetButtonDown("Jump") && currentAmmo < maxAmmo && !isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    void FixedUpdate()
    {
        float leftStickHorizontal = Input.GetAxis("Horizontal");
        float leftStickVertical = Input.GetAxis("Vertical");

        float rightStickHorizontal = 0f;
        float rightStickVertical = 0f;

        try
        {
            rightStickHorizontal = Input.GetAxis("RightStickX");
            rightStickVertical = Input.GetAxis("RightStickY");
        }
        catch (System.ArgumentException)
        {
            Debug.LogWarning("Input Axis 'RightStickX' or 'RightStickY' is missing in Project Settings!");
        }

        bool holdingReverseButton = Input.GetButton("Jump");

        if (leftStickHorizontal != 0)
        {
            float rotationAmount = -leftStickHorizontal * rotationSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation + rotationAmount);
        }

        Vector2 engineForce = Vector2.zero;

        if (holdingReverseButton)
        {
            engineForce += (Vector2)transform.up * -1f * reverseForce;
        }

        if (leftStickVertical > 0 && !holdingReverseButton)
        {
            engineForce += (Vector2)transform.up * leftStickVertical * thrustForce;
        }

        // Pushes the ship relative to its own wings, ignoring the left stick
        if (rightStickHorizontal != 0)
        {
            // Strafe left/right along your local horizontal wing axis
            engineForce += (Vector2)transform.right * rightStickHorizontal * strafeForce;
        }

        rb.AddForce(engineForce);

        if (AudioManagerScript.Instance != null)
        {
            bool isApplyingPower = engineForce.sqrMagnitude > 0.01f;

            AudioManagerScript.Instance.SetThrustVolume(isApplyingPower);
        }
    }

    void LateUpdate() { ScreenWrap(); }

    void ScreenWrap()
    {
        if (Camera.main == null) return;
        float camHeight = Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;
        Vector3 camPos = Camera.main.transform.position;

        Vector2 newPos = rb.position;
        float buffer = 1.0f;

        if (rb.position.x > camPos.x + camWidth + buffer) newPos.x = camPos.x - camWidth - buffer + 0.1f;
        else if (rb.position.x < camPos.x - camWidth - buffer) newPos.x = camPos.x + camWidth + buffer - 0.1f;

        if (rb.position.y > camPos.y + camHeight + buffer) newPos.y = camPos.y - camHeight - buffer + 0.1f;
        else if (rb.position.y < camPos.y - camHeight - buffer) newPos.y = camPos.y + camHeight + buffer - 0.1f;

        rb.position = newPos;
    }

    void Shoot()
    {
        if (bulletPrefab == null || leftFirePoint == null || rightFirePoint == null) return;

        Transform currentPoint = useLeftCannon ? leftFirePoint : rightFirePoint;
        GameObject bullet = Instantiate(bulletPrefab, currentPoint.position, transform.rotation);

        Rigidbody2D brb = bullet.GetComponent<Rigidbody2D>();
        if (brb != null)
        {
            brb.linearVelocity = transform.up * bulletSpeed;
        }

        currentAmmo--;
        if (ScoreManagerScript.Instance != null)
        { 
            ScoreManagerScript.Instance.UpdateHUD(lives, currentAmmo, maxAmmo, false);
        }
        useLeftCannon = !useLeftCannon;

        if (AudioManagerScript.Instance != null)
        {
            AudioManagerScript.Instance.PlaySFX(AudioManagerScript.Instance.laserSound);
        }

        if (AudioManagerScript.Instance != null)
        {
            AudioManagerScript.Instance.PlayLaserFire(); // Tells the pool to play a laser with fun random pitch shifts!
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;

        if (AudioManagerScript.Instance != null)
        {
            AudioManagerScript.Instance.PlayPlayerReload();
        }

        if (AudioManagerScript.Instance != null)
        {
            AudioManagerScript.Instance.PlaySFX(AudioManagerScript.Instance.reloadSound);
        }

        ScoreManagerScript.Instance.UpdateHUD(lives, currentAmmo, maxAmmo, true);
        Debug.Log("Reloading...");

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("Reload Complete!");
        ScoreManagerScript.Instance.UpdateHUD(lives, currentAmmo, maxAmmo, false);
    }

    public void TakeDamage()
    {
        if (isInvulnerable) return;
        lives--;

        ScoreManagerScript.Instance.UpdateHUD(lives, currentAmmo, maxAmmo, false);

        if (lives <= 0)
        {
            Debug.Log("GAME OVER");
            ScoreManagerScript.Instance.StopTimerPermanently();
            GameManagerScript.Instance.ShowGameOver();
        }
        else
        {
            StartCoroutine(RespawnSequence());
        }
    }

    System.Collections.IEnumerator RespawnSequence()
    {
        isInvulnerable = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = Vector3.zero;
        playerCollider.enabled = false;

        if (AudioManagerScript.Instance != null)
        {
            AudioManagerScript.Instance.PlayPlayerRespawn();
        }

        float timer = 0;
        while (timer < respawnTime)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        spriteRenderer.enabled = true;
        playerCollider.enabled = true;
        isInvulnerable = false;
    }
}
