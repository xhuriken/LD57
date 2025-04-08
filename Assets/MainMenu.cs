using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.Audio;
using Unity.Profiling;
using TMPro;

public class MainMenu : MonoBehaviour
{
    private RectTransform LeftSection;
    private RectTransform Icons;
    private RectTransform Logo;
    private List<RectTransform> Buttons = new List<RectTransform>();
    public GameObject transitionObj;
    private RectTransform Button;
    private RectTransform Settings;

    [Header("Hide Positions")]
    // Utilisation de Vector2 pour les positions UI
    public Vector2 hide_leftSection;
    public Vector2 hide_icons;
    public Vector2 hide_logo;
    public List<Vector2> hide_buttons = new List<Vector2>();
    public Vector2 hide_settings;

    [Header("Show Positions")]
    public Vector2 show_leftSection;
    public Vector2 show_icons;
    public Vector2 show_logo;
    public List<Vector2> show_buttons = new List<Vector2>();
    public Vector2 show_settings;


    [Header("Camera Settings")]
    public Vector3 defaultCamPos = new Vector3(0, 0, -10f);

    private bool isinTransition = false;
    private bool AlreadyPlay = false;

    [Header("Settings")]
    public AudioMixer mixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Awake()
    {
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }
    void SetMasterVolume(float value)
    {
        mixer.SetFloat("Master", Mathf.Log10(value) * 20);
    }

    void SetMusicVolume(float value)
    {
        mixer.SetFloat("Music", Mathf.Log10(value) * 20);
    }
    void SetSFXVolume(float value)
    {
        mixer.SetFloat("SFX", Mathf.Log10(value) * 20);
    }

    private bool isSettingsOpen = false;
    private bool isSettingsInTransition = false;


    void Start()
    {
        LeftSection = transform.GetChild(1).GetComponent<RectTransform>();
        Button = LeftSection.transform.GetChild(1).GetComponent<RectTransform>();
        Icons = transform.GetChild(2).GetComponent<RectTransform>();
        Logo = transform.GetChild(3).GetComponent<RectTransform>();
        Settings = transform.GetChild(4).GetComponent<RectTransform>();
        transitionObj = GameObject.Find("Transition");
        foreach (Transform child in Button.transform)
        {
            Buttons.Add(child.GetComponent<RectTransform>());
        }

        // Set show positions
        if (show_leftSection == Vector2.zero) show_leftSection = LeftSection.anchoredPosition;
        if (show_icons == Vector2.zero) show_icons = Icons.anchoredPosition;
        if (show_logo == Vector2.zero) show_logo = Logo.anchoredPosition;
        if (show_buttons.Count == 0)
        {
            foreach (RectTransform btn in Buttons)
            {
                show_buttons.Add(btn.anchoredPosition);
            }
        }
        if (show_settings == Vector2.zero) show_settings = Settings.anchoredPosition;


        // Set Hide pos
        hide_logo = show_logo + new Vector2(0, 300f);
        hide_leftSection = show_leftSection + new Vector2(-1100f, 0);
        hide_icons = show_icons + new Vector2(0, -150f);
        hide_buttons.Clear();
        for (int i = 0; i < show_buttons.Count; i++)
        {
            hide_buttons.Add(show_buttons[i] + new Vector2(-450f, 0));
        }
        hide_settings = show_settings + new Vector2(700f, 0);

        // Place Hide
        for (int i = 0; i < Buttons.Count && i < hide_buttons.Count; i++)
        {
            Buttons[i].anchoredPosition = hide_buttons[i];
        }
        LeftSection.anchoredPosition = hide_leftSection;
        Logo.anchoredPosition = hide_logo;
        Icons.anchoredPosition = hide_icons;
        Settings.anchoredPosition = hide_settings;
        StartCoroutine(InitialTransition());
    }

    void Update()
    {
        if (!GameManager.Instance.isStarted)
        {
            if (Input.GetKeyDown(KeyCode.Escape) && isSettingsOpen)
            { 
                ToggleSettings();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && GameManager.Instance.isStarted)
        {
            if (isSettingsOpen)
            {
                ToggleSettings();
            }
            else
            {
                ToggleMenu();
            }
        }
    }

    IEnumerator InitialTransition()
    {
        yield return new WaitForSeconds(2f);

        if (transitionObj != null)
        {
            SpriteRenderer img = transitionObj.GetComponent<SpriteRenderer>();
            if (img != null)
            {
                img.DOFade(0f, 1f);
                ToggleMenu();
            }
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Play()
    {
        if (!AlreadyPlay)
        {
            AlreadyPlay = true;
            GameManager.Instance.isStarted = true;
            Camera.main.transform.DOMove(defaultCamPos, 1f)
                .SetEase(Ease.InOutSine)
                .OnComplete(() => Debug.Log("Camera reached default position"));
        }

        ToggleMenu();
    }

    //public void ToggleSettings()
    //{
    //    //De la meme facon que le menu
    //    //Déplacer le rect transform de settings Smooth vers la position de settings
    //    //var animation pour evité de cliquer trop vite sur setting, et que ça bug.
    //    //Si settings est ouvert, quand on fait echap ça le ferme (et pas le menu, le Settings!)
    //    //Comme c'est un toggle, on fait la meme chose a l'envers si il est ouvert et que je relance cette fonction
    //}

    public void ToggleSettings()
    {
        // Empêche les nouveaux clics tant que la transition n'est pas terminée
        if (isSettingsInTransition)
            return;

        isSettingsInTransition = true;

        if (!isSettingsOpen)
        {
            // Ouvrir les paramètres : on déplace Settings vers sa position "show"
            Settings.DOAnchorPos(show_settings, 0.3f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    isSettingsOpen = true;
                    isSettingsInTransition = false;
                });
            Debug.Log("[MainMenu] Opening settings");
        }
        else
        {
            // Fermer les paramètres : on déplace Settings vers sa position "hide"
            Settings.DOAnchorPos(hide_settings, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    isSettingsOpen = false;
                    isSettingsInTransition = false;
                });
            Debug.Log("[MainMenu] Closing settings");
        }
    }


    public void Journal(TextMeshProUGUI text)
    {
        if (DOTween.IsTweening(text))
            return;

        Color originalColor = text.color;

        Color hexColor;
        if (!UnityEngine.ColorUtility.TryParseHtmlString("#CF0000", out hexColor))
        {
            Debug.LogError("La couleur hexadécimale n'a pas pu être convertie !");
            return;
        }

        DG.Tweening.Sequence journalSequence = DOTween.Sequence();
        journalSequence.Append(text.DOColor(hexColor, 0.1f)); 
        journalSequence.Append(text.DOColor(originalColor, 0.3f));
        journalSequence.Play();
    }

    public void ToggleMenu()
    {
        if (isinTransition) return;
        if (!GameManager.Instance.menuShown)
        {
            // Show
            isinTransition = true;
            DG.Tweening.Sequence seq = DOTween.Sequence();
            seq.Append(LeftSection.DOAnchorPos(show_leftSection, 0.1f).SetEase(Ease.InOutBack));
            seq.AppendInterval(0.2f);
            seq.Append(Logo.DOAnchorPos(show_logo, 0.1f).SetEase(Ease.InOutBack));
            seq.AppendInterval(0.2f);
            seq.Append(Icons.DOAnchorPos(show_icons, 0.1f).SetEase(Ease.InOutBack));
            seq.AppendInterval(0.2f);

            for (int i = 0; i < Buttons.Count && i < show_buttons.Count; i++)
            {
                seq.AppendInterval(0.2f);
                seq.Join(Buttons[i].DOAnchorPos(show_buttons[i], 0.1f).SetEase(Ease.InOutBack));
            }
            seq.OnComplete(() => { GameManager.Instance.menuShown = true; isinTransition = false; });
        }
        else
        {
            // Hide
            isinTransition = true;
            DG.Tweening.Sequence seq = DOTween.Sequence();
            for (int i = 0; i < Buttons.Count && i < hide_buttons.Count; i++)
            {
                seq.Join(Buttons[i].DOAnchorPos(hide_buttons[i], 0.1f).SetEase(Ease.InOutBack));
                seq.AppendInterval(0.2f);
            }
            seq.Append(Icons.DOAnchorPos(hide_icons, 0.1f).SetEase(Ease.InOutBack));
            seq.AppendInterval(0.2f);
            seq.Append(Logo.DOAnchorPos(hide_logo, 0.1f).SetEase(Ease.InOutBack));
            seq.AppendInterval(0.2f);
            seq.Append(LeftSection.DOAnchorPos(hide_leftSection, 0.1f).SetEase(Ease.InOutBack));
            seq.OnComplete(() => { GameManager.Instance.menuShown = false; isinTransition = false; });
        }
    }
}
