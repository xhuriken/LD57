using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RedBall;

public class FirstBall : MonoBehaviour , ISaveData
{
    [Header("Properties")]
    [SerializeField] private int duplicateCount = 3;
    [SerializeField] private GameObject duplicateBall;
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

    private bool isDragged = false;
    [Header("Utils")]
    public bool isClickable = true;  

    [Header("Particules/SFX")]
    public AudioClip as_duplicate;
    public AudioClip as_click;
    public GameObject duplicateParticules;
    public GameObject clickParticules;
    private AudioSource m_audioSource;

    private enum FirstBallState { Idle, Click, Duplicate, Drag }
    private FirstBallState currentState = FirstBallState.Idle;

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
                        Instantiate(duplicateParticules, transform.position, Quaternion.identity);
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
                    isDragged = true;
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


    private IEnumerator SpawnProp()
    {
        GameObject newObject = Instantiate(duplicateBall, transform.position, Quaternion.identity);
        newObject.name = "Ball";

        SaveableObject so = newObject.GetComponent<SaveableObject>();
        if (so != null)
        {
            so.SetUniqueId(System.Guid.NewGuid().ToString());
        }

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Debug.Log("SpawnProp random direction: " + randomDir);
        newObject.GetComponent<Rigidbody2D>().AddForce(randomDir * force, ForceMode2D.Impulse);
        yield return null;
    }




    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bumper"))
        {
            isDragged = false;
            currentState = FirstBallState.Idle;
        }
    }
}
