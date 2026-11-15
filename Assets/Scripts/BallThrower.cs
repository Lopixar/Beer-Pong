using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallThrower : MonoBehaviour
{
    public Camera arCamera;
    public GameObject ballPrefab;
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI throwsText;

    [Header("Game")]
    public int maxThrows = 20;
    public float throwZoneRadius = 0.8f; // in meters

    [Header("Ball in hand")]
    public Vector3 holdOffset = new Vector3(0f, -0.1f, 0.3f);

    [Header("Throw")]
    public float speedMultiplier = 1.5f;
    public float minSpeed = 1.5f;
    public float maxSpeed = 6f;
    public float upAmount = 0.6f; // throw angle
    public float sideAmount = 1f;
    public float minSwipe = 0.1f; // % of screen height

    GameObject table;
    Transform throwZone;
    GameObject heldBall;
    GameObject thrownBall;
    int throwsLeft;
    bool gameOver;

    bool swiping;
    Vector2 swipeStart;
    Vector2 swipeEnd;
    float swipeStartTime;

    void Start()
    {
        throwsText.text = "";
    }

    // called from TablePlacer
    public void StartGame(GameObject newTable)
    {
        table = newTable;
        throwZone = table.transform.Find("ThrowZone");
        throwsLeft = maxThrows;
        gameOver = false;
    }

    public void StopGame()
    {
        Destroy(heldBall);
        Destroy(thrownBall);
        table = null;
        throwsText.text = "";
    }

    void Update()
    {
        if (table == null)
            return;

        throwsText.text = GameTexts.ThrowsLeft + throwsLeft;

        // no ball anymore, spawn a new one
        if (heldBall == null && thrownBall == null)
        {
            if (throwsLeft > 0)
            {
                SpawnBall();
            }
            else if (!gameOver)
            {
                gameOver = true;
                OnNoThrowsLeft();
            }
        }

        if (heldBall == null)
            return;

        if (!IsInThrowZone())
        {
            heldBall.SetActive(false);
            infoText.text = GameTexts.GoToTableEdge;
            swiping = false;
            return;
        }

        heldBall.SetActive(true);
        infoText.text = GameTexts.SwipeToThrow;
        CheckSwipe();
    }

    void SpawnBall()
    {
        heldBall = Instantiate(ballPrefab, arCamera.transform);
        heldBall.transform.localPosition = holdOffset;
        heldBall.GetComponent<Rigidbody>().isKinematic = true;
    }

    bool IsInThrowZone()
    {
        // if theres no ThrowZone just let him throw
        if (throwZone == null)
            return true;

        Vector3 cameraPos = arCamera.transform.position;
        Vector3 zonePos = throwZone.position;
        cameraPos.y = 0;
        zonePos.y = 0;

        return Vector3.Distance(cameraPos, zonePos) < throwZoneRadius;
    }

    void CheckSwipe()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null)
            return;

        if (pointer.press.wasPressedThisFrame)
        {
            swiping = true;
            swipeStart = pointer.position.ReadValue();
            swipeStartTime = Time.time;
        }

        if (pointer.press.isPressed)
        {
            swipeEnd = pointer.position.ReadValue();
        }

        if (pointer.press.wasReleasedThisFrame && swiping)
        {
            swiping = false;
            ThrowBall(swipeEnd - swipeStart, Time.time - swipeStartTime);
        }
    }

    void ThrowBall(Vector2 swipe, float swipeTime)
    {
        float swipeUp = swipe.y / Screen.height;
        float swipeSide = swipe.x / Screen.width;

        // just a tap
        if (swipeUp < minSwipe)
            return;

        // quicker swipe = harder throw
        float speed = swipeUp / Mathf.Max(swipeTime, 0.05f) * speedMultiplier;
        speed = Mathf.Clamp(speed, minSpeed, maxSpeed);

        Vector3 forward = arCamera.transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = arCamera.transform.right;
        right.y = 0;
        right.Normalize();

        Vector3 direction = forward + Vector3.up * upAmount + right * swipeSide * sideAmount;

        heldBall.transform.SetParent(null);
        Rigidbody rb = heldBall.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.linearVelocity = direction.normalized * speed;

        Ball ball = heldBall.GetComponent<Ball>();
        ball.isThrown = true;
        ball.fallLimitY = table.transform.position.y; // floor

        thrownBall = heldBall;
        heldBall = null;
        throwsLeft--;
    }

    void OnNoThrowsLeft()
    {
        infoText.text = GameTexts.NoThrowsLeft;
        // TODO end screen
    }
}
