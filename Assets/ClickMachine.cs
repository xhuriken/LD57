using System.Collections;
using UnityEngine;

public class ClickerMachine : MonoBehaviour
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

    [Header("Drag & Repulse Settings")]
    public AudioClip as_cantplace;
    private Rigidbody2D m_rb;
    private AudioSource m_audioSource;
    private float lastSoundTime = -Mathf.Infinity;

    private bool isDragged = false;
    private Vector3 dragOffset;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        m_rb = GetComponent<Rigidbody2D>();
        m_audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        switch (currentState)
        {
            case MachineState.Drag:
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
                return;

            case MachineState.WaitBall:
                if (Input.GetMouseButtonDown(1) && IsMouseOverMachine())
                {
                    dragOffset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    isDragged = true;
                    currentState = MachineState.Drag;
                    return;
                }

                Vector2 center = transform.position;
                Vector2 leftPoint = center + Vector2.left * detectionRadius;
                Vector2 rightPoint = center + Vector2.right * detectionRadius;

                Rect leftRect = new Rect(leftPoint.x - detectionRectWidth / 2, leftPoint.y - detectionRectHeight / 2, detectionRectWidth, detectionRectHeight);
                Rect rightRect = new Rect(rightPoint.x - detectionRectWidth / 2, rightPoint.y - detectionRectHeight / 2, detectionRectWidth, detectionRectHeight);

                GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
                foreach (GameObject ball in balls)
                {
                    IClickMachine clickMachine = ball.GetComponent<IClickMachine>();
                    if (clickMachine != null && clickMachine.isInMachine)
                        continue;

                    Vector2 ballPos = ball.transform.position;
                    if (leftRect.Contains(ballPos) || rightRect.Contains(ballPos))
                    {
                        StartCoroutine(SlideAndClick(ball));
                        break;
                    }
                }
                CheckBallIntruders();

                break;

            case MachineState.Click:
            case MachineState.Exit:
                CheckBallIntruders();

                break;
        }
    }

    private void CheckBallIntruders()
    {
        Vector2 center = transform.position;
        Vector2 leftPoint = center + Vector2.left * detectionRadius;
        Vector2 rightPoint = center + Vector2.right * detectionRadius;

        Rect leftRect = new Rect(leftPoint.x - detectionRectWidth / 2, leftPoint.y - detectionRectHeight / 2, detectionRectWidth, detectionRectHeight);
        Rect rightRect = new Rect(rightPoint.x - detectionRectWidth / 2, rightPoint.y - detectionRectHeight / 2, detectionRectWidth, detectionRectHeight);

        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");

        foreach (GameObject ball in balls)
        {
            IClickMachine clickMachine = ball.GetComponent<IClickMachine>();
            if (clickMachine != null && clickMachine.isInMachine)
                continue; 

            Vector2 ballPos = ball.transform.position;
            if (leftRect.Contains(ballPos) || rightRect.Contains(ballPos))
            {
                Debug.Log("[ClickerMachine] Balle intruse détectée: " + ball.name);

                Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 sideDirection = (ball.transform.position.x < transform.position.x) ? Vector2.left : Vector2.right;
                    rb.AddForce(sideDirection * bumpForce, ForceMode2D.Impulse);
                }
            }
        }
    }


    private bool IsMouseOverMachine()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 center = transform.position;

        float height = 1.5f * 2f; 
        float width = height * (2f / 3f); 

        Rect rect = new Rect(
            center.x - width / 2f,
            center.y - height / 2f,
            width,
            height
        );

        return rect.Contains(mousePos);
    }

    private IEnumerator SlideAndClick(GameObject ball)
    {
        if (ball.GetComponent<Data>().isDragged)
        {
            ball.GetComponent<Data>().isDragged = false;
            GameManager.Instance.isDragging = false;
        }
        IClickMachine clickMachine = ball.GetComponent<IClickMachine>();
        if (clickMachine != null)
            clickMachine.isInMachine = true;

        currentState = MachineState.Click;

        Vector3 targetPosition = placementPoint != null ? placementPoint.position : transform.position;
        targetPosition.y -= placementYOffset;

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
            animator.SetTrigger("Click");

            INumber number = ball.GetComponent<INumber>();
            if (number != null)
                number.Click();
        }

        yield return new WaitForSeconds(clickDelay);

        currentState = MachineState.Exit;
        StartCoroutine(ExitBall(ball, ballStartPosition, targetPosition));
    }

    private IEnumerator ExitBall(GameObject ball, Vector3 ballStartPosition, Vector3 targetPosition)
    {
        yield return new WaitForSeconds(1f);

        ball.transform.SetParent(null);
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.isKinematic = false;

        Vector2 horizontalDirection = new Vector2(targetPosition.x - ballStartPosition.x, 0f);
        if (horizontalDirection == Vector2.zero)
            horizontalDirection = Vector2.right;
        else
            horizontalDirection.Normalize();

        if (rb != null)
        {
            rb.AddForce(horizontalDirection * bumpForce, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(exitDelay);

        currentState = MachineState.WaitBall;
        IClickMachine clickMachine = ball.GetComponent<IClickMachine>();
        if (clickMachine != null)
            clickMachine.isInMachine = false;
        //StartCoroutine(resetIsInMachine(ball));
    }

    //IEnumerator resetIsInMachine(GameObject ball)
    //{
    //    yield return new WaitForSeconds(0.5f);
    //    IClickMachine clickMachine = ball.GetComponent<IClickMachine>();
    //    if (clickMachine != null)
    //        clickMachine.isInMachine = false;
    //}
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
                Debug.Log("[ClickerMachine] Repulse with: " + hit.gameObject.name);
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
            Debug.Log("[ClickerMachine] Rigidbody set to dynamic for repulsion");
        }

        Vector2 repulseDirection = (transform.position - other.transform.position).normalized;
        m_rb.AddForce(repulseDirection * bumpForce, ForceMode2D.Impulse);

        if (Time.time - lastSoundTime >= 2f)
        {
            if (as_cantplace != null)
            {
                m_audioSource.PlayOneShot(as_cantplace, 0.7f);
                Debug.Log("[ClickerMachine] Sound cantplace joué");
            }
            lastSoundTime = Time.time;
        }

        StartCoroutine(ResetKinematicState(m_rb, thisWasKinematic));
    }

    private IEnumerator ResetKinematicState(Rigidbody2D rb, bool originalState)
    {
        yield return new WaitForSeconds(0.15f);
        rb.velocity = Vector2.zero;
        rb.isKinematic = originalState;

        Debug.Log("[ClickerMachine] Reset Rigidbody to Kinematic: " + originalState);

        CheckLayerCollisionAndRepulse();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector2 center = transform.position;
        Vector2 leftPoint = center + Vector2.left * detectionRadius;
        Vector2 rightPoint = center + Vector2.right * detectionRadius;
        Rect leftRect = new Rect(leftPoint.x - detectionRectWidth / 2, leftPoint.y - detectionRectHeight / 2, detectionRectWidth, detectionRectHeight);
        Rect rightRect = new Rect(rightPoint.x - detectionRectWidth / 2, rightPoint.y - detectionRectHeight / 2, detectionRectWidth, detectionRectHeight);
        Gizmos.DrawWireCube(leftRect.center, leftRect.size);
        Gizmos.DrawWireCube(rightRect.center, rightRect.size);
    }
}
