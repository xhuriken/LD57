using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FirstProp : MonoBehaviour
{
    //Ca c'est un Prop, object commun entre touts les forme
    //Ce script sert a donnée les characteristique de la forme


    [Header("Properties")]
    [SerializeField]
    private int duplicateCount = 3;
    [SerializeField]
    private GameObject duplicateBall;
    public float force = 2f;
    private int clickCount = 0;
    //Component
    private Animator m_animator;
    private Rigidbody2D m_rb;
    private CircleCollider2D m_cc;
    private Vector3 dragOffset;
    //bools
    private bool isDragged = false;
    private bool isMouseOver = false;

    [Header("Particules/SFX")]
    //Clips
    public AudioClip as_duplicate;
    public AudioClip as_click;
    //Particules
    public GameObject duplicateParticules;
    public GameObject clickParticules;
    private AudioSource m_audioSource;

    //State machine
    private enum PropState { Idle, Click, Duplicate, Drag, }
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
                //Left click = Click
                if (Input.GetMouseButtonDown(0) && isMouseOver && !GameManager.Instance.isDragging)
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
                //Right Click = Drag
                if (Input.GetMouseButtonDown(1) && isMouseOver)
                {
                    //Relative offset
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

    private void OnMouseEnter()
    {
        isMouseOver = true;
        //Debug.Log("Mouse is over the object");
    }

    private void OnMouseExit()
    {

        if (!isDragged)
        {
            isMouseOver = false;
            //Debug.Log("Mouse is not over the object anymore");
        }
    }

    private IEnumerator SpawnProp()
    {
        GameObject newObject = Instantiate(duplicateBall, transform.position, Quaternion.identity);
        newObject.name = "Ball";
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        newObject.GetComponent<Rigidbody2D>().AddForce(randomDir * force, ForceMode2D.Impulse);
        yield return null;
    }
}
