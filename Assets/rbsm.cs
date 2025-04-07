using System.Collections;
using UnityEngine;

public class rbsm : MonoBehaviour
{
    [Header("rbsm Settings")]
    public float bumpForce = 5f;
    public float rotationSpeed = 15f;

    [Header("Creation Settings")]
    public GameObject createPrefab;   
    public float createProjectileForce = 5f;
    public float createInterval = 10f;
    private float createTimer = 0f;
    public Transform sp;

    // Composants
    private Rigidbody2D m_rb;
    private Animator animator;
    private CircleCollider2D circleCollider;

    [Header("Utils")]
    private Vector3 dragOffset;
    private bool isDragged = false;
    private float lastSoundTime = -Mathf.Infinity;

    [Header("Particules/SFX")]
    //Clips
    public AudioClip as_spawn;
    public AudioClip as_cantplace;
    //Particules
    //Nothing
    private AudioSource m_audioSource;


    private enum RbsmState { Idle, Drag, Create }
    private RbsmState currentState = RbsmState.Idle;
    private int cardinalDirectionIndex = 0; 

    void Start()
    {
        m_rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
        m_audioSource = GetComponent<AudioSource>();
        sp = transform.GetChild(6);
    }

    void Update()
    {
        switch (currentState)
        {
            case RbsmState.Idle:
                if (GameManager.Instance.menuShown)
                    return;
                if (Input.GetMouseButtonDown(1) && IsMouseOver())
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    dragOffset = transform.position - (Vector3)mouseWorldPos;
                    m_rb.velocity = Vector2.zero;
                    GameManager.Instance.isDragging = true;
                    isDragged = true;
                    Debug.Log("Drag started");
                    currentState = RbsmState.Drag;
                }
                else
                {
               
                    createTimer += Time.deltaTime;
                    if (createTimer >= createInterval)
                    {
                        createTimer = 0f;
                        currentState = RbsmState.Create;
                    }
                }
                break;

            case RbsmState.Drag:
                if (GameManager.Instance.menuShown)
                    return;
                GameManager.Instance.isDragging = true;
            
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.001f)
                {
               
                    if (scroll > 0)
                    {
                        cardinalDirectionIndex = (cardinalDirectionIndex + 1) % 4;
                    }
                    else
                    {
                        cardinalDirectionIndex = (cardinalDirectionIndex + 3) % 4; 
                    }
                    
                    float newAngle = cardinalDirectionIndex * 90f;
                    transform.rotation = Quaternion.Euler(0, 0, newAngle);
                    Debug.Log("Rotation réglée sur " + newAngle + " degrés");
                }
     
                if (Input.GetMouseButton(1))
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    transform.position = mouseWorldPos + (Vector2)dragOffset;
                    m_rb.velocity = Vector2.zero;
                }
       
                if (Input.GetMouseButtonUp(1))
                {
                    currentState = RbsmState.Idle;
                    isDragged = false;
                    GameManager.Instance.isDragging = false;
                    Debug.Log("Drag ended, checking collisions");
                    CheckLayerCollisionAndRepulse();
                }
                break;

            case RbsmState.Create:
                
                animator.SetTrigger("IsCreate");
                currentState = RbsmState.Idle;
                break;
        }
    }


    private void Spawn()
    {
        if (createPrefab != null)
        {
            // Instanciation du prefab à la position sp.position avec la rotation actuelle
            GameObject projectile = Instantiate(createPrefab, sp.position, transform.rotation);

            // Si l'objet possède le composant SaveableObject, réassigner son ID
            SaveableObject so = projectile.GetComponent<SaveableObject>();
            if (so != null)
            {
                so.SetUniqueId(System.Guid.NewGuid().ToString());
            }

            Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
            if (projRb != null)
            {
                projRb.AddForce(transform.right * createProjectileForce, ForceMode2D.Impulse);
                m_audioSource.PlayOneShot(as_spawn);
            }
            Debug.Log("Projectile créé dans la direction " + transform.rotation.eulerAngles.z + "°");
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

        if (Time.time - lastSoundTime >= 2f)
        {
            m_audioSource.PlayOneShot(as_cantplace, 0.7f);
            lastSoundTime = Time.time;
        }

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
