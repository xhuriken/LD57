using System.Collections;
using UnityEngine;

public class Bumper : MonoBehaviour
{
    [Header("Bumper Settings")]
    public float bumpForce = 5f;
    public float rotationSpeed = 15f;

    // Composants
    private Rigidbody2D m_rb;
    private Animator animator;
    private CircleCollider2D circleCollider;

    [Header("Utils")]
    private Vector3 dragOffset;
    private bool isDragged = false;

    [Header("Particules/SFX")]
    //Clips
    public AudioClip as_bump1;
    public AudioClip as_bump2;
    public AudioClip as_bump3;
    public AudioClip as_bump4;
    public AudioClip as_bump5;
    private AudioSource m_audioSource;

    private enum BumperState { Idle, Drag }
    private BumperState currentState = BumperState.Idle;

    void Start()
    {
        m_rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
        m_audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        switch (currentState)
        {
            case BumperState.Idle:

                if (Input.GetMouseButtonDown(1) && IsMouseOver())
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    dragOffset = transform.position - (Vector3)mouseWorldPos;
                    m_rb.velocity = Vector2.zero;
                    GameManager.Instance.isDragging = true;
                    isDragged = true;
                    Debug.Log("Drag started");
                    currentState = BumperState.Drag;
                }
                break;

            case BumperState.Drag:
                GameManager.Instance.isDragging = true;
                // Rotation avec la molette pendant le drag
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.001f)
                {
                    float rotationAmount = scroll * rotationSpeed;
                    transform.Rotate(0, 0, rotationAmount);
                    Debug.Log("Rotated by " + rotationAmount);
                }
                // Déplacement du bumper
                if (Input.GetMouseButton(1))
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    transform.position = mouseWorldPos + (Vector2)dragOffset;
                    m_rb.velocity = Vector2.zero;
                }
                // Fin du drag
                if (Input.GetMouseButtonUp(1))
                {
                    currentState = BumperState.Idle;
                    isDragged = false;
                    GameManager.Instance.isDragging = false;
                    Debug.Log("Drag ended, checking collisions");
                    CheckLayerCollisionAndRepulse();
                }
                break;
        }
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
        else if (col is PolygonCollider2D)
        {
            return col.OverlapPoint(mousePos);
        }
        else
        {
            return col.OverlapPoint(mousePos);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDragged)
            return;

        if (collision.gameObject.CompareTag("Ball") || collision.gameObject.CompareTag("FirstBall"))
        {
            Rigidbody2D ballRigidbody = collision.gameObject.GetComponent<Rigidbody2D>();
            if (ballRigidbody != null)
            {
                //Debug.Log("OnTriggerEnter2D: Ball collision with " + collision.gameObject.name);
                animator.SetTrigger("IsBump");
                AudioClip[] bumpSounds = { as_bump1, as_bump2, as_bump3, as_bump4, as_bump5 };
                int randomIndex = Random.Range(0, bumpSounds.Length);
                AudioClip as_bump = bumpSounds[randomIndex];

                m_audioSource.PlayOneShot(as_bump, 0.7f);
                
                Vector2 forceDirection = transform.up;
                ballRigidbody.AddForce(-forceDirection * bumpForce, ForceMode2D.Impulse);
            }
        }
        // Repulsion immédiate si collision avec un objet du layer "Objects" et que le bumper n'est pas en drag
        //else if (collision.gameObject.layer == LayerMask.NameToLayer("Objects") && !isDragged)
        //{
        //    Debug.Log("OnTriggerEnter2D: Object collision with " + collision.gameObject.name);
        //    RepulseWith(collision.gameObject);
        //}
    }

    private void CheckLayerCollisionAndRepulse()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            return;

        Collider2D[] hits = null;

        if (col is CircleCollider2D circle)
        {
            float radius = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            hits = Physics2D.OverlapCircleAll(transform.position, radius);
            Debug.Log("OverlapCircleAll count: " + (hits != null ? hits.Length : 0));
        }
        else if (col is PolygonCollider2D)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.SetLayerMask(LayerMask.GetMask("Objects"));
            Collider2D[] results = new Collider2D[10]; 
            int count = col.OverlapCollider(filter, results);
            Debug.Log("OverlapCollider count: " + count);
            hits = new Collider2D[count];
            for (int i = 0; i < count; i++)
            {
                hits[i] = results[i];
                Debug.Log("OverlapCollider hit: " + hits[i].gameObject.name);
            }
        }
        else
        {
            Vector2 size = col.bounds.size;
            float angle = transform.eulerAngles.z;
            hits = Physics2D.OverlapBoxAll(transform.position, size, angle);
            Debug.Log("OverlapBoxAll count: " + (hits != null ? hits.Length : 0));
        }

        if (hits == null)
            return;

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject != gameObject && hit.gameObject.layer == LayerMask.NameToLayer("Objects"))
            {
                Debug.Log("Found collision with: " + hit.gameObject.name);
                RepulseDraggedWith(hit.gameObject);
                break;
            }
        }
    }

 
    private void RepulseWith(GameObject other)
    {
        Rigidbody2D otherRb = other.GetComponent<Rigidbody2D>();

        bool thisWasKinematic = m_rb.isKinematic;
        bool otherWasKinematic = otherRb != null ? otherRb.isKinematic : false;
        Debug.Log("RepulseWith: " + gameObject.name + " and " + other.name +
                  ". thisWasKinematic: " + thisWasKinematic + ", otherWasKinematic: " + otherWasKinematic);

        if (m_rb.isKinematic)
        {
            m_rb.isKinematic = false;
            Debug.Log(gameObject.name + " set to dynamic");
        }
        if (otherRb != null && otherRb.isKinematic)
        {
            otherRb.isKinematic = false;
            Debug.Log(other.name + " rb set to dynamic");
        }

        Vector2 repulseDirection = (transform.position - other.transform.position).normalized;
        Debug.Log("Repulse direction: " + repulseDirection);
        m_rb.AddForce(repulseDirection * bumpForce, ForceMode2D.Impulse);
        if (otherRb != null)
        {
            otherRb.AddForce(-repulseDirection * bumpForce, ForceMode2D.Impulse);
        }

        StartCoroutine(ResetKinematicState(m_rb, thisWasKinematic));
        if (otherRb != null)
        {
            StartCoroutine(ResetKinematicState(otherRb, otherWasKinematic));
        }
    }

    // Méthode pour repulser uniquement le bumper qui était en drag (this)
    private void RepulseDraggedWith(GameObject other)
    {
        bool thisWasKinematic = m_rb.isKinematic;
        if (m_rb.isKinematic)
        {
            m_rb.isKinematic = false;
            Debug.Log(gameObject.name + " set to dynamic for dragged repulsion");
        }

        Vector2 repulseDirection = (transform.position - other.transform.position).normalized;
        Debug.Log("RepulseDraggedWith direction: " + repulseDirection);
        m_rb.AddForce(repulseDirection * bumpForce, ForceMode2D.Impulse);

        StartCoroutine(ResetKinematicState(m_rb, thisWasKinematic));
    }

    private IEnumerator ResetKinematicState(Rigidbody2D rb, bool originalState)
    {
        yield return new WaitForSeconds(0.15f);
        rb.velocity = Vector2.zero;
        rb.isKinematic = originalState;
        Debug.Log("Reset " + rb.gameObject.name + " to isKinematic: " + originalState);
        // Optionnel : vous pouvez refaire une vérification de collision ici
        CheckLayerCollisionAndRepulse();
    }
}
