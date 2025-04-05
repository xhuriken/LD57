using UnityEngine;
using System.Collections;

public class urlButton : MonoBehaviour
{
    public string url = "http://fischhaus.com/";
    public void OpenURL()
    {
        Application.OpenURL(url);
    }

}