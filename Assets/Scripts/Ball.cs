using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class Ball : MonoBehaviour
{
    // Ok l'equipe ! 

    private Rigidbody2D m_rb;
    //private CircleCollider2D m_cc;
    Animator m_animator;

    private void Start()
    {
        //m_cc = GetComponent<CircleCollider2D>();
        //m_cc.enabled = false;
        m_rb = GetComponent<Rigidbody2D>();
        //StartCoroutine(EnableCollider());
        m_animator = GetComponent<Animator>();

    }

    private void Update()
    {
        //ECHEC
        //Appliquer un ralentissement a la balle constant pour simulé une frixion avec une courbe lerp
        //m_rb.velocity = Vector2.Lerp(m_rb.velocity, Vector2.zero, Time.deltaTime * 0.1f);
        //Nouvelle essaie
        //Prendre la velocity actuelle, et rajouter par dessus un ralentissement lerp jusqu'a 0
        // Facteur de friction simulée (ex: 0.99 = 1% de perte par frame)
        m_rb.velocity *= 0.99f;

        // Si trop lent, arrête complètement (évite de glisser à l'infini)
        if (m_rb.velocity.magnitude < 0.01f)
            m_rb.velocity = Vector2.zero;
    }

    //private IEnumerator EnableCollider()
    //{
    //    yield return new WaitForSeconds(0.1f);
    //    m_cc.enabled = true;
    //}

}
