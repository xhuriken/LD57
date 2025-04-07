using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;
using TMPro;

public class StockMachine : MonoBehaviour
{
    [SerializeField] private float actionRadius; // 2.5f
    [SerializeField] private List<string> objectInhalable;

    private enum StockMachineState { Idle, Inhale, Drag }
    private StockMachineState currentState = StockMachineState.Idle;

    private Animator m_animator;
    private Disc discComponent;
    private AudioSource m_audioSource;

    [Header("Particules/SFX")]
    public AudioClip as_inhale1;
    public AudioClip as_inhale2;
    private float lastSoundTime = -Mathf.Infinity; 
    public AudioClip as_cantplace;

    public GameObject number;

    private Rigidbody2D m_rb;
    private Vector3 dragOffset;
    private bool isDragged = false;
    private float bumpForce = 5f; 

    void Start()
    {
        m_animator = GetComponent<Animator>();
        discComponent = GetComponent<Disc>();
        m_audioSource = GetComponent<AudioSource>();
        m_rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        switch (currentState)
        {
            case StockMachineState.Idle:
                if (GameManager.Instance.CraftMode)
                    break;

                if (Input.GetMouseButtonDown(1) && IsMouseOver())
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    dragOffset = transform.position - (Vector3)mouseWorldPos;
                    m_rb.velocity = Vector2.zero;
                    GameManager.Instance.isDragging = true;
                    isDragged = true;
                    currentState = StockMachineState.Drag;
                    Debug.Log("[StockMachine] Drag started");
                }
                else
                {
                    Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, actionRadius);
                    foreach (Collider2D col in colliders)
                    {
                        if (objectInhalable.Contains(col.tag))
                        {
                            if (col.GetComponent<FirstBall>() != null)
                            {
                                Debug.Log("[StockMachine] Skipping " + col.gameObject.name);
                                continue;
                            }

                            currentState = StockMachineState.Inhale;
                            StartCoroutine(InhaleObject(col.gameObject));
                            break;
                        }
                    }
                }
                break;

            case StockMachineState.Inhale:
                break;

            case StockMachineState.Drag:
                if (Input.GetMouseButton(1))
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    transform.position = mouseWorldPos + (Vector2)dragOffset;
                    m_rb.velocity = Vector2.zero;
                }

                if (Input.GetMouseButtonUp(1))
                {
                    isDragged = false;
                    GameManager.Instance.isDragging = false;
                    currentState = StockMachineState.Idle;
                    Debug.Log("[StockMachine] Drag ended, checking collisions");
                    CheckLayerCollisionAndRepulse();
                }
                break;
        }
    }

    private IEnumerator InhaleObject(GameObject obj)
    {
        Data prop = obj.GetComponent<Data>();
        if (prop != null)
            prop.isInhaled = true;

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

            float gapAngleDegrees = ratio * maxGapAngleDegrees;
            float gapAngle = gapAngleDegrees * Mathf.Deg2Rad;

            Vector2 direction = (obj.transform.position - transform.position).normalized;
            float directionAngle = Mathf.Atan2(direction.y, direction.x);

            if (discComponent != null)
            {
                discComponent.AngRadiansStart = directionAngle + gapAngle / 2f;
                discComponent.AngRadiansEnd = directionAngle - gapAngle / 2f + 2f * Mathf.PI;
            }

            yield return null;
        }

        if (prop != null)
            prop.isFreeze = true;

        int randomIndex = Random.Range(0, 2);
        AudioClip as_inhale = (randomIndex == 0) ? as_inhale1 : as_inhale2;
        m_audioSource.PlayOneShot(as_inhale, 0.7f);
        m_animator.SetTrigger("Bounce");

        INumber numberComponent = obj.GetComponent<INumber>();
        int numberToAdd = 1;
        if (numberComponent != null)
        {
            numberToAdd = numberComponent.Number;
        }
        else
        {
            Debug.Log("[StockMachine] (Set 1) No INumber number initialized for: " + obj.name);
        }

        GameManager.Instance.AddNumber(numberToAdd);

        GameObject NumberTextFloating = Instantiate(number, transform.position, Quaternion.identity);
        TextMeshPro textMesh = NumberTextFloating.GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = numberToAdd.ToString();
        }
        else
        {
            Debug.LogWarning("[StockMachine] Aucun TextMeshPro trouvé dans Number instancié !");
        }

        yield return new WaitForSeconds(0.5f);

        if (discComponent != null)
        {
            discComponent.AngRadiansStart = 0f;
            discComponent.AngRadiansEnd = 2f * Mathf.PI;
        }

        Destroy(obj);

        currentState = StockMachineState.Idle;
    }

    private bool IsMouseOver()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            return false;

        if (col is CircleCollider2D circle)
        {
            float radius = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            return Vector2.Distance(transform.position, mousePos) <= radius;
        }
        else
        {
            return col.OverlapPoint(mousePos);
        }
    }

    private void CheckLayerCollisionAndRepulse()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, col.bounds.extents.x);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject != gameObject && hit.gameObject.layer == LayerMask.NameToLayer("Objects"))
            {
                Debug.Log("[StockMachine] Repulse with: " + hit.gameObject.name);
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
            Debug.Log("[StockMachine] Rigidbody set to dynamic for repulsion");
        }

        Vector2 repulseDirection = (transform.position - other.transform.position).normalized;
        m_rb.AddForce(repulseDirection * bumpForce, ForceMode2D.Impulse);

        if (Time.time - lastSoundTime >= 2f)
        {
            if (as_cantplace != null)
            {
                m_audioSource.PlayOneShot(as_cantplace, 0.7f);
                Debug.Log("[StockMachine] Sound cantplace joué");
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

        Debug.Log("[StockMachine] Reset Rigidbody to Kinematic: " + originalState);

        CheckLayerCollisionAndRepulse();
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, actionRadius);
    }
}
