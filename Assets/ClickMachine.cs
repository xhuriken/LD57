using System.Collections;
using UnityEngine;

public class ClickerMachine : MonoBehaviour
{
    // �tats de la machine
    private enum MachineState { WaitBall, Click }
    private MachineState currentState = MachineState.WaitBall;

    [Header("Ball Placement Settings")]
    // Point de placement de la balle (si non d�fini, le centre de la machine sera utilis�)
    public Transform placementPoint;
    // D�calage vertical pour le placement (la balle sera descendue de ce montant)
    public float placementYOffset = 0.5f;
    // Dur�e du glissement de la balle vers le placement
    public float slideDuration = 0.2f;
    // D�lai avant d'activer l'�v�nement de clic sur la balle
    public float clickDelay = 0.5f;

    [Header("Animation Settings")]
    // Animator de la machine pour d�clencher une animation "Click" si n�cessaire
    public Animator animator;

    // R�f�rence � la balle actuellement plac�e (composant RedBall)
    private RedBall currentBall;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // N'accepte une nouvelle balle que si la machine est en �tat WaitBall
        if (currentState != MachineState.WaitBall)
            return;

        if (collision.CompareTag("Ball"))
        {
          

            // Lancer la coroutine pour glisser la balle et activer l'�v�nement de clic
            StartCoroutine(SlideAndClick(collision.gameObject));
        }
    }

    /// <summary>
    /// Fait glisser la balle vers le point de placement puis, apr�s un d�lai, appelle ActivateClickEvent() sur la balle.
    /// </summary>
    /// <param name="ball">La balle � traiter</param>
    private IEnumerator SlideAndClick(GameObject ball)
    {
        // Passage en �tat Click pour bloquer l'entr�e d'autres balles
        currentState = MachineState.Click;

        // Calcul de la position cible avec d�calage vertical
        Vector3 targetPosition = (placementPoint != null) ? placementPoint.position : transform.position;
        targetPosition.y -= placementYOffset;

        // Enregistrer la position de d�part (peut servir pour d'autres calculs)
        Vector3 startPosition = ball.transform.position;

        // D�sactiver la physique de la balle pendant le glissement
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        // Interpoler la position de la balle entre startPosition et targetPosition
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            ball.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / slideDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        ball.transform.position = targetPosition;

        // Rattacher la balle � la machine (optionnel)
        ball.transform.SetParent(transform);

        // R�cup�rer le composant RedBall de la balle
        currentBall = ball.GetComponent<RedBall>();

        // D�clencher une animation de click sur la machine (si assign�e)
        if (animator != null)
        {
            animator.SetTrigger("Click");
            Debug.Log("Trigger 'Click' envoy� � l'Animator de la machine.");
        }

        // Attendre un d�lai avant d'activer l'�v�nement de clic sur la balle
        yield return new WaitForSeconds(clickDelay);

        // Appeler la m�thode publique de la balle pour d�clencher son ClickEvent
        if (currentBall != null)
        {
            currentBall.ActivateClickEvent();
            Debug.Log("ActivateClickEvent() appel�e sur la balle.");
        }

        // Revenir � l'�tat d'attente pour accepter une nouvelle balle
        currentState = MachineState.WaitBall;
    }
}
