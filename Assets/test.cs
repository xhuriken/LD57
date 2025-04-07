using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        float radius = 1.5f;
        Vector3 pos0 = new Vector3(radius, 0, 0);
        Vector3 pos1 = new Vector3(radius * Mathf.Cos(45 * Mathf.Deg2Rad), radius * Mathf.Sin(45 * Mathf.Deg2Rad), 0);
        Vector3 pos2 = new Vector3(0, radius, 0);
        Vector3 pos3 = new Vector3(radius * Mathf.Cos(135 * Mathf.Deg2Rad), radius * Mathf.Sin(135 * Mathf.Deg2Rad), 0);
        Vector3 pos4 = new Vector3(-radius, 0, 0);
        Vector3 pos5 = new Vector3(radius * Mathf.Cos(225 * Mathf.Deg2Rad), radius * Mathf.Sin(225 * Mathf.Deg2Rad), 0);
        Vector3 pos6 = new Vector3(0, -radius, 0);
        Vector3 pos7 = new Vector3(radius * Mathf.Cos(315 * Mathf.Deg2Rad), radius * Mathf.Sin(315 * Mathf.Deg2Rad), 0);

        // Affichage dans la console pour vérification
        Debug.Log("Pos0: " + pos0);
        Debug.Log("Pos1: " + pos1);
        Debug.Log("Pos2: " + pos2);
        Debug.Log("Pos3: " + pos3);
        Debug.Log("Pos4: " + pos4);
        Debug.Log("Pos5: " + pos5);
        Debug.Log("Pos6: " + pos6);
        Debug.Log("Pos7: " + pos7);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
