using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FirstBall : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private int duplicateCount = 3;
    [SerializeField] private GameObject duplicateBall;
    public float force = 2f;
    private int clickCount = 0;

    // Composants
    private Animator m_animator;
    private Rigidbody2D m_rb;
    private CircleCollider2D m_cc;
    private Vector3 dragOffset;

    // Booléens pour le suivi de l'état de la souris et cliquabilité
    private bool isDragged = false;
    private bool isMouseOver = false;
    [Header("Utils")]
    public bool isClickable = true;  // Flag similaire à RedBall

    [Header("Particules/SFX")]
    public AudioClip as_duplicate;
    public AudioClip as_click;
    public GameObject duplicateParticules;
    public GameObject clickParticules;
    private AudioSource m_audioSource;

    // Machine à états
    private enum PropState { Idle, Click, Duplicate, Drag }
    private PropState currentState = PropState.Idle;

    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_cc = GetComponent<CircleCollider2D>();
        m_audioSource = GetComponent<AudioSource>();
        m_rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        switch (currentState)
        {
            case PropState.Idle:
                // Vérification de cliquabilité et présence de la souris sur l'objet
                if (isClickable && Input.GetMouseButtonDown(0) && IsMouseOver() && !GameManager.Instance.isDragging)
                {
                    clickCount++;
                    if (clickCount >= duplicateCount)
                    {
                        clickCount = 0;
                        currentState = PropState.Duplicate;
                        m_audioSource.PlayOneShot(as_duplicate);
                        m_animator.SetTrigger("Duplicate");
                        Instantiate(duplicateParticules, transform.position, Quaternion.identity);
                    }
                    else
                    {
                        currentState = PropState.Click;
                        m_audioSource.PlayOneShot(as_click);
                        m_animator.SetTrigger("Click");
                        Instantiate(clickParticules, transform.position, Quaternion.identity);
                    }
                }
                // Clic droit pour drag si la souris est sur l'objet
                if (Input.GetMouseButtonDown(1) && IsMouseOver())
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    dragOffset = transform.position - (Vector3)mouseWorldPos;
                    m_rb.velocity = Vector2.zero;
                    GameManager.Instance.isDragging = true;
                    currentState = PropState.Drag;
                    isDragged = true;
                }
                break;

            case PropState.Click:
                currentState = PropState.Idle;
                break;

            case PropState.Duplicate:
                currentState = PropState.Idle;
                break;

            case PropState.Drag:
                if (Input.GetMouseButton(1))
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    transform.position = mouseWorldPos + (Vector2)dragOffset;
                    m_rb.velocity = Vector2.zero;
                }
                if (Input.GetMouseButtonUp(1))
                {
                    currentState = PropState.Idle;
                    GameManager.Instance.isDragging = false;
                    isDragged = false;
                }
                break;
        }
    }

    /// <summary>
    /// Méthode custom pour détecter si la souris est sur l'objet.
    /// </summary>
    /// <returns>true si la souris est sur l'objet, false sinon.</returns>
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

    private void OnMouseEnter()
    {
        isMouseOver = true;
        // Possibilité d'afficher un indicateur visuel ici
    }

    private void OnMouseExit()
    {
        if (!isDragged)
        {
            isMouseOver = false;
        }
    }

    private IEnumerator SpawnProp()
    {
        GameObject newObject = Instantiate(duplicateBall, transform.position, Quaternion.identity);
        newObject.name = "Ball";
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Debug.Log("SpawnProp random direction: " + randomDir);
        newObject.GetComponent<Rigidbody2D>().AddForce(randomDir * force, ForceMode2D.Impulse);
        yield return null;
    }
}
