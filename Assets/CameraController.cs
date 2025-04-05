using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 1f;

    public Zone zone;

    private Camera cam;

    [Header("Pan Settings")]
    private Vector3 dragOrigin;
    private bool isPanning = false;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (!GameManager.Instance.isDragging)
        {
            HandlePan();
            HandleZoom();

        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;

            // Calcul max zoom
            if (zone != null && zone.zone != null)
            {
                float zoneHalfWidth = zone.zone.Width / 2f;
                float zoneHalfHeight = zone.zone.Height / 2f;
                // The half width and height of the zone can't "dépasser" idk the word
                float maxOrthographicSize = Mathf.Min(zone.zone.Height / 2f, zone.zone.Width / (2f * cam.aspect));
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxOrthographicSize);
            }
            else
            {
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, 20f);
            }

            // Calcul limit
            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth = cam.orthographicSize * cam.aspect;
            Vector3 zoneCenter = zone != null ? zone.transform.position : Vector3.zero;
            float zoneHalfWidthFinal = zone != null && zone.zone != null ? zone.zone.Width / 2f : 10f;
            float zoneHalfHeightFinal = zone != null && zone.zone != null ? zone.zone.Height / 2f : 10f;

            // Clamp cam pos
            Vector3 newPos = cam.transform.position;
            newPos.x = Mathf.Clamp(newPos.x, zoneCenter.x - (zoneHalfWidthFinal - camHalfWidth), zoneCenter.x + (zoneHalfWidthFinal - camHalfWidth));
            newPos.y = Mathf.Clamp(newPos.y, zoneCenter.y - (zoneHalfHeightFinal - camHalfHeight), zoneCenter.y + (zoneHalfHeightFinal - camHalfHeight));
            cam.transform.position = newPos;
        }
    }

    void HandlePan()
    {
        if (Input.GetMouseButtonDown(2))
        {
            isPanning = true;
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }
        if (Input.GetMouseButton(2) && isPanning)
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPos = cam.transform.position + difference;

            // Calculer les dimensions de la vue de la caméra en world units
            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth = cam.orthographicSize * cam.aspect;

            // Récupérer le centre et la moitié des dimensions de la zone
            Vector3 zoneCenter = zone.transform.position;
            float zoneHalfWidth = zone.zone.Width / 2f;
            float zoneHalfHeight = zone.zone.Height / 2f;

            // Si la vue de la caméra est plus grande que la zone, on empêche le déplacement en dehors du centre
            float minX = zoneCenter.x - Mathf.Max(0, zoneHalfWidth - camHalfWidth);
            float maxX = zoneCenter.x + Mathf.Max(0, zoneHalfWidth - camHalfWidth);
            float minY = zoneCenter.y - Mathf.Max(0, zoneHalfHeight - camHalfHeight);
            float maxY = zoneCenter.y + Mathf.Max(0, zoneHalfHeight - camHalfHeight);

            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

            cam.transform.position = newPos;
        }
        if (Input.GetMouseButtonUp(2))
        {
            isPanning = false;
        }
    }
}
