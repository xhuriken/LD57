using System.Collections;
using UnityEngine;

public class ClickerMachine : MonoBehaviour
{ 
    private enum MachineState { WaitBall, Click, Exit }
    private MachineState currentState = MachineState.WaitBall;

    [Header("Ball Placement Settings")]

    public Transform placementPoint;

    public float placementYOffset = 0.5f;
 
    public float slideDuration = 0.2f;
    
    public float clickDelay = 0.5f;

    [Header("Animation Settings")]
 
    public Animator animator;

    [Header("Exit Settings")]

    public float bumpForce = 5f;
   

    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
     
        if (currentState != MachineState.WaitBall)
            return;

        if (collision.CompareTag("Ball"))
        {
           
          
            StartCoroutine(SlideAndClick(collision.gameObject));
        }
    }

    /// <summary>
    /// Fait glisser la balle vers le point de placement avec un offset, déclenche l'animation click,
    /// puis passe à l'état Exit.
    /// </summary>
    /// <param name="ball">La balle à traiter</param>
    private IEnumerator SlideAndClick(GameObject ball)
    {
      
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

            ball.GetComponent<INumber>().Click();
        }

      
        yield return new WaitForSeconds(clickDelay);

 
        currentState = MachineState.Exit;
        StartCoroutine(ExitBall(ball, ballStartPosition, targetPosition));
    }

    /// <summary>
    /// Détache la balle, réactive sa physique et lui applique une force dans le sens d'entrée.
    /// </summary>
    /// <param name="ball">La balle à expulser</param>
    /// <param name="ballStartPosition">Position initiale de la balle lors de l'entrée</param>
    /// <param name="targetPosition">Position finale de la balle dans la machine</param>
    private IEnumerator ExitBall(GameObject ball, Vector3 ballStartPosition, Vector3 targetPosition)
    {
     
        ball.transform.SetParent(null);

  
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        Vector2 exitDirection = (targetPosition - ballStartPosition).normalized;
        if (rb != null)
        {
            rb.AddForce(exitDirection * bumpForce, ForceMode2D.Impulse);
            Debug.Log("Balle expulsée dans la direction: " + exitDirection);
        }

        yield return new WaitForSeconds(0.1f);

        currentState = MachineState.WaitBall;
    }
}