using UnityEngine;
using DG.Tweening;

public class Letters : MonoBehaviour
{
    [Header("Hover Target Values")]
    public Vector3 hoverPosition;
    public Vector3 hoverRotation;
    public Vector3 hoverScale = Vector3.one;

    // Variables privées pour stocker l'état initial
    private Vector3 _startPosition;
    private Vector3 _startRotation;
    private Vector3 _startScale;

    private bool _isHovered = false;

    [Header("Tween Settings")]
    public float tweenDuration = 0.5f;
    public Ease tweenEase = Ease.InOutBack;

    [Header("SFX")]
    public AudioClip hover;
    private AudioSource m_audio;
    private void Start()
    {
        _startPosition = transform.localPosition;
        _startRotation = transform.localEulerAngles;
        _startScale = transform.localScale;

        m_audio = transform.parent.GetComponent<AudioSource>();
        //hoverPosition = _startPosition;
        //hoverRotation = _startRotation;
        //hoverScale = _startScale;   
    }

    private void Update()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            bool isNowHovered = col.OverlapPoint(mouseWorldPos);

            if (isNowHovered && !_isHovered)
            {
                OnHoverEnter();
                m_audio.PlayOneShot(hover);
            }
            else if (!isNowHovered && _isHovered)
            {
                OnHoverExit();
            }
        }
    }

    private void OnHoverEnter()
    {
        _isHovered = true;

        transform.DOLocalMove(hoverPosition, tweenDuration).SetEase(tweenEase);
        transform.DOLocalRotate(hoverRotation, tweenDuration).SetEase(tweenEase);
        transform.DOScale(hoverScale, tweenDuration).SetEase(tweenEase);
    }

    private void OnHoverExit()
    {
        _isHovered = false;

        transform.DOLocalMove(_startPosition, tweenDuration).SetEase(tweenEase);
        transform.DOLocalRotate(_startRotation, tweenDuration).SetEase(tweenEase);
        transform.DOScale(_startScale, tweenDuration).SetEase(tweenEase);
    }
}
