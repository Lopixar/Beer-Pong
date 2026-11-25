using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Controller of the end-of-game overlay (victory and defeat share the same panel).
// The visuals (panel, texts, buttons, confetti) are built at runtime by MobileUIStyler,
// so it matches the menus, this script only fills in the texts / numbers and shows / hides it.
// BallThrower decides when the player wins or loses (see "Cups To Win" in its inspector).
public class VictoryScreen : MonoBehaviour
{
    public static VictoryScreen Instance { get; private set; }

    [HideInInspector] public GameObject panel;
    [HideInInspector] public GameObject confetti;
    [HideInInspector] public Image glow;
    [HideInInspector] public TMP_Text title;
    [HideInInspector] public TMP_Text subtitle;
    [HideInInspector] public TMP_Text cupsValue;
    [HideInInspector] public TMP_Text throwsValue;
    [HideInInspector] public TMP_Text accuracyValue;
    [HideInInspector] public TMP_Text playAgainLabel;

    const string AmberHex = "FFC23C";
    const string RedHex = "FF3D57";

    public bool IsVisible
    {
        get { return panel != null && panel.activeSelf; }
    }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(int cupsHit, int cupsTotal, int throwsUsed)
    {
        bool allCups = cupsTotal > 0 && cupsHit >= cupsTotal;
        Open(true, allCups ? GameTexts.VictoryAllCups : GameTexts.VictoryTarget, cupsHit, cupsTotal, throwsUsed);
    }

    public void ShowDefeat(int cupsHit, int cupsTotal, int throwsUsed)
    {
        Open(false, GameTexts.DefeatSubtitle, cupsHit, cupsTotal, throwsUsed);
    }

    void Open(bool win, string subtitleText, int cupsHit, int cupsTotal, int throwsUsed)
    {
        if (panel == null)
            return;

        string accent = win ? AmberHex : RedHex;
        string line1 = win ? GameTexts.VictoryLine1 : GameTexts.DefeatLine1;
        string line2 = win ? GameTexts.VictoryLine2 : GameTexts.DefeatLine2;

        if (title != null)
            title.text = line1 + "\n<color=#" + accent + ">" + line2 + "</color>";

        if (subtitle != null)
            subtitle.text = subtitleText;

        if (playAgainLabel != null)
            playAgainLabel.text = win ? GameTexts.PlayAgain : GameTexts.TryAgain;

        if (confetti != null)
            confetti.SetActive(win);

        if (glow != null)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + accent, out c);
            c.a = glow.color.a;
            glow.color = c;
        }

        if (cupsValue != null)
            cupsValue.text = cupsTotal > 0 ? cupsHit + "/" + cupsTotal : cupsHit.ToString();

        if (throwsValue != null)
            throwsValue.text = throwsUsed.ToString();

        if (accuracyValue != null)
        {
            accuracyValue.text = throwsUsed > 0
                ? Mathf.Min(100, Mathf.RoundToInt(100f * cupsHit / throwsUsed)) + "%"
                : "-";
        }

        // fills in the texts first, so the intro animation already shows the final text
        panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
