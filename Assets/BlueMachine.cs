using System.Collections;
using UnityEngine;

public class BlueMachine : MonoBehaviour
{
    private enum MachineState { WaitBall, Click, Exit, Drag }
    private MachineState currentState = MachineState.WaitBall;

    [Header("Ball Placement Settings")]
    public Transform placementPoint;
    public float placementYOffset = 0.5f;
    public float slideDuration = 0.2f;
    public float clickDelay = 0.5f;

    [Header("Detection Settings")]
    public float detectionRadius = 1f;
    public float detectionRectWidth = 0.1f;
    public float detectionRectHeight = 3f;

    [Header("Animation Settings")]
    public Animator animator;

    [Header("Exit Settings")]
    public float bumpForce = 5f;
    public float exitDelay = 1f;

    [Header("Drag Settings")]
    public AudioClip as_cantplace;
    private bool isDragged = false;
    private Vector3 dragOffset;
    private Rigidbody2D m_rb;
    private AudioSource m_audioSource;
    private float lastSoundTime = -Mathf.Infinity;

    [Header("Prefab Settings")]
    public GameObject BluePrefab;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        m_rb = GetComponent<Rigidbody2D>();
        m_audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (HandleMachineDrag())
            return;

        if (currentState != MachineState.WaitBall)
            return;

        HandleBallDetection();
    }

    private bool HandleMachineDrag()
    {
        if (currentState == MachineState.Drag)
        {
            if (Input.GetMouseButton(1))
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                transform.position = mouseWorldPos + dragOffset;
                m_rb.velocity = Vector2.zero;
            }
            if (Input.GetMouseButtonUp(1))
            {
                isDragged = false;
                currentState = MachineState.WaitBall;
                CheckLayerCollisionAndRepulse();
            }
            return true;
        }

        if (Input.GetMouseButtonDown(1) && IsMouseOverMachine())
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragOffset = transform.position - mouseWorldPos;
            isDragged = true;
            currentState = MachineState.Drag;
            return true;
        }
        return false;
    }

    private bool IsMouseOverMachine()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D col = GetComponent<Collider2D>();
        return (col != null && col.OverlapPoint(mousePos));
    }

    private void HandleBallDetection()
    {
        Vector2 center = transform.position;
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject ball in balls)
        {
            IClickMachine clickMachine = ball.GetComponent<IClickMachine>();
            if (clickMachine != null && clickMachine.isInMachine)
                continue;

            float distance = Vector2.Distance(center, ball.transform.position);
            if (distance <= detectionRadius)
            {
                StartCoroutine(SlideAndClick(ball));
                break;
            }
        }
    }

    private IEnumerator SlideAndClick(GameObject ball)
    {
        IClickMachine clickMachine = ball.GetComponent<IClickMachine>();
        if (clickMachine != null)
            clickMachine.isInMachine = true;

        currentState = MachineState.Click;

        Vector3 targetPosition = placementPoint != null ? placementPoint.position : transform.position;

        Vector3 ballStartPosition = ball.transform.position;

        float elapsed = 0f;
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        while (elapsed < slideDuration)
        {
            ball.transform.position = Vector3.Lerp(ballStartPosition, targetPosition, elapsed / slideDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        ball.transform.position = targetPosition;
        ball.transform.SetParent(transform);

        if (animator != null)
        {
            animator.SetTrigger("Turn");
        }

        GameObject blueBall = Instantiate(BluePrefab, transform.position, transform.rotation);
        Destroy(ball);

        yield return new WaitForSeconds(clickDelay);

        currentState = MachineState.Exit;
        StartCoroutine(ExitBall(blueBall, ballStartPosition, targetPosition));
    }

    private IEnumerator ExitBall(GameObject ball, Vector3 ballStartPosition, Vector3 targetPosition)
    {
        yield return new WaitForSeconds(1f);
        BlueBall blueBall = ball.GetComponent<BlueBall>();
        if (blueBall != null && blueBall.currentState != BlueBall.BlueBallState.Friction)
        {
            blueBall.currentState = BlueBall.BlueBallState.Friction;
        }
        ball.transform.SetParent(null);
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.isKinematic = false;

        Vector2 exitDirection = (targetPosition - ballStartPosition).normalized;
        if (rb != null)
        {
            rb.AddForce(exitDirection * bumpForce, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(1f);

        currentState = MachineState.WaitBall;
        if (ball.GetComponent<IClickMachine>() != null)
            ball.GetComponent<IClickMachine>().isInMachine = false;
    }

    private void CheckLayerCollisionAndRepulse()
    {
        Vector2 center = transform.position;
        Vector2 size = new Vector2(2f, 3f);
        float angle = 0f;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;

            if (hit.transform.IsChildOf(this.transform))
                continue;


            if (hit.gameObject.layer == LayerMask.NameToLayer("Objects"))
            {
                Debug.Log("[BlueMachine] Repulse with: " + hit.gameObject.name);
                RepulseDraggedWith(hit.gameObject);
                break;
            }
        }
    }

    private void RepulseDraggedWith(GameObject other)
    {
        bool thisWasKinematic = m_rb.isKinematic;
        if (m_rb.isKinematic)
        {
            m_rb.isKinematic = false;
        }

        Vector2 repulseDirection = (transform.position - other.transform.position).normalized;
        m_rb.AddForce(repulseDirection * bumpForce, ForceMode2D.Impulse);

        if (Time.time - lastSoundTime >= 2f)
        {
            if (as_cantplace != null)
            {
                m_audioSource.PlayOneShot(as_cantplace, 0.7f);
            }
            lastSoundTime = Time.time;
        }

        StartCoroutine(ResetKinematicState(m_rb, thisWasKinematic));
    }

    private IEnumerator ResetKinematicState(Rigidbody2D rb, bool originalState)
    {
        yield return new WaitForSeconds(0.15f);
        rb.isKinematic = originalState;
        rb.velocity = Vector2.zero;

        CheckLayerCollisionAndRepulse();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
