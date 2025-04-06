using UnityEngine;
using System.Collections;

public class urlButton : MonoBehaviour
{
    public string url = "";
    public void OpenURL()
    {
        Debug.Log(url);
        Application.OpenURL(url);
    }

}