using System.Collections;
using UnityEngine;

public class ClickerMachine : MonoBehaviour
{
    // États de la machine
    private enum MachineState { WaitBall, Click }
    private MachineState currentState = MachineState.WaitBall;

    [Header("Ball Placement Settings")]
    // Point de placement de la balle (si non défini, le centre de la machine sera utilisé)
    public Transform placementPoint;
    // Décalage vertical pour le placement (la balle sera descendue de ce montant)
    public float placementYOffset = 0.5f;
    // Durée du glissement de la balle vers le placement
    public float slideDuration = 0.2f;
    // Délai avant d'activer l'événement de clic sur la balle
    public float clickDelay = 0.5f;

    [Header("Animation Settings")]
    // Animator de la machine pour déclencher une animation "Click" si nécessaire
    public Animator animator;

    // Référence à la balle actuellement placée (composant RedBall)
    private RedBall currentBall;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // N'accepte une nouvelle balle que si la machine est en état WaitBall
        if (currentState != MachineState.WaitBall)
            return;

        if (collision.CompareTag("Ball"))
        {
          

            // Lancer la coroutine pour glisser la balle et activer l'événement de clic
            StartCoroutine(SlideAndClick(collision.gameObject));
        }
    }

    /// <summary>
    /// Fait glisser la balle vers le point de placement puis, après un délai, appelle ActivateClickEvent() sur la balle.
    /// </summary>
    /// <param name="ball">La balle à traiter</param>
    private IEnumerator SlideAndClick(GameObject ball)
    {
        // Passage en état Click pour bloquer l'entrée d'autres balles
        currentState = MachineState.Click;

        // Calcul de la position cible avec décalage vertical
        Vector3 targetPosition = (placementPoint != null) ? placementPoint.position : transform.position;
        targetPosition.y -= placementYOffset;

        // Enregistrer la position de départ (peut servir pour d'autres calculs)
        Vector3 startPosition = ball.transform.position;

        // Désactiver la physique de la balle pendant le glissement
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

        // Rattacher la balle à la machine (optionnel)
        ball.transform.SetParent(transform);

        // Récupérer le composant RedBall de la balle
        currentBall = ball.GetComponent<RedBall>();

        // Déclencher une animation de click sur la machine (si assignée)
        if (animator != null)
        {
            animator.SetTrigger("Click");
            Debug.Log("Trigger 'Click' envoyé à l'Animator de la machine.");
        }

        // Attendre un délai avant d'activer l'événement de clic sur la balle
        yield return new WaitForSeconds(clickDelay);

        // Appeler la méthode publique de la balle pour déclencher son ClickEvent
        if (currentBall != null)
        {
            currentBall.ActivateClickEvent();
            Debug.Log("ActivateClickEvent() appelée sur la balle.");
        }

        // Revenir à l'état d'attente pour accepter une nouvelle balle
        currentState = MachineState.WaitBall;
    }
}
