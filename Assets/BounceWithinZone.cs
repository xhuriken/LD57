using UnityEngine;

public class BounceWithinZone : MonoBehaviour
{
    public Zone zone;

    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();
        zone = FindObjectOfType<Zone>();
    }

    void FixedUpdate()
    {
        if (zone == null)
            return;

        Vector3 zoneCenter = zone.transform.position;
        float zoneWidth = zone.zone.Width;
        float zoneHeight = zone.zone.Height;
        float leftBound = (zoneCenter.x - zoneWidth / 2f) + 0.1f;
        float rightBound = (zoneCenter.x + zoneWidth / 2f) - 0.1f;
        float bottomBound = (zoneCenter.y - zoneHeight / 2f) + 0.1f;
        float topBound = (zoneCenter.y + zoneHeight / 2f) - 0.1f;

        float radius = 0f;
        if (circleCollider != null)
        {
            radius = circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }

        Vector2 pos = transform.position;
        Vector2 velocity = rb.velocity;
        bool collided = false;

        // Left
        if (pos.x - radius < leftBound)
        {
            pos.x = leftBound + radius;
            velocity = Reflect(velocity, Vector2.right);
            collided = true;
        }
        // Right
        else if (pos.x + radius > rightBound)
        {
            pos.x = rightBound - radius;
            velocity = Reflect(velocity, Vector2.left);
            collided = true;
        }
        // Bottom
        if (pos.y - radius < bottomBound)
        {
            pos.y = bottomBound + radius;
            velocity = Reflect(velocity, Vector2.up);
            collided = true;
        }
        // Top
        else if (pos.y + radius > topBound)
        {
            pos.y = topBound - radius;
            velocity = Reflect(velocity, Vector2.down);
            collided = true;
        }

        if (collided)
        {
            transform.position = pos;
            rb.velocity = velocity;

            BlueBall blueBall = GetComponent<BlueBall>();
            if (blueBall != null && blueBall.currentState != BlueBall.BlueBallState.Friction)
            {
                blueBall.currentState = BlueBall.BlueBallState.Friction;
            }
        }
    }

    Vector2 Reflect(Vector2 v, Vector2 n)
    {
        return v - 2f * Vector2.Dot(v, n) * n;
    }
}
