using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;

public class StockMachine : MonoBehaviour
{
    [SerializeField] private float actionRadius; // 2.5f
    [SerializeField] private List<string> objectInhalable;

    private enum StockMachineState { Idle, Inhale }
    private StockMachineState currentState = StockMachineState.Idle;
    private Animator m_animator;

    private Disc discComponent;
    [Header("Particules/SFX")]
    //Clips
    public AudioClip as_inhale1;
    public AudioClip as_inhale2;
    //Particules
    //Nothing
    private AudioSource m_audioSource;
    void Start()
    {
        m_animator = GetComponent<Animator>();
        discComponent = GetComponent<Disc>();
        m_audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        switch (currentState)
        {
            case StockMachineState.Idle:
                Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, actionRadius);
                foreach (Collider2D col in colliders)
                {
                    if (objectInhalable.Contains(col.tag))
                    {
                        currentState = StockMachineState.Inhale;
                        StartCoroutine(InhaleObject(col.gameObject));
                        break;
                    }
                }
                break;

            case StockMachineState.Inhale:
                break;
        }
    }

    private IEnumerator InhaleObject(GameObject obj)
    {
        Data prop = obj.GetComponent<Data>();
        if (prop != null)
        {
            prop.isInhaled = true;
        }

        float moveDuration = 1f;
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;
        Vector3 targetPos = transform.position;
        obj.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        float maxGapAngleDegrees = 80f;
        float stockMachineRadius = 1f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            obj.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / moveDuration);

            float distance = Vector2.Distance(obj.transform.position, transform.position);


            float ratio = 0f;
            if (distance >= actionRadius)
            {
                ratio = 0f;
            }
            else if (distance > stockMachineRadius)
            {

                ratio = Mathf.InverseLerp(actionRadius, stockMachineRadius, distance);
            }
            else
            {

                ratio = Mathf.InverseLerp(0f, stockMachineRadius, distance);
            }

            //Bonjour, je suis un peu nul en math, orevoir

            // Convertit le ratio en degrés de trou
            float gapAngleDegrees = ratio * maxGapAngleDegrees;
            float gapAngle = gapAngleDegrees * Mathf.Deg2Rad; // en radians

            // Calcul de l'angle directionnel (en radians) du centre vers la balle
            Vector2 direction = (obj.transform.position - transform.position).normalized;
            float directionAngle = Mathf.Atan2(direction.y, direction.x);

            // Mise à jour des valeurs du Disc (AngRadiansStart, AngRadiansEnd)
            if (discComponent != null)
            {
                // Centrage du "trou" autour de directionAngle
                discComponent.AngRadiansStart = directionAngle + gapAngle / 2f;
                discComponent.AngRadiansEnd = directionAngle - gapAngle / 2f + 2f * Mathf.PI;
            }

            yield return null;
        }
        prop.isFreeze = true;
        int randomIndex = Random.Range(0, 2);
        AudioClip as_inhale = (randomIndex == 0) ? as_inhale1 : as_inhale2;
        m_audioSource.PlayOneShot(as_inhale, 0.7f);
        m_animator.SetTrigger("Bounce");
        yield return new WaitForSeconds(0.5f);

        if (discComponent != null)
        {
            discComponent.AngRadiansStart = 0f;
            discComponent.AngRadiansEnd = 2f * Mathf.PI;
        }

        Destroy(obj);

        currentState = StockMachineState.Idle;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, actionRadius);
    }
}
