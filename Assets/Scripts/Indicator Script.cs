using UnityEngine;

[RequireComponent(typeof(RectTransform))] // Automatically guarantees structural component integrity
public class IndicatorScript : MonoBehaviour
{
    public Transform target;
    private RectTransform rectTransform;
    public float margin = 10f;

    private int targetSize;
    private Rigidbody2D targetRb;
    private Camera mainCamera; // Optimized global cache pointer

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
    }

    public void Setup(Transform asteroidTransform, int size)
    {
        target = asteroidTransform;
        targetSize = size;
        if (asteroidTransform != null)
        {
            targetRb = asteroidTransform.GetComponent<Rigidbody2D>();
        }
    }

    void Update()
    {
        // Safe Cleanup: Erase the arrow indicator instantly if its target asteroid explodes
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Optimization: Safeguard camera references without causing CPU stutter logs
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        // FIX: Project calculations strictly to a 2D plane to ignore camera depth offsets (Z = -10)
        Vector2 cameraPlanarPos = new Vector2(mainCamera.transform.position.x, mainCamera.transform.position.y);
        Vector2 targetPlanarPos = new Vector2(target.position.x, target.position.y);

        // Fixed: Swapped costly Camera.main searches with your high-performance mainCamera cache
        Vector2 toPlayer = (cameraPlanarPos - targetPlanarPos).normalized;
        Vector2 movementDir = (targetRb != null) ? targetRb.linearVelocity.normalized : Vector2.zero; // Fixed API call
        float directionDot = Vector2.Dot(toPlayer, movementDir);

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(target.position);

        bool isVisible = viewportPos.x > -0.1f && viewportPos.x < 1.1f &&
                         viewportPos.y > -0.1f && viewportPos.y < 1.1f &&
                         viewportPos.z > 0f;

        // Erase the indicator when the asteroid enters the viewport safely
        if (isVisible)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);
        if (screenPos.z < 0) screenPos *= -1;

        // Scale bounding margins based on screen pixel structures
        float halfWidth = (rectTransform.rect.width * rectTransform.localScale.x) / 2f;
        float halfHeight = (rectTransform.rect.height * rectTransform.localScale.y) / 2f;

        // Clamp coordinates cleanly to keep indicators bounded to the monitor edges
        float x = Mathf.Clamp(screenPos.x, halfWidth + margin, Screen.width - (halfWidth + margin));
        float y = Mathf.Clamp(screenPos.y, halfHeight + margin, Screen.height - (halfHeight + margin));

        rectTransform.position = new Vector3(x, y, 0);

        // Point the arrow sprite towards the asteroid's active flight trajectory direction
        if (targetRb != null && targetRb.linearVelocity.magnitude > 0.1f) // Fixed API call
        {
            Vector2 velocity = targetRb.linearVelocity; // Fixed API call
            float trajectoryAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            rectTransform.rotation = Quaternion.Euler(0, 0, trajectoryAngle - 90);
        }
    }
}
