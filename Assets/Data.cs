using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Data : MonoBehaviour
{
    [Header("Utils")]
    public bool isInhaled = false;
    public bool isFreeze = false;
    public bool isDragged = false;
    private void Start()
    {
        isInhaled = false;
        isFreeze = false;
        isDragged = false;
    }

}
