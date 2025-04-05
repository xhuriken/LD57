using UnityEngine;
using UnityEngine.EventSystems;

public abstract class CustomButtonBase : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{

    public AudioClip hover;
    public AudioClip pressed;
    public AudioSource m_audio;

    private void Start()
    {

    }
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        // Set Cursor
        m_audio.PlayOneShot(hover);
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        // Play audio
        m_audio.PlayOneShot(pressed);

    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        // Set Cursor
    }
}
