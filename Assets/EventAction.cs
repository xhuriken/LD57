using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventAction : MonoBehaviour
{
    public GameObject GrayVioletBall;
    public void GrayVoiletBall()
    {
        Instantiate(GrayVioletBall, transform.position, Quaternion.identity);
    }
}
