using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Prop : MonoBehaviour
{
    //Ca c'est un Prop, object commun entre toutes les formes
    //Ce script sert à donner les caractéristiques de la forme
    [Header("Properties")]
    [SerializeField] private int duplicateCount = 3;
    public float force = 2f;
    private int clickCount = 0;

    // Composants
    private Animator m_animator;
    private Rigidbody2D m_rb;
    private CircleCollider2D m_cc;
    private Vector3 dragOffset;

    // Booléens de gestion
    public bool isDragged = false;
    // On se passe de la variable isMouseOver et on utilise IsMouseOver() pour vérifier en temps réel.

    [Header("Particules/SFX")]
    public AudioClip as_duplicate;
    public AudioClip as_click;
    public GameObject duplicateParticules;
    public GameObject clickParticules;
    private AudioSource m_audioSource;

    [Header("Utils")]
    public bool isInhaled = false; // modifié par StockMachine.cs
    // State machine
    public enum PropState { Idle, Click, Duplicate, Drag, Inhale }
    public PropState currentState = PropState.Idle;
    // L'état Inhale correspond à l'animation (Trigger "Inhale")

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
                // Si l'objet est inhalé, passer en état Inhale
                if (isInhaled)
                {
                    currentState = PropState.Inhale;
                    m_animator.SetTrigger("Inhale");
                    break;
                }

                // Clic gauche : déclenche Click ou Duplicate
                if (Input.GetMouseButtonDown(0) && IsMouseOver() && !GameManager.Instance.isDragging)
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
                // Clic droit : lance le drag
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
                if (Input.GetMouseButtonUp(1) || isInhaled)
                {
                    currentState = PropState.Idle;
                    GameManager.Instance.isDragging = false;
                    isDragged = false;
                }
                break;

            case PropState.Inhale:
                // État géré par l'animation d'inhalation
                break;
        }
    }

    // Méthode de vérification personnalisée pour déterminer si la souris est sur l'objet
    private bool IsMouseOver()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            return false;

        // Pour un CircleCollider2D, vérifie la distance par rapport au centre
        if (col is CircleCollider2D circle)
        {
            float radius = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            return Vector2.Distance(transform.position, mousePos) <= radius;
        }
        // Pour un PolygonCollider2D ou d'autres types, on utilise OverlapPoint
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
