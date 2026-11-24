using System.Collections;
using UnityEngine;

// Cup with assisted scoring: if the ball touches the mouth of the cup or gets inside,
// it is pulled to the bottom instead of bouncing out.
// The trigger zone is set up in Awake, so the Box Collider values in the prefab are ignored.
public class Cup : MonoBehaviour
{
    [Header("Capture zone (cup local space, meters)")]
    public float captureRadius = 0.05f; // from the cup axis, the rim radius is ~0.046
    public float rimY = 0.06f;          // top of the cup (mesh goes from -0.06 to 0.06)
    public float bottomY = -0.05f;      // inside bottom of the cup
    public float ballRadius = 0.02f;    // ball scale 0.04 / 2

    [Header("Capture animation")]
    public float captureDuration = 0.2f;
    public float ballRestY = -0.035f;   // where the ball ends up (ball center)

    bool scored;

    void Awake()
    {
        // trigger that covers the inside of the cup and the mouth
        BoxCollider zone = GetComponent<BoxCollider>();
        if (zone == null)
            zone = gameObject.AddComponent<BoxCollider>();

        float top = rimY + ballRadius;
        float width = (captureRadius + ballRadius) * 2f;

        zone.isTrigger = true;
        zone.size = new Vector3(width, top - bottomY, width);
        zone.center = new Vector3(0f, (top + bottomY) / 2f, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        TryCapture(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryCapture(other);
    }

    // backup for fast balls that bounce off the cup between physics steps
    void OnCollisionEnter(Collision collision)
    {
        TryCapture(collision.collider);
    }

    void TryCapture(Collider other)
    {
        if (scored)
            return;

        Ball ball = other.GetComponent<Ball>();

        // dont count the ball thats still in hand
        if (ball == null || !ball.isThrown || ball.captured)
            return;

        if (!IsInsideMouth(ball.transform.position))
            return;

        scored = true;
        ball.captured = true;
        StartCoroutine(PullIn(ball));
    }

    // ball center over the opening (or inside the cup) and low enough to touch the rim
    bool IsInsideMouth(Vector3 worldPos)
    {
        Vector3 p = transform.InverseTransformPoint(worldPos);
        float horizontal = new Vector2(p.x, p.z).magnitude;

        return horizontal < captureRadius
            && p.y < rimY + ballRadius
            && p.y > bottomY;
    }

    IEnumerator PullIn(Ball ball)
    {
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        ball.GetComponent<Collider>().enabled = false;

        Vector3 start = ball.transform.position;
        Vector3 end = transform.TransformPoint(0f, ballRestY, 0f);

        for (float t = 0f; t < 1f; t += Time.deltaTime / captureDuration)
        {
            if (ball == null)
                yield break;

            ball.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        OnCupHit();

        if (ball != null)
            Destroy(ball.gameObject);
        Destroy(gameObject);
    }

    void OnCupHit()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayScore();
        }
    }
}