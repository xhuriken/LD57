using Shapes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static RedBall; 

public class BlueBall : MonoBehaviour, ICraftableBall, INumber, ISaveData, IClickMachine
{
    [Header("Properties")]
    [SerializeField] private int duplicateCount = 3;
    public float force = 2f;
    private int clickCount = 0; 

    public int ClickCount
    {
        get { return clickCount; }
        set { clickCount = value; }
    }

    private Animator m_animator;
    private Rigidbody2D m_rb;
    private CircleCollider2D m_cc;
    private Vector3 dragOffset;
    private Data m_data;

    [Header("Utils")]
    public bool isClickable = true;

    [Header("Particles/SFX")]
    public AudioClip as_duplicate;
    public AudioClip as_click;
    public GameObject duplicateParticules;
    public GameObject clickParticules;
    private AudioSource m_audioSource;

    public bool isInMachine { get; set; } = false;
    public enum BlueBallState { Spawn, Idle, Click, Duplicate, Drag, Friction, Inhale, Crafting, CraftingMoving }
    public BlueBallState currentState = BlueBallState.Spawn;

    [Header("Oscillation Settings")]
    public float amplitude = 5f;
    public float oscillationSpeed = 2f;

    [Header("Friction Settings")]
    public float frictionFactor = 0.99f;
    public float frictionThreshold = 0.01f;

    private float originY;
    private float topY;
    private float bottomY;
    private int direction = 1;

    [Header("Crafting & Selection")]
    public GameObject selectionIndicator;

    private void Start()
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
        if (currentState == BlueBallState.Spawn)
        {
            yield return new WaitForSeconds(1f);
            currentState = BlueBallState.Idle;
            m_rb.velocity = new Vector2(0, oscillationSpeed * direction);
        }
    }

    void Update()
    {
        if (HasCraftModeCollider() && !m_rb.isKinematic)
        {
            m_rb.isKinematic = true;
        }
        else if (!HasCraftModeCollider() && m_rb.isKinematic)
        {
            m_rb.isKinematic = false;
        }
        if (isInMachine && m_cc.enabled)
        {
            m_cc.enabled = false;
            currentState = BlueBallState.Friction;
        }
        else if (!isInMachine && !m_cc.enabled)
        {
            m_cc.enabled = true;
        }
        if (isInMachine)
        {
            currentState = BlueBallState.Friction;
            m_rb.isKinematic = true;
        }
        else
        {
            if(m_rb.isKinematic)
                m_rb.isKinematic = false;
        }


        if (Input.GetMouseButtonDown(1) && IsMouseOver() && currentState == BlueBallState.Friction)
        {
            dragOffset = transform.position - (Vector3)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            m_rb.velocity = Vector2.zero;
            GameManager.Instance.isDragging = true;
            m_data.isDragged = true;
            currentState = BlueBallState.Drag;
            Debug.Log("[BlueBall] Forced Drag from Friction state");
        }

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
                    currentState = BlueBallState.Idle;
                break;

            case BlueBallState.Drag:
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
                            Debug.Log("[BlueBall] Dragged out of craft area, deselected.");
                            if (selectionIndicator != null)
                                selectionIndicator.SetActive(false);
                            GameManager.Instance.UnregisterSelectedBall(this);
                        }
                    }
                }
                if (Input.GetMouseButtonUp(1) || (m_data != null && m_data.isInhaled))
                {
                    currentState = BlueBallState.Idle;
                    GameManager.Instance.isDragging = false;
                    m_data.isDragged = false;
                    originY = transform.position.y;
                    topY = originY + amplitude;
                    bottomY = originY - amplitude;
                }
                break;

            case BlueBallState.Friction:
                if (m_data.isDragged)
                {
                    currentState = BlueBallState.Drag;
                    break;
                }
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
                break;
        }
    }

    private bool HasCraftModeCollider()
    {
        return GetComponentInChildren<CraftModeCollider>() != null;
    }

    private void ClickEvent()
    {
        if (HasCraftModeCollider() && !(GameManager.Instance.selectedBalls.Count > 0 && GameManager.Instance.selectedBalls[0] == this))
            return;
        if (isInMachine)
        {
            return;
        }
        if (!isClickable) return;
        if (GameManager.Instance.menuShown) return;

        if (GameManager.Instance.CraftMode)
        {
            if (IsMouseOver() && Input.GetMouseButtonDown(0))
                ToggleCraftingSelection();

            if (IsMouseOver() && Input.GetMouseButtonDown(1) && !HasCraftModeCollider())
            {
                Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                dragOffset = transform.position - (Vector3)mouseWorldPos;
                m_rb.velocity = Vector2.zero;
                GameManager.Instance.isDragging = true;
                m_data.isDragged = true;
                currentState = BlueBallState.Drag;
            }
            return;
        }

        if (Input.GetMouseButtonDown(0) && IsMouseOver() && !GameManager.Instance.isDragging)
            Click();

        if (Input.GetMouseButtonDown(1) && IsMouseOver())
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragOffset = transform.position - (Vector3)mouseWorldPos;
            m_rb.velocity = Vector2.zero;
            GameManager.Instance.isDragging = true;
            m_data.isDragged = true;
            currentState = BlueBallState.Drag;

            originY = transform.position.y;
            topY = originY + amplitude;
            bottomY = originY - amplitude;
        }
    }

    private void ToggleCraftingSelection()
    {
        if (GameManager.Instance.selectedBalls.Contains(this))
        {
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
            if (selectionIndicator != null)
                selectionIndicator.SetActive(true);
            if (GameManager.Instance.selectedBalls.Count == 0 && GameManager.Instance.craftModeColliderPrefab != null)
            {
                GameManager.Instance.currentCraftModeCollider = Instantiate(
                    GameManager.Instance.craftModeColliderPrefab,
                    transform.position,
                    Quaternion.identity,
                    transform);
                Debug.Log("[BlueBall] Instantiated CraftModeCollider on first selected ball: " + gameObject.name);
            }
            GameManager.Instance.RegisterSelectedBall(this);
        }
    }

    public void CancelCraftingVisual()
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
        currentState = BlueBallState.Idle;
        Debug.Log("[BlueBall] Crafting visual cancelled for ball: " + gameObject.name);
        foreach (Transform child in transform)
        {
            if (child.CompareTag("CraftCollider"))
            {
                Destroy(child.gameObject);
                Debug.Log("[BlueBall] Destroyed CraftCollider child on ball: " + gameObject.name);
            }
        }
    }

    public void ApplyCraftForce(Vector2 direction)
    {

    }

    public void Click()
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

        SaveableObject so = newObject.GetComponent<SaveableObject>();
        if (so != null)
        {
            so.SetUniqueId(System.Guid.NewGuid().ToString());
        }

        Vector2 dir;
        if (isInMachine)
        {
            dir = Vector2.down;
            Debug.Log("[BlueBall] SpawnProp direction: " + dir);
        }
        else
        {
            dir = Random.insideUnitCircle.normalized;
            Debug.Log("[BlueBall] SpawnProp random direction: " + dir);
        }
        newObject.GetComponent<Rigidbody2D>().AddForce(dir * force, ForceMode2D.Impulse);
        yield return null;
    }

    public string CraftBallType { get { return "BlueBall"; } }
    public Transform Transform { get { return transform; } }
    public int Number { get; private set; } = 2;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bumper"))
        {
            currentState = BlueBallState.Friction;
            if (m_data.isDragged)
            {
                m_data.isDragged = false;
                GameManager.Instance.isDragging = false;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState != BlueBallState.Drag)
        {
            currentState = BlueBallState.Friction;
        }
    }
}
