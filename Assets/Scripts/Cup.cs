using UnityEngine;

// trigger collider goes at the bottom of the cup
public class Cup : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Ball ball = other.GetComponent<Ball>();

        // dont count the ball thats still in hand
        if (ball == null || !ball.isThrown)
            return;

        OnCupHit();

        Destroy(other.gameObject);
        Destroy(gameObject);
    }

    void OnCupHit()
    {
        // TODO add point
    }
}
