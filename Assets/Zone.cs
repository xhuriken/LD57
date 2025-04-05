using Shapes;
using System.Collections;
using UnityEngine;

public class Zone : MonoBehaviour
{
    public float Width = 35.5f;
    public float Height = 20f;
    public Rectangle zone;

    public float transitionDuration = 0.5f;
    private bool isInTransition = false;

    private void Start()
    {
        zone = GetComponent<Rectangle>();
        zone.Width = Width;
        zone.Height = Height;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) && !isInTransition)
        {
            // + 50%
            float targetWidth = zone.Width * 1.5f;
            float targetHeight = zone.Height * 1.5f;
            StartCoroutine(SmoothResize(targetWidth, targetHeight, transitionDuration));
        }
        else if (Input.GetKeyDown(KeyCode.G) && !isInTransition)
        {
            // - 50%
            float targetWidth = zone.Width * 0.5f;
            float targetHeight = zone.Height * 0.5f;
            StartCoroutine(SmoothResize(targetWidth, targetHeight, transitionDuration));
        }
    }

    IEnumerator SmoothResize(float targetWidth, float targetHeight, float duration)
    {
        isInTransition = true;
        float elapsed = 0f;
        float startWidth = zone.Width;
        float startHeight = zone.Height;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            zone.Width = Mathf.Lerp(startWidth, targetWidth, elapsed / duration);
            zone.Height = Mathf.Lerp(startHeight, targetHeight, elapsed / duration);
            yield return null;
        }

        zone.Width = targetWidth;
        zone.Height = targetHeight;
        isInTransition = false;
    }
}
