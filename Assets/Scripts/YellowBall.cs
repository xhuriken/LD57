using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static BlueBall; // si nécessaire

public class YellowBall : MonoBehaviour, ICraftableBall, INumber, IClickMachine
{
    [Header("Properties")]
    [SerializeField] private int duplicateCount = 3;
    public float force = 2f;
    private int clickCount = 0;

    // Components
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

    public enum RedBallState { Spawn, Idle, Click, Duplicate, Drag, Inhale, Crafting, CraftingMoving }
    public RedBallState currentState = RedBallState.Spawn;

    public GameObject selectionIndicator;

    // Paramètres pour l'animation du cercle via Shapes
    public float targetRadius = 1f;
    public float duration = 1f;

    public bool isInMachine { get; set; } = false;

    private void Start()
    {
        m_animator = GetComponent<Animator>();
        m_cc = GetComponent<CircleCollider2D>();
        m_audioSource = GetComponent<AudioSource>();
        m_rb = GetComponent<Rigidbody2D>();
        m_data = GetComponent<Data>();

        currentState = RedBallState.Spawn;
        StartCoroutine(TransitionFromSpawn());
    }

    private IEnumerator TransitionFromSpawn()
    {
        yield return new WaitForSeconds(1f);
        if (currentState == RedBallState.Spawn)
        {
            currentState = RedBallState.Idle;
        }
    }

    private void Update()
    {
        if (HasCraftModeCollider() && !m_rb.isKinematic)
        {
            m_rb.isKinematic = true;
        }
        else if (!HasCraftModeCollider() && m_rb.isKinematic)
        {
            m_rb.isKinematic = false;
        }

        // Pour la GrayViolet
        m_animator.SetBool("isInhaled", m_data.isInhaledGrayViolet);

        switch (currentState)
        {
            case RedBallState.Spawn:
                ClickEvent();
                m_rb.velocity *= 0.99f;
                if (m_rb.velocity.magnitude < 0.01f)
                    m_rb.velocity = Vector2.zero;
                break;
            case RedBallState.Idle:
                if (m_data != null && m_data.isInhaled)
                {
                    currentState = RedBallState.Inhale;
                    m_animator.SetTrigger("Inhale");
                    m_rb.velocity = Vector2.zero;
                    break;
                }
                m_rb.velocity *= 0.99f;
                if (m_rb.velocity.magnitude < 0.01f)
                    m_rb.velocity = Vector2.zero;
                ClickEvent();
                break;
            case RedBallState.Click:
                // Une fois dans l'état Click, la machine à click pourra appeler ActivateClickEvent()
                currentState = RedBallState.Idle;
                break;
            case RedBallState.Duplicate:
                currentState = RedBallState.Idle;
                break;
            case RedBallState.Drag:
                if (Input.GetMouseButton(1))
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    transform.position = mouseWorldPos + (Vector2)dragOffset;
                    m_rb.velocity = Vector2.zero;

                    // Vérifier si la balle est en dehors de la zone de Craft
                    if (GameManager.Instance.CraftMode && GameManager.Instance.currentCraftModeCollider != null)
                    {
                        float dist = Vector2.Distance(GameManager.Instance.currentCraftModeCollider.transform.position, transform.position);
                        float radius = GameManager.Instance.currentCraftModeCollider.GetComponent<CraftModeCollider>().radius;
                        if (dist > radius)
                        {
                            Debug.Log("[RedBall] Dragged out of craft area, deselected.");
                            if (selectionIndicator != null)
                                selectionIndicator.SetActive(false);
                            GameManager.Instance.UnregisterSelectedBall(this);
                        }
                    }
                }
                if (Input.GetMouseButtonUp(1) || (m_data != null && m_data.isInhaled) || isInMachine)
                {
                    currentState = RedBallState.Idle;
                    GameManager.Instance.isDragging = false;
                    m_data.isDragged = false;
                }
                break;
            case RedBallState.Inhale:
                m_rb.velocity = Vector2.zero;
                break;
            case RedBallState.Crafting:
                ClickEvent();
                m_rb.velocity = Vector2.zero;
                break;
            case RedBallState.CraftingMoving:
                break;
        }
    }

    private void ClickEvent()
    {
        if (HasCraftModeCollider() && !(GameManager.Instance.selectedBalls.Count > 0 && GameManager.Instance.selectedBalls[0] == this))
        {
            return;
        }
        if (!isClickable)
            return;
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
                m_data.isDragged = true;
                currentState = RedBallState.Drag;
            }
            return;
        }
        if (Input.GetMouseButtonDown(0) && IsMouseOver() && !GameManager.Instance.isDragging)
        {
            Click();
        }
        if (Input.GetMouseButtonDown(1) && IsMouseOver())
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragOffset = transform.position - (Vector3)mouseWorldPos;
            m_rb.velocity = Vector2.zero;
            GameManager.Instance.isDragging = true;
            currentState = RedBallState.Drag;
            m_data.isDragged = true;
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
            currentState = RedBallState.Crafting;
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
        currentState = RedBallState.Idle;
        Debug.Log("[RedBall] Crafting visual cancelled for ball: " + gameObject.name);
        foreach (Transform child in transform)
        {
            if (child.CompareTag("CraftCollider"))
            {
                Destroy(child.gameObject);
                Debug.Log("[RedBall] Destroyed CraftCollider child on ball: " + gameObject.name);
            }
        }
    }

    private bool HasCraftModeCollider()
    {
        return GetComponentInChildren<CraftModeCollider>() != null;
    }

    public void ApplyCraftForce(Vector2 direction)
    {
        // inused but keep here
    }

    // Méthode Click utilisant Shapes pour animer la création d'un cercle jaune transparent
    public void Click()
    {
        // Création d'un nouvel objet pour le cercle
        GameObject circleObj = new GameObject("AnimatedCircle");
        circleObj.transform.position = transform.position;

        // Ajout du composant Circle du package Shapes
        Circle circle = circleObj.AddComponent<Circle>();
        // Initialiser le rayon à 0
        circle.Radius = 0;
        // Définir la couleur jaune avec transparence
        circle.Color = new Color(1f, 1f, 0f, 0.5f);

        // Lancer la coroutine pour animer l'agrandissement du rayon
        StartCoroutine(AnimateCircle(circle, targetRadius, duration));
    }

    private IEnumerator AnimateCircle(Circle circle, float targetRadius, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            circle.Radius = Mathf.Lerp(0, targetRadius, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        circle.Radius = targetRadius;
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
        Vector2 dir;
        if (isInMachine)
        {
            dir = Vector2.down;
            Debug.Log("[RedBall] SpawnProp direction: " + dir);
        }
        else
        {
            dir = Random.insideUnitCircle.normalized;
            Debug.Log("[RedBall] SpawnProp random direction: " + dir);
        }
        newObject.GetComponent<Rigidbody2D>().AddForce(dir * force, ForceMode2D.Impulse);
        yield return null;
    }

    public string CraftBallType { get { return "RedBall"; } }
    public Transform Transform { get { return transform; } }
    public int Number { get; private set; } = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bumper"))
        {
            m_data.isDragged = false;
            currentState = RedBallState.Idle;
        }
        if (collision.CompareTag("GrayViolet"))
        {
            if (m_data != null && !m_data.isInhaledGrayViolet)
            {
                m_data.isInhaledGrayViolet = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("GrayViolet"))
        {
            if (m_data != null && m_data.isInhaledGrayViolet)
            {
                m_data.isInhaledGrayViolet = false;
            }
        }
    }
}
