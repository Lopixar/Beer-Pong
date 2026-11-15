using UnityEngine;

public class Ball : MonoBehaviour
{
    public float lifeTime = 5f;

    [HideInInspector] public bool isThrown;
    [HideInInspector] public float fallLimitY;

    float timer;

    void Update()
    {
        if (!isThrown)
            return;

        timer += Time.deltaTime;

        // fell off the table or flying too long
        if (transform.position.y < fallLimitY || timer > lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
