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

    [Header("Victory")]
    [Tooltip("Cups you have to knock down to win. 0 = all of them. Set it to 1 or 2 to test the victory screen quickly.")]
    public int cupsToWin = 0;
    [Tooltip("Editor only: press V to force the victory screen, L to force the defeat screen.")]
    public bool debugVictoryKey = true;

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

    bool victory;
    int cupsTotal;
    int cupsHit;

    bool swiping;
    Vector2 swipeStart;
    Vector2 swipeEnd;
    float swipeStartTime;

    // how many cups have to go down for the victory screen
    public int CupsNeeded
    {
        get
        {
            int needed = cupsToWin > 0 ? Mathf.Min(cupsToWin, cupsTotal) : cupsTotal;
            return Mathf.Max(1, needed);
        }
    }

    void OnEnable()
    {
        Cup.Scored += OnCupScored;
    }

    void OnDisable()
    {
        Cup.Scored -= OnCupScored;
    }

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

        victory = false;
        cupsHit = 0;
        cupsTotal = table.GetComponentsInChildren<Cup>(true).Length;

        if (VictoryScreen.Instance != null)
            VictoryScreen.Instance.Hide();
    }

    public void StopGame()
    {
        Destroy(heldBall);
        Destroy(thrownBall);
        table = null;
        victory = false;
        throwsText.text = "";

        if (VictoryScreen.Instance != null)
            VictoryScreen.Instance.Hide();
    }

    void Update()
    {
#if UNITY_EDITOR
        // quick way to check the victory screen without playing
        if (debugVictoryKey && !victory
            && Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            Win();
        }

        if (debugVictoryKey && !victory
            && Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            OnNoThrowsLeft();
        }
#endif

        // table gone or already won: nothing to do (no more balls)
        if (table == null || victory)
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

        // Play throw sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayThrow();
        }

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

    // out of throws and the last ball is gone: cups are still standing, so the player loses
    void OnNoThrowsLeft()
    {
        infoText.text = "";

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayLose();

        if (VictoryScreen.Instance != null)
            VictoryScreen.Instance.ShowDefeat(cupsHit, cupsTotal, maxThrows);
        else
            Debug.LogWarning("BallThrower: out of throws but there is no end screen (is MobileUIStyler running?).");
    }

    void OnCupScored(Cup cup)
    {
        // ignore cups that are not part of the current table
        if (table == null || victory || !cup.transform.IsChildOf(table.transform))
            return;

        cupsHit++;

        if (cupsHit >= CupsNeeded)
            Win();
    }

    void Win()
    {
        // without a table (debug) there is no game to stop, so the flag stays free for more tests
        victory = table != null;
        swiping = false;

        Destroy(heldBall);
        Destroy(thrownBall);
        heldBall = null;
        thrownBall = null;

        throwsText.text = "";
        infoText.text = "";

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayWin();

        if (VictoryScreen.Instance != null)
            VictoryScreen.Instance.Show(cupsHit, cupsTotal, table != null ? maxThrows - throwsLeft : 0);
        else
            Debug.LogWarning("BallThrower: victory reached but there is no VictoryScreen (is MobileUIStyler running?).");
    }

    [ContextMenu("Debug/Force defeat")]
    void DebugForceDefeat()
    {
        if (Application.isPlaying && !victory)
            OnNoThrowsLeft();
    }

    [ContextMenu("Debug/Force victory")]
    void DebugForceVictory()
    {
        if (Application.isPlaying && !victory)
            Win();
    }
}
