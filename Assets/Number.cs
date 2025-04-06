using UnityEngine;
using DG.Tweening;
using TMPro; // Ajoute pour TextMeshPro

public class Number : MonoBehaviour
{
    private Vector2 randomDirection;
    private float randomRotationSpeed;

    private TextMeshPro textMesh;

    void Start()
    {
        textMesh = GetComponentInChildren<TextMeshPro>();

        transform.localScale = Vector3.zero;

        randomDirection = (Vector2.up + new Vector2(Random.Range(-0.1f, 0.1f), 0f)).normalized;

        randomRotationSpeed = Random.Range(-90f, 90f);

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1f, 0.1f).SetEase(Ease.OutBack));
        seq.Join(transform.DOMoveY(transform.position.y + 1.7f, 0.5f).SetEase(Ease.OutSine));

        seq.AppendInterval(1f);

        seq.AppendCallback(() => {
            StartCoroutine(MoveUpAndRotate());
        });

        seq.Append(transform.DOScale(0f, 0.3f).SetEase(Ease.InBack));

        seq.OnComplete(() => Destroy(gameObject));
    }

    private System.Collections.IEnumerator MoveUpAndRotate()
    {
        float duration = 0.3f;
        float elapsed = 0f;

        float totalRotation = Random.Range(-180f, 180f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position += (Vector3)(randomDirection * Mathf.Lerp(1.5f, 0f, t)) * Time.deltaTime;

            transform.rotation = Quaternion.Euler(0f, 0f, totalRotation * t);

            yield return null;
        }
    }
}
