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

    private bool isDragged = false;
    private Vector3 dragOffset;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (currentState == MachineState.Drag)
        {
            if (Input.GetMouseButton(1))
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                transform.position = mouseWorldPos + dragOffset;
            }
            if (Input.GetMouseButtonUp(1))
            {
                isDragged = false;
                currentState = MachineState.WaitBall;
            }
            return;
        }
        if (Input.GetMouseButtonDown(1) && IsMouseOverMachine())
        {
            dragOffset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            isDragged = true;
            currentState = MachineState.Drag;
            return;
        }

        if (currentState != MachineState.WaitBall)
            return;

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
    }

    /// <summary>
    /// Méthode utilitaire pour détecter si la souris survole la machine (via son collider).
    /// </summary>
    private bool IsMouseOverMachine()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D col = GetComponent<Collider2D>();
        return (col != null && col.OverlapPoint(mousePos));
    }

    /// <summary>
    /// Fait glisser la balle vers le point de placement (avec offset vertical), déclenche l'animation "Click",
    /// puis passe à l'état Exit pour expulser la balle.
    /// </summary>
    private IEnumerator SlideAndClick(GameObject ball)
    {
       
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

        Debug.Log("Balle placée avec décalage au point: " + targetPosition);

      
        if (animator != null)
        {
            animator.SetTrigger("Click");
            Debug.Log("Trigger 'Click' envoyé.");
            
            INumber number = ball.GetComponent<INumber>();
            if (number != null)
                number.Click();
        }

        yield return new WaitForSeconds(clickDelay);

        currentState = MachineState.Exit;
        StartCoroutine(ExitBall(ball, ballStartPosition, targetPosition));
    }

    /// <summary>
    /// Détache la balle, réactive sa physique et lui applique une force pour l'expulser dans le sens opposé à son entrée, toujours horizontal.
    /// </summary>
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
            Debug.Log("Balle expulsée horizontalement dans la direction: " + horizontalDirection);
        }

        yield return new WaitForSeconds(exitDelay);

        currentState = MachineState.WaitBall;
        if (ball.GetComponent<IClickMachine>() != null)
            ball.GetComponent<IClickMachine>().isInMachine = false;
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
