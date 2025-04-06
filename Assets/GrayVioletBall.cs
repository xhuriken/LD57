using Febucci.UI;
using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RedBall;

public class GrayVioletBall : MonoBehaviour, ICraftableBall
{
    // Components
    private Animator m_animator;
    private Rigidbody2D m_rb;
    private CircleCollider2D m_cc;
    private Vector3 dragOffset;

    [Header("Utils")]
    public bool isDragged = false;

    [Header("Particules/SFX")]
    public AudioClip as_duplicate;
    public AudioClip as_click;
    public GameObject duplicateParticules;
    public GameObject clickParticules;
    private AudioSource m_audioSource;

    private enum GrayBallState { Idle, Drag, Inhale, Craft, Crafting }
    private GrayBallState currentState = GrayBallState.Idle;

    [Header("Idle Movement")]
    private Vector3 idleCenter;
    private float idleRadius = 3f;
    private float idleSpeed = 50f; // degrés/seconde
    private float currentAngle = 0f;

    public GameObject pointEffector;
    public GameObject selectionIndicator;
    public GameObject SpaceCraftHelp;
    public string CraftBallType { get { return "GrayVioletBall"; } }

    void Start()
    {
        SpaceCraftHelp = GameObject.FindGameObjectWithTag("TextHelpCraft");
        if (pointEffector != null)
        {
            pointEffector.GetComponent<PointEffector2D>().enabled = false;
            pointEffector.GetComponent<CircleCollider2D>().enabled = false;
        }
        m_animator = GetComponent<Animator>();
        m_cc = GetComponent<CircleCollider2D>();
        m_audioSource = GetComponent<AudioSource>();
        m_rb = GetComponent<Rigidbody2D>();

        GameObject zoneObject = GameObject.Find("Zone");
        if (zoneObject != null)
        {
            Zone zoneScript = zoneObject.GetComponent<Zone>();
            if (zoneScript != null)
            {
                float width = zoneScript.Width;
                float height = zoneScript.Height;
                int side = Random.Range(0, 4);
                Vector3 spawnPoint = Vector3.zero;
                switch (side)
                {
                    case 0: // haut
                        spawnPoint = new Vector3(Random.Range(-width / 2f, width / 2f), height / 2f, 0);
                        break;
                    case 1: // bas
                        spawnPoint = new Vector3(Random.Range(-width / 2f, width / 2f), -height / 2f, 0);
                        break;
                    case 2: // gauche
                        spawnPoint = new Vector3(-width / 2f, Random.Range(-height / 2f, height / 2f), 0);
                        break;
                    case 3: // droite
                        spawnPoint = new Vector3(width / 2f, Random.Range(-height / 2f, height / 2f), 0);
                        break;
                }
                idleCenter = spawnPoint;
                transform.position = idleCenter + (Vector3)(Vector2.right * idleRadius);
                currentAngle = 0f;
            }
            else
            {
                Debug.LogError("[GrayVioletBall] Aucun script Zone trouvé sur l'objet 'Zone'.");
            }
        }
        else
        {
            Debug.LogError("[GrayVioletBall] Aucun GameObject nommé 'Zone' trouvé dans la scène.");
        }
    }

    void Update()
    {
        if (GameManager.Instance.CraftMode)
        {
            ClickEvent();
        }

        switch (currentState)
        {
            case GrayBallState.Idle:
                if (!GameManager.Instance.CraftMode)
                {
                    IdleMovement();
                }
                break;
            case GrayBallState.Drag:
                if (Input.GetMouseButton(1))
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    transform.position = mouseWorldPos + (Vector2)dragOffset;
                    m_rb.velocity = Vector2.zero;

                    if (GameManager.Instance.CraftMode && GameManager.Instance.currentCraftModeCollider != null)
                    {
                        float dist = Vector2.Distance(GameManager.Instance.currentCraftModeCollider.transform.position, transform.position);
                        float radius = GameManager.Instance.currentCraftModeCollider.GetComponent<CraftModeCollider>().radius;
                        if (dist > radius)
                        {
                            Debug.Log("[GrayVioletBall] Dragged out of craft area, deselected.");
                            if (selectionIndicator != null)
                                selectionIndicator.SetActive(false);
                            GameManager.Instance.UnregisterSelectedBall(this);
                        }
                    }
                }
                if (Input.GetMouseButtonUp(1))
                {
                    currentState = (GameManager.Instance.CraftMode ? GrayBallState.Craft : (m_rb.isKinematic ? GrayBallState.Inhale : GrayBallState.Idle));
                    GameManager.Instance.isDragging = false;
                    isDragged = false;
                }
                break;
            case GrayBallState.Inhale:
                m_rb.velocity = Vector2.zero;
                m_rb.isKinematic = true;
                if (pointEffector != null)
                {
                    var effector = pointEffector.GetComponent<PointEffector2D>();
                    if (effector != null && !effector.enabled)
                        effector.enabled = true;
                    var effectorCollider = pointEffector.GetComponent<CircleCollider2D>();
                    if (effectorCollider != null && !effectorCollider.enabled)
                        effectorCollider.enabled = true;
                }
                if (!GameManager.Instance.NeverCraft)
                {
                    SpaceCraftHelp.GetComponent<TypewriterByCharacter>().ShowText("Press Space for Craft");
                    GameManager.Instance.NeverCraft = true;
                }

                if(GameManager.Instance.NeverCraft && Input.GetKeyDown(KeyCode.Space))
                {
                    SpaceCraftHelp.GetComponent<TypewriterByCharacter>().StartDisappearingText();
                }

                if (Input.GetMouseButtonDown(1) && IsMouseOver())
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    dragOffset = transform.position - (Vector3)mouseWorldPos;
                    m_rb.velocity = Vector2.zero;
                    GameManager.Instance.isDragging = true;
                    currentState = GrayBallState.Drag;
                    isDragged = true;
                }
                
                break;
            case GrayBallState.Craft:
                m_rb.velocity = Vector2.zero;
                break;
            case GrayBallState.Crafting:
                break;
        }
    }

    private void IdleMovement()
    {
        currentAngle += idleSpeed * Time.deltaTime;
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * idleRadius;
        transform.position = idleCenter + offset;
    }

    private void ClickEvent()
    {
        if (GameManager.Instance.menuShown)
            return;
        if (GameManager.Instance.CraftMode)
        {
            if (IsMouseOver() && Input.GetMouseButtonDown(0))
            {
                ToggleCraftingSelection();
            }
            if (IsMouseOver() && Input.GetMouseButtonDown(1) && !HasCraftModeCollider())
            {
                Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                dragOffset = transform.position - (Vector3)mouseWorldPos;
                m_rb.velocity = Vector2.zero;
                GameManager.Instance.isDragging = true;
                isDragged = true;
                currentState = GrayBallState.Drag;
            }
        }
    }

    private void ToggleCraftingSelection()
    {
        if (GameManager.Instance.selectedBalls.Contains(this))
        {
            if (GameManager.Instance.selectedBalls[0] == this)
            {
                Debug.Log("[RedBall] First ball clicked again, cancelling CraftMode: " + gameObject.name);
                GameManager.Instance.CancelCraftMode();
            }
            else
            {
                Debug.Log("[RedBall] Deselecting ball: " + gameObject.name);
                CancelCraftingVisual();
                GameManager.Instance.UnregisterSelectedBall(this);
            }
        }
        else
        {
            if (GameManager.Instance.selectedBalls.Count > 0 && GameManager.Instance.currentCraftModeCollider != null)
            {
                CraftModeCollider cmc = GameManager.Instance.currentCraftModeCollider.GetComponent<CraftModeCollider>();
                if (cmc != null)
                {
                    float craftRadius = cmc.radius;
                    float dist = Vector2.Distance(GameManager.Instance.currentCraftModeCollider.transform.position, transform.position);
                    if (dist > craftRadius)
                    {
                        Debug.Log("[RedBall] Ball " + gameObject.name + " is outside the craft radius (" + dist + " > " + craftRadius + "). Selection denied.");
                        return;
                    }
                }
            }
            Debug.Log("[RedBall] Selecting ball for crafting: " + gameObject.name);
            currentState = GrayBallState.Craft;
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(true);
            }
            if (GameManager.Instance.selectedBalls.Count == 0 && GameManager.Instance.craftModeColliderPrefab != null)
            {
                GameManager.Instance.currentCraftModeCollider = Instantiate(
                    GameManager.Instance.craftModeColliderPrefab,
                    transform.position,
                    Quaternion.identity,
                    transform);
                Debug.Log("[RedBall] Instantiated CraftModeCollider on first selected ball: " + gameObject.name);
            }
            GameManager.Instance.RegisterSelectedBall(this);
        }
    }

    public void CancelCraftingVisual()
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
        currentState = m_rb.isKinematic ? GrayBallState.Inhale : GrayBallState.Idle;
        Debug.Log("[GrayVioletBall] Crafting visual cancelled for ball: " + gameObject.name);
        foreach (Transform child in transform)
        {
            if (child.CompareTag("CraftCollider"))
            {
                Destroy(child.gameObject);
                Debug.Log("[GrayVioletBall] Destroyed CraftCollider child on ball: " + gameObject.name);
            }
        }
    }

    private bool HasCraftModeCollider()
    {
        return GetComponentInChildren<CraftModeCollider>() != null;
    }

    public void ApplyCraftForce(Vector2 direction)
    {
    }

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState != GrayBallState.Drag && currentState != GrayBallState.Inhale)
        {
            currentState = GrayBallState.Inhale;
            m_rb.isKinematic = true;
            Debug.Log("[GrayVioletBall] Transition to Inhale");
        }
    }

    public Transform Transform { get { return transform; } }
}
