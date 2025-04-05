using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool DebugMode = false;

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
        if (!DebugMode)
        {
            camControl.enabled = false;
        }
        else
        {
            Camera.main.transform.position = new Vector3(0, 0, -10);
        }
    }

    private void Update()
    {
        if(DebugMode)
            return;
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