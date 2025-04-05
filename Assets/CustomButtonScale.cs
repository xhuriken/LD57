using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Shapes;

public class CustomButtonScale : CustomButtonBase
{
    private const float OriginalScale = 1.0f;
    private Vector3 _originalTextPos;

    [SerializeField] private float toScale;
    [SerializeField] private float duration;
    [SerializeField] private float moveDistance = 1f;
    [SerializeField] private float ballAnimDuration = 0.5f;
    [SerializeField] private float ballTargetRadius = 15f;
    private GameObject _text;
    private GameObject _ball;
    private Animator _ballAnim;
    private Disc _ballDisc;
    private void Start()
    {
        _text = transform.GetChild(0).gameObject;
        _ball = transform.GetChild(1).gameObject;
        _ballAnim = _ball.GetComponent<Animator>();
        _ballDisc = _ball.GetComponent<Disc>();
        _originalTextPos = _text.transform.localPosition;

        _ballDisc.Radius = 0f;
        //Debug.Log(_ballDisc.Radius);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        Debug.Log("OnPointerEnter");
        //_ballAnim.SetTrigger("Show");

        _text.transform.DOScale(toScale, duration).SetEase(Ease.InOutBack);
        _text.transform.DOLocalMoveX(_originalTextPos.x + moveDistance, duration).SetEase(Ease.InOutBack);


        DOTween.To(() => _ballDisc.Radius, x => _ballDisc.Radius = x, ballTargetRadius, ballAnimDuration)
                .SetEase(Ease.OutBounce);
        _ball.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InOutBack);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        //_ballAnim.SetTrigger("Mask");

        _text.transform.DOScale(OriginalScale, duration).SetEase(Ease.InOutBack);
        _text.transform.DOLocalMoveX(_originalTextPos.x, duration).SetEase(Ease.InOutBack);

        DOTween.To(() => _ballDisc.Radius, x => _ballDisc.Radius = x, 0f, ballAnimDuration)
                .SetEase(Ease.InBack);
        _ball.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InOutBack);
    }
}
