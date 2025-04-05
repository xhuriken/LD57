using System.Collections;
using UnityEngine;

public class BallBase : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] protected int duplicateCount = 3;
    public float force = 2f;
    protected int clickCount = 0;

    // Composants
    protected Animator m_animator;
    protected Rigidbody2D m_rb;
    protected CircleCollider2D m_cc;
    protected Vector3 dragOffset;

    // Gestion du drag
    public bool isDragged = false;

    [Header("Particules/SFX")]
    public AudioClip as_duplicate;
    public AudioClip as_click;
    public GameObject duplicateParticules;
    public GameObject clickParticules;
    protected AudioSource m_audioSource;

    [Header("Utils")]
    public bool isInhaled = false; // mis à jour par StockMachine
    public enum BallState { Idle, Click, Duplicate, Drag, Inhale }
    public BallState currentState = BallState.Idle;
    public bool isClickable = true;

    protected virtual void Start()
    {
        m_animator = GetComponent<Animator>();
        m_cc = GetComponent<CircleCollider2D>();
        m_audioSource = GetComponent<AudioSource>();
        m_rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Update()
    {
        switch (currentState)
        {
            case BallState.Idle:
                if (isInhaled)
                {
                    currentState = BallState.Inhale;
                    m_animator.SetTrigger("Inhale");
                    break;
                }
                if (Input.GetMouseButtonDown(0) && IsMouseOver() && !GameManager.Instance.isDragging)
                {
                    clickCount++;
                    if (clickCount >= duplicateCount)
                    {
                        clickCount = 0;
                        currentState = BallState.Duplicate;
                        m_audioSource.PlayOneShot(as_duplicate);
                        m_animator.SetTrigger("Duplicate");
                        Instantiate(duplicateParticules, transform.position, Quaternion.identity);
                    }
                    else
                    {
                        currentState = BallState.Click;
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
                    currentState = BallState.Drag;
                    isDragged = true;
                }
                break;

            case BallState.Click:
                currentState = BallState.Idle;
                break;

            case BallState.Duplicate:
                currentState = BallState.Idle;
                break;

            case BallState.Drag:
                if (Input.GetMouseButton(1))
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    transform.position = mouseWorldPos + (Vector2)dragOffset;
                    m_rb.velocity = Vector2.zero;
                }
                if (Input.GetMouseButtonUp(1) || isInhaled)
                {
                    currentState = BallState.Idle;
                    GameManager.Instance.isDragging = false;
                    isDragged = false;
                }
                break;

            case BallState.Inhale:
                // État géré par l'animation d'inhalation
                break;
        }
    }

    // Vérifie si la souris se trouve sur l'objet en fonction du type de collider
    protected bool IsMouseOver()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            return false;

        if (col is CircleCollider2D circle)
        {
            float radius = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            return Vector2.Distance(transform.position, mousePos) <= radius;
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
        Debug.Log(randomDir);
        newObject.GetComponent<Rigidbody2D>().AddForce(randomDir * force, ForceMode2D.Impulse);
        yield return null;
    }
}
