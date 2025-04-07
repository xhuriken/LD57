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

        // Charger la taille sauvegardée, sinon utiliser les valeurs par défaut
        if (PlayerPrefs.HasKey("ZoneWidth") && PlayerPrefs.HasKey("ZoneHeight"))
        {
            zone.Width = PlayerPrefs.GetFloat("ZoneWidth");
            zone.Height = PlayerPrefs.GetFloat("ZoneHeight");
            Debug.Log("Zone size loaded on Start: " + zone.Width + " x " + zone.Height);
        }
        else
        {
            zone.Width = Width;
            zone.Height = Height;
        }
    }

    private void Update()
    {
        // Modifier la taille avec H (agrandir) et G (réduire)
        if (Input.GetKeyDown(KeyCode.H) && !isInTransition)
        {
            float targetWidth = zone.Width * 1.5f;
            float targetHeight = zone.Height * 1.5f;
            StartCoroutine(SmoothResize(targetWidth, targetHeight, transitionDuration));
        }
        else if (Input.GetKeyDown(KeyCode.G) && !isInTransition)
        {
            float targetWidth = zone.Width * 0.5f;
            float targetHeight = zone.Height * 0.5f;
            StartCoroutine(SmoothResize(targetWidth, targetHeight, transitionDuration));
        }

        // Sauvegarder la taille de la zone avec K
        if (Input.GetKeyDown(KeyCode.K))
        {
            SaveZone();
        }
        // Charger la taille de la zone avec L
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadZone();
        }
    }

    public IEnumerator SmoothResize(float targetWidth, float targetHeight, float duration)
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

    public void SaveZone()
    {
        PlayerPrefs.SetFloat("ZoneWidth", zone.Width);
        PlayerPrefs.SetFloat("ZoneHeight", zone.Height);
        PlayerPrefs.Save();
        Debug.Log("Zone size saved: " + zone.Width + " x " + zone.Height);
    }

    public void LoadZone()
    {
        if (PlayerPrefs.HasKey("ZoneWidth") && PlayerPrefs.HasKey("ZoneHeight"))
        {
            float loadedWidth = PlayerPrefs.GetFloat("ZoneWidth");
            float loadedHeight = PlayerPrefs.GetFloat("ZoneHeight");
            zone.Width = loadedWidth;
            zone.Height = loadedHeight;
            Debug.Log("Zone size loaded: " + loadedWidth + " x " + loadedHeight);
        }
        else
        {
            Debug.Log("No saved zone size found.");
        }
    }
}
