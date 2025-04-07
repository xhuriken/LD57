using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Shapes;

public class Shop : MonoBehaviour
{
    [Header("Références")]
    public ParticleSystem particle1;
    public ParticleSystem particle2;
    public GameObject ballShopContainer;
    public Disc discComponent;
    private Rigidbody2D m_rb;
    private AudioSource m_audioSource;

    [Header("Paramètres")]
    public float spawnDelay = 0.1f;
    public float hideDelay = 0.1f;
    public float postHideDelay = 0.2f;
    public float moveDuration = 0.5f;
    public float radius = 1.5f;
    public float shopDetectionRadius = 0.8f;
    public float expelMultiplier = 2f;

    [Header("Disc Color Cycling")]
    public Color[] discColors;
    public float colorTransitionDuration = 2f;

    [Header("Drag & Repulse Settings")]
    public AudioClip as_cantplace;
    public float bumpForce = 5f;

    private float lastSoundTime = -Mathf.Infinity;
    private bool isShopActive = false;
    private bool isAnimating = false;
    private bool isDragged = false;
    private Vector3 dragOffset;

    private List<BallShop> ballShops = new List<BallShop>();
    private Color discCurrentColor;

    void Start()
    {
        discCurrentColor = discComponent.Color;
        m_rb = GetComponent<Rigidbody2D>();
        m_audioSource = GetComponent<AudioSource>();

        foreach (Transform child in ballShopContainer.transform)
        {
            BallShop bs = child.GetComponent<BallShop>();
            if (bs != null)
            {
                ballShops.Add(bs);
                bs.Initialize(this);
            }
        }

        if (discComponent != null && discColors != null && discColors.Length > 0)
        {
            StartCoroutine(CycleDiscColors());
        }
        else
        {
            Debug.Log("[Shop] No disc colors assigned!");
        }
    }

    void Update()
    {
        if (discComponent != null)
            discComponent.Color = discCurrentColor;

        HandleDrag();

        if (Input.GetMouseButtonDown(0))
        {
            if (!isAnimating && IsMouseOverShop())
            {
                if (!isShopActive)
                {
                    Debug.Log("[Shop] Activation du shop.");
                    ActivateShop();
                }
                else
                {
                    Debug.Log("[Shop] Fermeture du shop (clic sur le shop).");
                    HideShop();
                }
            }
        }
    }

    private void HandleDrag()
    {
        if (isShopActive || isAnimating)
            return;

        if (Input.GetMouseButtonDown(1) && IsMouseOverShop())
        {
            dragOffset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            isDragged = true;
        }

        if (isDragged && Input.GetMouseButton(1))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = mouseWorldPos + dragOffset;
            m_rb.velocity = Vector2.zero;
        }

        if (isDragged && Input.GetMouseButtonUp(1))
        {
            isDragged = false;
            CheckLayerCollisionAndRepulse();
        }
    }

    private bool IsMouseOverShop()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float distance = Vector2.Distance(mousePos, transform.position);
        return distance <= shopDetectionRadius;
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
                Debug.Log("[Shop] Repulse with: " + hit.gameObject.name);
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
            Debug.Log("[Shop] Rigidbody set to dynamic for repulsion");
        }

        Vector2 repulseDirection = (transform.position - other.transform.position).normalized;
        m_rb.AddForce(repulseDirection * bumpForce, ForceMode2D.Impulse);

        if (Time.time - lastSoundTime >= 2f)
        {
            if (as_cantplace != null)
            {
                m_audioSource.PlayOneShot(as_cantplace, 0.7f);
                Debug.Log("[Shop] Sound cantplace joué");
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

        Debug.Log("[Shop] Reset Rigidbody to Kinematic: " + originalState);

        CheckLayerCollisionAndRepulse();
    }

    IEnumerator CycleDiscColors()
    {
        Debug.Log("[Shop] Démarrage du cycle de couleurs du disc.");
        int index = 0;
        while (true)
        {
            Color targetColor = discColors[index];

            Debug.Log($"[Shop] Changement de couleur du disc : R:{targetColor.r} G:{targetColor.g} B:{targetColor.b}");

            Tweener colorTween = DOTween.To(
                () => discCurrentColor,
                x => discCurrentColor = x,
                targetColor,
                colorTransitionDuration
            ).SetEase(Ease.InOutSine);

            yield return colorTween.WaitForCompletion();

            index = (index + 1) % discColors.Length;
        }
    }

    void ActivateShop()
    {
        if (isAnimating) return;
        isShopActive = true;
        StartCoroutine(SpawnBallShops());
    }

    public void OnBallSelected(BallShop selectedBall)
    {
        Debug.Log("[Shop] Tentative d'achat : globalNumber = " + GameManager.Instance.globalNumber + " | Prix = " + selectedBall.identity.Price);
        if (GameManager.Instance.globalNumber >= selectedBall.identity.Price)
        {
            GameManager.Instance.globalNumber -= selectedBall.identity.Price;
            Debug.Log("[Shop] Achat réussi. Nouveau globalNumber = " + GameManager.Instance.globalNumber);
            StartCoroutine(HideBallShopsAndPurchase(selectedBall));
        }
        else
        {
            Debug.Log("[Shop] Fonds insuffisants pour acheter la ball " + selectedBall.identity.Price);
            selectedBall.FlashPriceTextRed();
        }
    }

    void HideShop()
    {
        if (isAnimating) return;
        StartCoroutine(HideAllBalls());
    }

    IEnumerator HideAllBalls()
    {
        isAnimating = true;
        Vector3 center = transform.position;
        for (int i = 0; i < ballShops.Count; i++)
        {
            BallShop ball = ballShops[i];
            ball.HideWithMoveAndScale(center, moveDuration);
            yield return new WaitForSeconds(hideDelay);
        }
        isShopActive = false;
        isAnimating = false;
        Debug.Log("[Shop] Shop fermé.");
    }

    IEnumerator SpawnBallShops()
    {
        isAnimating = true;
        int count = ballShops.Count;
        Vector3 center = transform.position;
        for (int i = 0; i < count; i++)
        {
            BallShop ball = ballShops[i];
            float angle = -(360f / count) * i + 90f;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 targetPos = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * radius;
            ball.SpawnWithMoveAndScale(targetPos, moveDuration);
            yield return new WaitForSeconds(spawnDelay);
        }
        isAnimating = false;
        Debug.Log("[Shop] Shop ouvert.");
    }

    IEnumerator HideBallShopsAndPurchase(BallShop selectedBall)
    {
        isAnimating = true;
        Vector3 center = transform.position;
        for (int i = 0; i < ballShops.Count; i++)
        {
            BallShop ball = ballShops[i];
            ball.HideWithMoveAndScale(center, moveDuration);
            yield return new WaitForSeconds(hideDelay);
        }
        yield return new WaitForSeconds(postHideDelay);

        Vector3 direction = (selectedBall.spawnTargetPosition - center).normalized;
        Vector3 finalPos = center + direction * (radius * expelMultiplier);

        GameObject instanceBall = selectedBall.identity.instanceBall;
        if (instanceBall != null)
        {
            Debug.Log("[Shop] Instanciation de la ball sélectionnée au centre.");
            GameObject newBall = Instantiate(instanceBall, center, Quaternion.identity);
            yield return StartCoroutine(ExpelObject(newBall, finalPos));
        }
        isShopActive = false;
        isAnimating = false;
        Debug.Log("[Shop] Transaction terminée, shop fermé.");
    }

    IEnumerator ExpelObject(GameObject obj, Vector3 finalPos)
    {

        IClickMachine clickMachine = obj.GetComponent<IClickMachine>();

        clickMachine.isInMachine = true;
        BlueBall blueBall = obj.GetComponent<BlueBall>();
        if (blueBall != null && blueBall.currentState != BlueBall.BlueBallState.Friction)
        {
            blueBall.currentState = BlueBall.BlueBallState.Friction;
        }

        float expelDuration = 1f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        float finalDistance = radius * expelMultiplier;

        if (discComponent != null)
        {
            discComponent.AngRadiansStart = 0f;
            discComponent.AngRadiansEnd = 2f * Mathf.PI;
        }

        while (elapsed < expelDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / expelDuration;
            obj.transform.position = Vector3.Lerp(startPos, finalPos, t);

            float distance = Vector2.Distance(obj.transform.position, transform.position);

            float gapAngleDegrees = 0f;
            if (distance <= radius)
            {
                gapAngleDegrees = Mathf.Lerp(0f, 80f, distance / radius);
            }
            else if (distance < finalDistance)
            {
                gapAngleDegrees = Mathf.Lerp(80f, 0f, (distance - radius) / (finalDistance - radius));
            }
            float gapAngle = gapAngleDegrees * Mathf.Deg2Rad;

            Vector2 dir = (obj.transform.position - transform.position).normalized;
            float directionAngle = Mathf.Atan2(dir.y, dir.x);

            if (discComponent != null)
            {
                discComponent.AngRadiansStart = directionAngle + gapAngle / 2f;
                discComponent.AngRadiansEnd = directionAngle - gapAngle / 2f + 2f * Mathf.PI;
            }

            yield return null;
        }

        if (discComponent != null)
        {
            discComponent.AngRadiansStart = 0f;
            discComponent.AngRadiansEnd = 2f * Mathf.PI;
        }

        clickMachine.isInMachine = false;
    }
}
