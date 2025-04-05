using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool isDragging = false;
    public bool menuShown = false;
    public bool isStarted = false;

    private CameraController camControl;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
            camControl = Camera.main.GetComponent<CameraController>();
            camControl.enabled = false;
    }

    private void Update()
    {
        if (menuShown && camControl.enabled)
        {
            camControl.enabled = false;
        }
        else if (!menuShown && !camControl.enabled)
        {
            camControl.enabled = true;
        }
    }
}