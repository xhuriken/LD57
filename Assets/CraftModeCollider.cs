using UnityEngine;

public class CraftModeCollider : MonoBehaviour
{
    public float radius = 1f;
    public bool isInTransition;
    public void CheckForObstacles()
    {
        int layerMask = 1 << LayerMask.NameToLayer("Objects");
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius, layerMask);

        if (colliders.Length > 0)
        {
            CancelCraftMode();
        }
    }

    private void CancelCraftMode()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Cancel");
        }
        GameManager.Instance.CancelCraftMode();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void Update()
    {
        if (isInTransition && !GameManager.Instance.isInTransition)
        {
            GameManager.Instance.isInTransition = true;
        }
        else if (!isInTransition && GameManager.Instance.isInTransition)
        {
            GameManager.Instance.isInTransition = false;
        }
    }
}
