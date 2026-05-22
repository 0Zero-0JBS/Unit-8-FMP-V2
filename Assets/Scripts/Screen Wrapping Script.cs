using UnityEngine;

public class ScreenWrappingScript : MonoBehaviour
{
    private Camera cam;
    private float screenWidth;
    private float screenHeight;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;
        screenHeight = 2f * cam.orthographicSize;
        screenWidth = screenHeight * cam.aspect;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = transform.position;
        // Horizontal wrap
        if (pos.x > screenWidth / 2) pos.x = -screenWidth / 2;
        else if (pos.x < -screenWidth / 2) pos.x = screenWidth / 2;

        // Vertical wrap
        if (pos.y > screenHeight / 2) pos.y = -screenHeight / 2;
        else if (pos.y < -screenHeight / 2) pos.y = screenHeight / 2;

        transform.position = pos;
    }
}
