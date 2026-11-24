
using UnityEngine;

public class Ball : MonoBehaviour
{
    public float lifeTime = 5f;

    [HideInInspector] public bool isThrown;
    [HideInInspector] public bool captured;
    [HideInInspector] public float fallLimitY;

    float timer;

    [Header("Bounce Sound")]
    public float bounceCooldown = 0.12f;

    float lastBounceTime = -1f;

    void Update()
    {
        if (!isThrown || captured)
            return;

        timer += Time.deltaTime;

        if (transform.position.y < fallLimitY || timer > lifeTime)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isThrown || captured)
            return;

        if (Time.time - lastBounceTime < bounceCooldown)
            return;

        lastBounceTime = Time.time;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBounce();
        }
    }
}
