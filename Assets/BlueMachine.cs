using System.Collections;
using UnityEngine;

public class BlueMachine : MonoBehaviour
{
    private enum MachineState { WaitBall, Click, Exit, Drag }
    private MachineState currentState = MachineState.WaitBall;

    [Header("Ball Placement Settings")]
    // Point de placement de la boule (si null, on utilisera le centre de la machine)
    public Transform placementPoint;
    // Décalage vertical pour le placement (la boule sera placée en dessous du point exact)
    public float placementYOffset = 0.5f;
    // Durée du glissement de la boule vers le placement
    public float slideDuration = 0.2f;
    // Délai après l'animation "Click" avant d'exécuter la sortie
    public float clickDelay = 0.5f;

    [Header("Detection Settings")]
    // Pour la détection par zones (si vous souhaitez reprendre l'ancien système)
    public float detectionRadius = 1f;
    public float detectionRectWidth = 0.1f;
    public float detectionRectHeight = 3f;

    [Header("Animation Settings")]
    public Animator animator;

    [Header("Exit Settings")]
    // Force appliquée lors de l'expulsion de la boule
    public float bumpForce = 5f;
    // Temps d'attente supplémentaire après l'expulsion avant de réactiver la détection
    public float exitDelay = 1f;

    [Header("Drag Settings")]
    // Variables pour le drag de la machine
    private bool isDragged = false;
    private Vector3 dragOffset;

    [Header("Prefab Settings")]
    // Préfabriqué de la boule bleue (qui remplacera la boule rouge)
    public GameObject BluePrefab;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Priorité au drag de la machine
        if (HandleMachineDrag())
            return;

        // On ne procède à la détection des balles que si la machine est en état d'attente
        if (currentState != MachineState.WaitBall)
            return;

        HandleBallDetection();
    }

    /// <summary>
    /// Gère le drag de la machine. Retourne true si le drag est actif ou vient de démarrer.
    /// </summary>
    private bool HandleMachineDrag()
    {
        // Si la machine est déjà en mode Drag, mettre à jour sa position
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
            return true;
        }
        // Démarrer le drag de la machine si on clique droit dessus
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

    /// <summary>
    /// Vérifie si la souris survole la machine (via son collider).
    /// </summary>
    private bool IsMouseOverMachine()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D col = GetComponent<Collider2D>();
        return (col != null && col.OverlapPoint(mousePos));
    }

    /// <summary>
    /// Détection des balles à partir du centre de la machine.
    /// Ici, on accepte une balle venant de n'importe quel côté si sa distance au centre est inférieure à detectionRadius.
    /// </summary>
    private void HandleBallDetection()
    {
        Vector2 center = transform.position;
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject ball in balls)
        {
            IClickMachine clickMachine = ball.GetComponent<IClickMachine>();
            if (clickMachine != null && clickMachine.isInMachine)
                continue;

            // Calculer la distance entre la balle et le centre de la machine
            float distance = Vector2.Distance(center, ball.transform.position);
            if (distance <= detectionRadius)
            {
                StartCoroutine(SlideAndClick(ball));
                // On ne traite qu'une seule balle à la fois.
                break;
            }
        }
    }

    /// <summary>
    /// Glisse la boule rouge vers le point de placement, lance l'animation et remplace la boule rouge par une bleue.
    /// </summary>
    private IEnumerator SlideAndClick(GameObject ball)
    {
        // Marquer la boule comme étant dans la machine via son interface IClickMachine
        IClickMachine clickMachine = ball.GetComponent<IClickMachine>();
        if (clickMachine != null)
            clickMachine.isInMachine = true;

        currentState = MachineState.Click;

        // Calculer la position cible (placementPoint ou centre de la machine) avec le décalage vertical
        Vector3 targetPosition = placementPoint != null ? placementPoint.position : transform.position;
        // targetPosition.y -= placementYOffset; // Si vous souhaitez appliquer un offset vertical, décommentez cette ligne

        // Sauvegarder la position d'entrée de la boule rouge
        Vector3 ballStartPosition = ball.transform.position;

        float elapsed = 0f;
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        // Animation de glissement
        while (elapsed < slideDuration)
        {
            ball.transform.position = Vector3.Lerp(ballStartPosition, targetPosition, elapsed / slideDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        ball.transform.position = targetPosition;
        ball.transform.SetParent(transform);

        Debug.Log("Boule rouge placée au point: " + targetPosition);

        // Déclencher l'animation de la machine (ici, le trigger "Turn")
        if (animator != null)
        {
            animator.SetTrigger("Turn");
            Debug.Log("Trigger 'Turn' envoyé.");
        }

        // Remplacer la boule rouge par une boule bleue
        GameObject blueBall = Instantiate(BluePrefab, transform.position, transform.rotation);
        Destroy(ball);

        yield return new WaitForSeconds(clickDelay);

        currentState = MachineState.Exit;
        StartCoroutine(ExitBall(blueBall, ballStartPosition, targetPosition));
    }

    /// <summary>
    /// Détache la boule bleue, réactive sa physique et lui applique une force pour l'expulser horizontalement 
    /// dans le sens opposé à l'entrée de la boule rouge.
    /// </summary>
    private IEnumerator ExitBall(GameObject ball, Vector3 ballStartPosition, Vector3 targetPosition)
    {
        // Délai avant l'expulsion
        yield return new WaitForSeconds(1f);
        // Détacher la balle de la machine
        ball.transform.SetParent(null);
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Calculer la direction d'expulsion.
        // Ici, on souhaite expulser la balle du côté opposé de son entrée.
        // Par exemple, si la balle est entrée par la gauche (ballStartPosition.x < targetPosition.x),
        // alors (targetPosition - ballStartPosition) sera orienté vers la droite et la balle sera expulsée vers la droite.
        Vector2 exitDirection = (targetPosition - ballStartPosition).normalized;
        if (rb != null)
        {
            rb.AddForce(exitDirection * bumpForce, ForceMode2D.Impulse);
            Debug.Log("Balle expulsée dans la direction: " + exitDirection);
        }

        yield return new WaitForSeconds(1f);

        currentState = MachineState.WaitBall;
        if (ball.GetComponent<IClickMachine>() != null)
            ball.GetComponent<IClickMachine>().isInMachine = false;
    }



    // Affichage en mode éditeur : dessin du cercle de détection pour le debug.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
