using DG.Tweening;
using Febucci.UI;
using Shapes;
using System.Collections;
using TMPro;
using UnityEngine;

[System.Serializable]
public class BallShopIdentity
{
    public bool canBuy;
    public int Price = 1;
    public GameObject instanceBall;
}

public class BallShop : MonoBehaviour
{
    public BallShopIdentity identity;
    public TextMeshPro priceText;

    private Shop parentShop;
    private Vector3 originalScale;
    private Color originalColor;
    private Disc disc;
    private TypewriterByCharacter typeWriter;

    private Tweener scaleTween;
    private Tweener colorTween;
    private bool canHover = false;
    private bool isHovering = false;
    private bool isFlashing = false;

    public Vector3 spawnTargetPosition;

    void Awake()
    {
        originalScale = new Vector3(1f, 1f, 1f);
        disc = GetComponent<Disc>();
        if (disc != null)
            originalColor = disc.Color;

        typeWriter = GetComponentInChildren<TypewriterByCharacter>();
        transform.localScale = Vector3.zero;
    }

    public void Initialize(Shop shop)
    {
        parentShop = shop;

        if (!identity.canBuy)
        {
            if (disc != null)
            {
                disc.Color = Color.gray;
            }

            if (priceText != null)
            {
                priceText.color = Color.gray;
            }
        }
    }

    public void SpawnWithMoveAndScale(Vector3 targetPosition, float duration)
    {
        transform.DOKill();
        transform.position = parentShop.transform.position;
        transform.localScale = Vector3.zero;
        spawnTargetPosition = targetPosition;

        transform.DOMove(targetPosition, duration)
            .SetEase(Ease.OutBounce);
        transform.DOScale(originalScale, duration)
            .SetEase(Ease.OutBounce)
            .OnComplete(() =>
            {
                canHover = true;
                Debug.Log("[Ball " + identity.Price + "] Spawn terminé, hover activé.");
            });
    }

    public void HideWithMoveAndScale(Vector3 centerPosition, float duration)
    {
        transform.DOKill();
        canHover = false;
        transform.DOMove(centerPosition, duration)
            .SetEase(Ease.InBack);
        transform.DOScale(Vector3.zero, duration)
            .SetEase(Ease.InBack);
    }

    public bool IsMouseOver()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            return false;
        if (col is CircleCollider2D circle)
        {
            float rad = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            return Vector2.Distance(transform.position, mousePos) <= rad;
        }
        else
        {
            return col.OverlapPoint(mousePos);
        }
    }

    void Update()
    {
        if (!canHover)
            return;
        if (!identity.canBuy)
        {
            return;
        }
        bool mouseOver = IsMouseOver();
        if (mouseOver)
        {
            if (!isHovering)
            {
                isHovering = true;
                Debug.Log("[Ball " + identity.Price + "] Survol débuté.");

                ApplyHoverEffects();
            }
            if (Input.GetMouseButtonDown(0) && !isFlashing)
            {

                Debug.Log("[Ball " + identity.Price + "] Cliquée.");
                parentShop.OnBallSelected(this);
            }
        }
        else
        {
            if (isHovering)
            {   
                isHovering = false;
                Debug.Log("[Ball " + identity.Price + "] Survol terminé.");
                RemoveHoverEffects();
            }
        }
    }

    void ApplyHoverEffects()
    {
        if (scaleTween != null) scaleTween.Kill();
        if (colorTween != null) colorTween.Kill();

        scaleTween = transform.DOScale(originalScale * 1.2f, 0.2f)
            .SetEase(Ease.OutQuad);
        if (disc != null)
        {
            Color targetColor = new Color(
                Mathf.Min(originalColor.r + 0.2f, 1f),
                Mathf.Min(originalColor.g + 0.2f, 1f),
                Mathf.Min(originalColor.b + 0.2f, 1f),
                originalColor.a
            );
            colorTween = DOTween.To(
                () => disc.Color,
                x => disc.Color = x,
                targetColor,
                0.2f
            ).SetEase(Ease.OutQuad);
        }
        if (typeWriter != null)
        {
            Debug.Log("[Ball " + identity.Price + "] Affichage du texte.");
            typeWriter.ShowText(identity.Price.ToString());
        }
    }

    void RemoveHoverEffects()
    {
        if (scaleTween != null) scaleTween.Kill();
        if (colorTween != null) colorTween.Kill();

        scaleTween = transform.DOScale(originalScale, 0.2f)
            .SetEase(Ease.OutQuad);
        if (disc != null)
        {
            colorTween = DOTween.To(
                () => disc.Color,
                x => disc.Color = x,
                originalColor,
                0.2f
            ).SetEase(Ease.OutQuad);
        }
        if (typeWriter != null)
        {
            Debug.Log("[Ball " + identity.Price + "] Masquage du texte.");
            typeWriter.StartDisappearingText();
        }
    }

    public void FlashPriceTextRed()
    {
        if (isFlashing)
            return;
        isFlashing = true;
        if (priceText != null)
        {
            Color originalTextColor = priceText.color;
            Debug.Log("[Ball " + identity.Price + "] Flash rouge (fonds insuffisants).");
            priceText.DOColor(Color.red, 0.1f).OnComplete(() =>
            {
                priceText.DOColor(originalTextColor, 0.1f).OnComplete(() =>
                {
                    isFlashing = false;
                });
            });
        }
        else
        {
            isFlashing = false;
        }
    }
}
