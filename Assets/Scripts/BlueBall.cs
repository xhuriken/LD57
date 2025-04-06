using Shapes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class BlueBall : MonoBehaviour, ICraftableBall
{
    [Header("Properties")]
    [SerializeField] private int duplicateCount = 3;
    public float force = 2f;
    private int clickCount = 0;

    // Composants
    private Animator m_animator;
    private Rigidbody2D m_rb;
    private CircleCollider2D m_cc;
    private Vector3 dragOffset;
    private Data m_data;

    [Header("Utils")]
    public bool isDragged = false;
    public bool isClickable = true;

    [Header("Particles/SFX")]
    public AudioClip as_duplicate;
    public AudioClip as_click;
    public GameObject duplicateParticules;
    public GameObject clickParticules;
    private AudioSource m_audioSource;

    // États de BlueBall
    public enum BlueBallState { Spawn, Idle, Click, Duplicate, Drag, Friction, Inhale, Crafting, CraftingMoving }
    public BlueBallState currentState = BlueBallState.Spawn;

    [Header("Oscillation Settings")]
    public float amplitude = 5f;
    public float oscillationSpeed = 2f;

    [Header("Friction Settings")]
    public float frictionFactor = 0.99f;
    public float frictionThreshold = 0.01f;

    // Variables d'oscillation
    private float originY;
    private float topY;
    private float bottomY;
    private int direction = 1;

    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_cc = GetComponent<CircleCollider2D>();
        m_audioSource = GetComponent<AudioSource>();
        m_rb = GetComponent<Rigidbody2D>();
        m_data = GetComponent<Data>();

        currentState = BlueBallState.Spawn;
        StartCoroutine(TransitionFromSpawn());

        originY = transform.position.y;
        topY = originY + amplitude;
        bottomY = originY - amplitude;
        direction = 1;
    }

    IEnumerator TransitionFromSpawn()
    {
        yield return new WaitForSeconds(1f);
        currentState = BlueBallState.Idle;
        m_rb.velocity = new Vector2(0, oscillationSpeed * direction);
    }

    void Update()
    {
        switch (currentState)
        {
            case BlueBallState.Spawn:
                ClickEvent();
                break;
            case BlueBallState.Idle:
                if (m_data != null && m_data.isInhaled)
                {
                    currentState = BlueBallState.Inhale;
                    m_animator.SetTrigger("Inhale");
                    m_rb.velocity = Vector2.zero;
                    break;
                }
                if (!m_data.isFreeze)
                {
                    transform.position += new Vector3(0, oscillationSpeed * direction * Time.deltaTime, 0);
                    if (direction > 0 && transform.position.y >= topY)
                    {
                        direction = -1;
                        m_rb.velocity = new Vector2(0, oscillationSpeed * direction);
                    }
                    else if (direction < 0 && transform.position.y <= bottomY)
                    {
                        direction = 1;
                        m_rb.velocity = new Vector2(0, oscillationSpeed * direction);
                    }
                }
                if (isClickable)
                    ClickEvent();
                break;
            case BlueBallState.Click:
                currentState = BlueBallState.Idle;
                break;
            case BlueBallState.Duplicate:
                m_rb.velocity = Vector2.zero;
                if (!m_data.isFreeze)
                {
                    currentState = BlueBallState.Idle;
                }
                break;
            case BlueBallState.Drag:
                if (Input.GetMouseButton(1))
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    transform.position = mouseWorldPos + (Vector2)dragOffset;
                    m_rb.velocity = Vector2.zero;
                }
                if (Input.GetMouseButtonUp(1) || m_data.isInhaled)
                {
                    currentState = BlueBallState.Idle;
                    GameManager.Instance.isDragging = false;
                    isDragged = false;
                    originY = transform.position.y;
                    topY = originY + amplitude;
                    bottomY = originY - amplitude;
                }
                break;
            case BlueBallState.Friction:
                if (isClickable)
                    ClickEvent();
                m_rb.velocity *= frictionFactor;
                if (m_rb.velocity.magnitude < frictionThreshold)
                {
                    m_rb.velocity = Vector2.zero;
                    float distToTop = Mathf.Abs(topY - transform.position.y);
                    float distToBottom = Mathf.Abs(transform.position.y - bottomY);
                    direction = (distToTop < distToBottom) ? -1 : 1;
                    m_rb.velocity = new Vector2(0, oscillationSpeed * direction);
                    currentState = BlueBallState.Idle;
                }
                break;
            case BlueBallState.Inhale:
                m_rb.velocity = Vector2.zero;
                break;
            case BlueBallState.Crafting:
                ClickEvent();
                m_rb.velocity = Vector2.zero;
                break;
            case BlueBallState.CraftingMoving:
                // Mouvement géré par DOTween
                break;
        }
    }
    private bool HasCraftModeCollider()
    {
        return GetComponentInChildren<CraftModeCollider>() != null;
    }
    private void ClickEvent()
    {
        if (HasCraftModeCollider())
        {
            Debug.Log("[RedBall] Click disabled because CraftModeCollider is present on " + gameObject.name);
            return;
        }
        if (!isClickable)
            return;
        if (GameManager.Instance.menuShown)
            return;
        if (Input.GetMouseButtonDown(0) && IsMouseOver() && !GameManager.Instance.isDragging)
        {
            clickCount++;
            if (clickCount >= duplicateCount)
            {
                clickCount = 0;
                currentState = BlueBallState.Duplicate;
                m_audioSource.PlayOneShot(as_duplicate);
                m_animator.SetTrigger("Duplicate");
                Instantiate(duplicateParticules, transform.position, Quaternion.identity);
            }
            else
            {
                currentState = BlueBallState.Click;
                m_audioSource.PlayOneShot(as_click);
                m_animator.SetTrigger("Click");
                Instantiate(clickParticules, transform.position, Quaternion.identity);
            }
        }
        if (Input.GetMouseButtonDown(0) && IsMouseOver() && GameManager.Instance.CraftMode)
        {
            ToggleCraftingSelection();
        }
        if (Input.GetMouseButtonDown(1) && IsMouseOver())
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragOffset = transform.position - (Vector3)mouseWorldPos;
            m_rb.velocity = Vector2.zero;
            GameManager.Instance.isDragging = true;
            currentState = BlueBallState.Drag;
            isDragged = true;
        }
    }

    private void ToggleCraftingSelection()
    {
        if (GameManager.Instance.selectedBalls.Contains(this))
        {
            // Pour la première balle, recliquer annule tout le CraftMode
            if (GameManager.Instance.selectedBalls[0] == this)
            {
                Debug.Log("[BlueBall] First ball clicked again, cancelling CraftMode: " + gameObject.name);
                GameManager.Instance.CancelCraftMode();
            }
            else
            {
                Debug.Log("[BlueBall] Deselecting ball: " + gameObject.name);
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
                        Debug.Log("[BlueBall] Ball " + gameObject.name + " is outside the craft radius (" + dist + " > " + craftRadius + "). Selection denied.");
                        return;
                    }
                }
            }
            Debug.Log("[BlueBall] Selecting ball for crafting: " + gameObject.name);
            currentState = BlueBallState.Crafting;
            // Si BlueBall possède un visuel de sélection, activez-le ici
            GameManager.Instance.RegisterSelectedBall(this);
        }
    }

    public void CancelCraftingVisual()
    {
        // Implémentation similaire à RedBall (adapter si un visuel existe)
        currentState = BlueBallState.Idle;
        Debug.Log("[BlueBall] Crafting visual cancelled for ball: " + gameObject.name);
    }

    public void ApplyCraftForce(Vector2 direction)
    {
        // Ici, nous utilisons DOTween via le GameManager pour déplacer la balle,
        // donc cette méthode peut rester vide ou être utilisée pour d'autres effets.
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
    private IEnumerator SpawnProp()
    {
        GameObject newObject = Instantiate(gameObject, transform.position, Quaternion.identity);
        newObject.name = gameObject.name;
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Debug.Log("[RedBall] SpawnProp random direction: " + randomDir);
        newObject.GetComponent<Rigidbody2D>().AddForce(randomDir * force, ForceMode2D.Impulse);
        yield return null;
    }

    public string CraftBallType { get { return "BlueBall"; } }
    public Transform Transform { get { return transform; } }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bumper"))
        {
            isDragged = false;
            currentState = BlueBallState.Friction;
        }
    }
}