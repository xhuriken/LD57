using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Shapes;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
public class Shop : MonoBehaviour
{
    [Header("Références")]
    public ParticleSystem particle1;
    public ParticleSystem particle2;
    public GameObject ballShopContainer;
    public Disc discComponent;

    [Header("Paramètres")]
    public float spawnDelay = 0.1f;
    public float hideDelay = 0.1f;
    public float postHideDelay = 0.2f;
    public float moveDuration = 0.5f;
    public float radius = 1.5f;
    public float shopDetectionRadius = 0.8f;

    [Header("Disc Color Cycling")]
    public Color[] discColors;
    public float colorTransitionDuration = 2f;

    public float expelMultiplier = 2f; 

    private bool isShopActive = false;
    private bool isAnimating = false; 
    private List<BallShop> ballShops = new List<BallShop>();
    private Color[] correctedDiscColors;
    private Color discCurrentColor;
    void Start()
    {
        discCurrentColor = discComponent.Color;

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
            Debug.Log("zob");
        }
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




    private bool IsMouseOverShop()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float distance = Vector2.Distance(mousePos, transform.position);
        return distance <= shopDetectionRadius;
    }

    void Update()
    {
        if (discComponent != null)
            discComponent.Color = discCurrentColor;

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
    }
}
