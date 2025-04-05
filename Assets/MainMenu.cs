using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    private GameObject LeftSection;
    private GameObject Button;
    private List<GameObject[]> Buttons;
    private GameObject Icons;
    private GameObject Logo;
    private Transform defaultCamPos;
    private GameObject transition;

    //coordinate
    public Transform hide_leftSection;
    public Transform hide_button;
    public Transform hide_icons;
    public Transform hide_logo;
    private List<Transform[]> hide_buttons;

    public Transform show_leftSection;
    public Transform show_button;
    public Transform show_icons;
    public Transform show_logo;
    private List<Transform[]> show_buttons;

    // Start is called before the first frame update
    void Start()
    {
        LeftSection = transform.GetChild(0).gameObject;

        Button = LeftSection.transform.GetChild(0).gameObject;

        //Tout les enfant de buttons doivent être dans Buttons.

        Icons = transform.GetChild(1).gameObject;

        Logo = transform.GetChild(2).gameObject;
        //Set show_ var to actual transform of objects (show_logo = Logo.transform)
        //Wait 2 seconds
        //transition object -> get color of image, and set transparency to 0 smoothly. (j'ai dotween, animation de 1 seconde)
        //Set all object to all transform assignate (Logo.transform -> hide_logo transform)

        //Toggle menu
    }

    // Update is called once per frame
    void Update()
    {
    }

    void PlayGame()
    {
        //Toggle menu

    }

    //Need function toggle, ToggleMenu. Will show unshow menu.
    //For show menu:
    // first move LeftSection to show_leftSection 0.1f
    //wait 0.2f
    // then move logo to show_logo 0.1f
    //wait 0.2f
    // then move icons to show_icons 0.1f
    //wait 0.2f
    // then move buttons to show_buttons (wait 0.2f before all buttons transform)
    //Put var to true for canShowMenu (use bool for toggle you know ?)
}
