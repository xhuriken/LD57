using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterX : MonoBehaviour
{
    public float timeToDestroy = 2f;
    void Start()
    {
        Destroy(gameObject, timeToDestroy);
    }
}
