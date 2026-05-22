using System.Collections;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [Header("Movement Settings")]
    public float rotationSpeed = 250f; // Used specifically for gamepad turning fallback
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
    private Camera mainCamera;

    [Header("Lives & Respawn")]
    public int lives = 3;
    public float respawnTime = 2f;
    private bool isInvulnerable = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;

    // Unified physics storage variables
    private float inputStrafe = 0f;
    private float inputThrustForward = 0f;
    private float inputThrustReverse = 0f;

    // Rotation tracking variables
    private Vector2 gamepadLookDirection = Vector2.zero;
    private bool isUsingGamepad = false;
    private float targetAngle = 0f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        currentAmmo = maxAmmo;
        mainCamera = Camera.main;
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
    }

    void Update()
    {
        inputStrafe = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        inputThrustForward = verticalInput > 0 ? verticalInput : 0f;
        inputThrustReverse = verticalInput < 0 ? Mathf.Abs(verticalInput) : 0f;

        float rightStickX = 0f;
        float rightStickY = 0f;

        try
        {
            rightStickX = Input.GetAxis("RightStickX");
            rightStickY = Input.GetAxis("RightStickY");
        }
        catch (System.ArgumentException)
        {
            // Safeguard if project settings are missing the axes
        }

        Vector2 rightStickInput = new Vector2(rightStickX, rightStickY);

        // Detect device based on current input activity
        if (rightStickInput.sqrMagnitude > 0.1f)
        {
            isUsingGamepad = true;
            gamepadLookDirection = rightStickInput;
        }
        else if (Input.GetAxisRaw("Mouse X") != 0 || Input.GetAxisRaw("Mouse Y") != 0)
        {
            isUsingGamepad = false;
        }

        float rightTrigger = 0f;
        try { rightTrigger = Input.GetAxis("RightTriggerFire"); } catch { }

        bool shootingInputPressed = Input.GetMouseButton(0) || Input.GetButton("Fire1") || rightTrigger > 0.1f;

        if (shootingInputPressed && !isReloading)
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

        // Manual Reload triggers (R key, Face Button down, or Controller Jump setup)
        bool reloadButtonPressed = Input.GetKeyDown(KeyCode.R) || Input.GetButtonDown("Fire2") || Input.GetButtonDown("Jump");
        if (reloadButtonPressed && currentAmmo < maxAmmo && !isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    void FixedUpdate()
    {
        if (isUsingGamepad)
        {
            // Point relative to the angle of the joystick
            targetAngle = Mathf.Atan2(gamepadLookDirection.y, gamepadLookDirection.x) * Mathf.Rad2Deg - 90f;
            rb.MoveRotation(targetAngle);
        }
        else if (mainCamera != null)
        {
            // Point relative to mouse pointer positions
            Vector3 mouseScreenPos = Input.mousePosition;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, transform.position.z - mainCamera.transform.position.z));
            Vector2 lookDirection = (Vector2)mouseWorldPos - (Vector2)transform.position;

            targetAngle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg - 90f;
            rb.MoveRotation(targetAngle);
        }

        Vector2 engineForce = Vector2.zero;

        // Forward
        if (inputThrustForward > 0)
        {
            engineForce += (Vector2)transform.up * inputThrustForward * thrustForce;
        }

        // Reverse (Now correctly sharing the Vertical Axis / S key)
        if (inputThrustReverse > 0)
        {
            engineForce += (Vector2)transform.up * -1f * inputThrustReverse * reverseForce;
        }

        // Strafe
        if (Mathf.Abs(inputStrafe) > 0.01f)
        {
            engineForce += (Vector2)transform.right * inputStrafe * strafeForce;
        }

        rb.AddForce(engineForce);

        // Audio Handler Link
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
