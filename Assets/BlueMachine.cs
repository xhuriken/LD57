using System.Collections;
using UnityEngine;

public class BlueMachine : MonoBehaviour
{
    private enum MachineState { WaitBall, Click, Exit, Drag }
    private MachineState currentState = MachineState.WaitBall;

    [Header("Ball Placement Settings")]
    // Point de placement de la boule (si null, on utilisera le centre de la machine)
    public Transform placementPoint;
    // D�calage vertical pour le placement (la boule sera plac�e en dessous du point exact)
    public float placementYOffset = 0.5f;
    // Dur�e du glissement de la boule vers le placement
    public float slideDuration = 0.2f;
    // D�lai apr�s l'animation "Click" avant d'ex�cuter la sortie
    public float clickDelay = 0.5f;

    [Header("Detection Settings")]
    // Pour la d�tection par zones (si vous souhaitez reprendre l'ancien syst�me)
    public float detectionRadius = 1f;
    public float detectionRectWidth = 0.1f;
    public float detectionRectHeight = 3f;

    [Header("Animation Settings")]
    public Animator animator;

    [Header("Exit Settings")]
    // Force appliqu�e lors de l'expulsion de la boule
    public float bumpForce = 5f;
    // Temps d'attente suppl�mentaire apr�s l'expulsion avant de r�activer la d�tection
    public float exitDelay = 1f;

    [Header("Drag Settings")]
    // Variables pour le drag de la machine
    private bool isDragged = false;
    private Vector3 dragOffset;

    [Header("Prefab Settings")]
    // Pr�fabriqu� de la boule bleue (qui remplacera la boule rouge)
    public GameObject BluePrefab;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Priorit� au drag de la machine
        if (HandleMachineDrag())
            return;

        // On ne proc�de � la d�tection des balles que si la machine est en �tat d'attente
        if (currentState != MachineState.WaitBall)
            return;

        HandleBallDetection();
    }

    /// <summary>
    /// G�re le drag de la machine. Retourne true si le drag est actif ou vient de d�marrer.
    /// </summary>
    private bool HandleMachineDrag()
    {
        // Si la machine est d�j� en mode Drag, mettre � jour sa position
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
        // D�marrer le drag de la machine si on clique droit dessus
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
    /// V�rifie si la souris survole la machine (via son collider).
    /// </summary>
    private bool IsMouseOverMachine()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D col = GetComponent<Collider2D>();
        return (col != null && col.OverlapPoint(mousePos));
    }

    /// <summary>
    /// D�tection des balles � partir du centre de la machine.
    /// Ici, on accepte une balle venant de n'importe quel c�t� si sa distance au centre est inf�rieure � detectionRadius.
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
                // On ne traite qu'une seule balle � la fois.
                break;
            }
        }
    }

    /// <summary>
    /// Glisse la boule rouge vers le point de placement, lance l'animation et remplace la boule rouge par une bleue.
    /// </summary>
    private IEnumerator SlideAndClick(GameObject ball)
    {
        // Marquer la boule comme �tant dans la machine via son interface IClickMachine
        IClickMachine clickMachine = ball.GetComponent<IClickMachine>();
        if (clickMachine != null)
            clickMachine.isInMachine = true;

        currentState = MachineState.Click;

        // Calculer la position cible (placementPoint ou centre de la machine) avec le d�calage vertical
        Vector3 targetPosition = placementPoint != null ? placementPoint.position : transform.position;
        // targetPosition.y -= placementYOffset; // Si vous souhaitez appliquer un offset vertical, d�commentez cette ligne

        // Sauvegarder la position d'entr�e de la boule rouge
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

        Debug.Log("Boule rouge plac�e au point: " + targetPosition);

        // D�clencher l'animation de la machine (ici, le trigger "Turn")
        if (animator != null)
        {
            animator.SetTrigger("Turn");
            Debug.Log("Trigger 'Turn' envoy�.");
        }

        // Remplacer la boule rouge par une boule bleue
        GameObject blueBall = Instantiate(BluePrefab, transform.position, transform.rotation);
        Destroy(ball);

        yield return new WaitForSeconds(clickDelay);

        currentState = MachineState.Exit;
        StartCoroutine(ExitBall(blueBall, ballStartPosition, targetPosition));
    }

    /// <summary>
    /// D�tache la boule bleue, r�active sa physique et lui applique une force pour l'expulser horizontalement 
    /// dans le sens oppos� � l'entr�e de la boule rouge.
    /// </summary>
    private IEnumerator ExitBall(GameObject ball, Vector3 ballStartPosition, Vector3 targetPosition)
    {
        // D�lai avant l'expulsion
        yield return new WaitForSeconds(1f);
        // D�tacher la balle de la machine
        ball.transform.SetParent(null);
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Calculer la direction d'expulsion.
        // Ici, on souhaite expulser la balle du c�t� oppos� de son entr�e.
        // Par exemple, si la balle est entr�e par la gauche (ballStartPosition.x < targetPosition.x),
        // alors (targetPosition - ballStartPosition) sera orient� vers la droite et la balle sera expuls�e vers la droite.
        Vector2 exitDirection = (targetPosition - ballStartPosition).normalized;
        if (rb != null)
        {
            rb.AddForce(exitDirection * bumpForce, ForceMode2D.Impulse);
            Debug.Log("Balle expuls�e dans la direction: " + exitDirection);
        }

        yield return new WaitForSeconds(1f);

        currentState = MachineState.WaitBall;
        if (ball.GetComponent<IClickMachine>() != null)
            ball.GetComponent<IClickMachine>().isInMachine = false;
    }



    // Affichage en mode �diteur : dessin du cercle de d�tection pour le debug.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
