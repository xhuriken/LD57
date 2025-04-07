using Shapes;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using static RedBall;

public class WhiteBall : MonoBehaviour, ISaveData
{
    [Header("Properties")]
    [SerializeField] private int duplicateCount = 3;
    public float force = 2f;
    private int clickCount = 0;  // Compteur privé

    // Propriété pour accéder au compteur de clics (utilisée pour la sauvegarde via ISaveData)
    public int ClickCount
    {
        get { return clickCount; }
        set { clickCount = value; }
    }

    // Composants
    private Animator m_animator;
    private Rigidbody2D m_rb;
    private CircleCollider2D m_cc;
    private Vector3 dragOffset;
    private Data m_data;

    [Header("Utils")]
    public bool isClickable = true;

    [Header("Particules/SFX")]
    public AudioClip as_duplicate;
    public AudioClip as_click;
    public GameObject duplicateParticules;
    public GameObject clickParticules;
    private AudioSource m_audioSource;

    // Machine à états
    private enum FirstBallState { Idle, Click, Duplicate, Drag }
    private FirstBallState currentState = FirstBallState.Idle;

    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_cc = GetComponent<CircleCollider2D>();
        m_audioSource = GetComponent<AudioSource>();
        m_rb = GetComponent<Rigidbody2D>();
        m_data = GetComponent<Data>();
    }

    void Update()
    {
        switch (currentState)
        {
            case FirstBallState.Idle:
                if (!isClickable) return;
                if (GameManager.Instance.menuShown) return;

                if (isClickable && Input.GetMouseButtonDown(0) && IsMouseOver() && !GameManager.Instance.isDragging)
                {
                    clickCount++;
                    if (clickCount >= duplicateCount)
                    {
                        clickCount = 0;
                        currentState = FirstBallState.Duplicate;
                        m_audioSource.PlayOneShot(as_duplicate);
                        m_animator.SetTrigger("Duplicate");
                     
                    
                    }
                    else
                    {
                        currentState = FirstBallState.Click;
                        m_audioSource.PlayOneShot(as_click);
                        m_animator.SetTrigger("Click");
                        Instantiate(clickParticules, transform.position, Quaternion.identity);
                    }
                }
                if (Input.GetMouseButtonDown(1) && IsMouseOver())
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    dragOffset = transform.position - (Vector3)mouseWorldPos;
                    m_rb.velocity = Vector2.zero;
                    GameManager.Instance.isDragging = true;
                    currentState = FirstBallState.Drag;
                }
                m_rb.velocity *= 0.99f;
                if (m_rb.velocity.magnitude < 0.01f)
                    m_rb.velocity = Vector2.zero;
                break;

            case FirstBallState.Click:
                currentState = FirstBallState.Idle;
                break;

            case FirstBallState.Duplicate:
                currentState = FirstBallState.Idle;
                break;

            case FirstBallState.Drag:
                if (Input.GetMouseButton(1))
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    transform.position = mouseWorldPos + (Vector2)dragOffset;
                    m_rb.velocity = Vector2.zero;
                }
                if (Input.GetMouseButtonUp(1))
                {
                    currentState = FirstBallState.Idle;
                    GameManager.Instance.isDragging = false;
                }
                break;
        }
    }

    /// <summary>
    /// Retourne true si la souris est sur l'objet.
    /// </summary>
    public bool IsMouseOver()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            return false;
        if (col is CircleCollider2D circle)
        {
            float rad = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            return Vector2.Distance(transform.position, mousePos) <= rad;
        }
        else
        {
            return col.OverlapPoint(mousePos);
        }
    }

    /// <summary>
    /// Augmente la taille de la zone (bordure) de 1.5 fois.
    /// </summary>
    private void IncreaseZoneBorder()
    {
        Zone zone = FindObjectOfType<Zone>();
        if (zone != null)
        {
            float targetWidth = zone.zone.Width * 1.5f;
            float targetHeight = zone.zone.Height * 1.5f;
            StartCoroutine(zone.SmoothResize(targetWidth, targetHeight, zone.transitionDuration));
            Debug.Log("Zone border increased to: " + targetWidth + " x " + targetHeight);
        }
        else
        {
            Debug.LogWarning("Zone not found.");
        }
    }

    /// <summary>
    /// Fonction pour détruire cet objet.
    /// </summary>
    public void DestroyThisObject()
    {
        Destroy(gameObject);
    }

    #region Save/Load Functionality
    /// <summary>
    /// Retourne les données de sauvegarde de cette WhiteBall.
    /// Cette méthode crée et remplit un objet TransformData contenant la position, rotation, échelle, clickCount, etc.
    /// Assurez-vous que la classe TransformData est définie dans votre système de sauvegarde.
    /// </summary>
    public TransformData GetSaveData()
    {
        TransformData data = new TransformData();
        SaveableObject so = GetComponent<SaveableObject>();
        if (so != null)
        {
            data.id = so.UniqueId;
            data.prefabPath = so.PrefabPath;
        }
        data.position = transform.position;
        data.rotation = transform.rotation;
        data.scale = transform.localScale;
        data.clickCount = this.ClickCount;
        return data;
    }

    /// <summary>
    /// Charge les données de sauvegarde dans cette WhiteBall.
    /// </summary>
    public void LoadSaveData(TransformData data)
    {
        transform.position = data.position;
        transform.rotation = data.rotation;
        transform.localScale = data.scale;
        this.ClickCount = data.clickCount;
        SaveableObject so = GetComponent<SaveableObject>();
        if (so != null)
        {
            so.SetUniqueId(data.id);
        }
    }
    #endregion
}
