using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour
{
    [Header("Gold & Bet Text")]
    [SerializeField] private TMP_Text totalGoldText;
    [SerializeField] private TMP_Text betText;

    [Header("Bet Buttons")]
    [SerializeField] private Button tenGoldBetBtn;
    [SerializeField] private Button fiftyGoldBetBtn;
    [SerializeField] private Button hundredGoldBetBtn;

    [Header("Other Buttons")]
    [SerializeField] private Button exitBtn;

    private void Start()
    {
        // Register button events
        tenGoldBetBtn.onClick.AddListener(() => BettingBtnPressed(10));
        fiftyGoldBetBtn.onClick.AddListener(() => BettingBtnPressed(50));
        hundredGoldBetBtn.onClick.AddListener(() => BettingBtnPressed(100));

        exitBtn.onClick.AddListener(ExitBtnPressed);

        ApplyNeonTheme();

        // Update UI when the game starts
        UpdateUI();
    }

    // =========================================================
    // NEON THEME
    // =========================================================

    private void ApplyNeonTheme()
    {
        StyleBetButton(tenGoldBetBtn, NeonTheme.NeonPink);
        StyleBetButton(fiftyGoldBetBtn, NeonTheme.NeonPurple);
        StyleBetButton(hundredGoldBetBtn, NeonTheme.NeonGold);
        StyleBetButton(exitBtn, NeonTheme.NeonCyan);

        StyleGlowText(totalGoldText, NeonTheme.NeonGold);
        StyleGlowText(betText, NeonTheme.NeonCyan);
    }

    private void StyleBetButton(Button button, Color accent)
    {
        if (button == null)
            return;

        Image image = button.image;

        if (image != null)
        {
            image.sprite = NeonTheme.CreateRoundedPanel(
                220, 60, 16,
                NeonTheme.PanelFill, NeonTheme.BackgroundBottom,
                accent, 3,
                10, accent
            );
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        AddButtonBounce(button);
    }

    private void StyleGlowText(TMP_Text text, Color accent)
    {
        if (text == null)
            return;

        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        text.outlineWidth = 0.2f;
        text.outlineColor = accent;
    }

    private void AddButtonBounce(Button button)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        RectTransform rect = button.GetComponent<RectTransform>();
        Vector3 baseScale = rect.localScale;

        AddTriggerEntry(trigger, EventTriggerType.PointerEnter, () => StartCoroutine(ScaleTo(rect, baseScale * 1.08f, 0.12f)));
        AddTriggerEntry(trigger, EventTriggerType.PointerExit, () => StartCoroutine(ScaleTo(rect, baseScale, 0.12f)));
        AddTriggerEntry(trigger, EventTriggerType.PointerDown, () => StartCoroutine(ScaleTo(rect, baseScale * 0.94f, 0.06f)));
        AddTriggerEntry(trigger, EventTriggerType.PointerUp, () => StartCoroutine(ScaleTo(rect, baseScale * 1.08f, 0.08f)));
    }

    private void AddTriggerEntry(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((_) => action());
        trigger.triggers.Add(entry);
    }

    private IEnumerator ScaleTo(RectTransform rect, Vector3 targetScale, float duration)
    {
        Vector3 start = rect.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rect.localScale = Vector3.Lerp(start, targetScale, elapsed / duration);
            yield return null;
        }

        rect.localScale = targetScale;
    }

    // =========================================================
    // WIN CELEBRATION
    // =========================================================

    public void PlayWinCelebration(int prizeAmount)
    {
        StartCoroutine(WinCelebrationRoutine(prizeAmount));
    }

    private IEnumerator WinCelebrationRoutine(int prizeAmount)
    {
        SpawnBurst();

        int endGold = SlotGameManager.Instance.TotalGold;
        int startGold = endGold - prizeAmount;

        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int shown = Mathf.RoundToInt(Mathf.Lerp(startGold, endGold, elapsed / duration));
            totalGoldText.text = shown.ToString();
            yield return null;
        }

        totalGoldText.text = endGold.ToString();
    }

    private void SpawnBurst()
    {
        const int count = 12;
        Color[] palette =
        {
            NeonTheme.NeonPink, NeonTheme.NeonGold, NeonTheme.NeonCyan, NeonTheme.NeonPurple
        };

        for (int i = 0; i < count; i++)
        {
            GameObject spark = new GameObject("WinSpark");
            spark.transform.SetParent(transform, false);

            Image image = spark.AddComponent<Image>();
            Color color = palette[i % palette.Length];
            image.sprite = NeonTheme.CreateGlowDot(64, color);
            image.raycastTarget = false;

            RectTransform rect = spark.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(40, 40);
            rect.anchoredPosition = Vector2.zero;

            float angle = i * (360f / count) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            StartCoroutine(AnimateSpark(rect, image, direction));
        }
    }

    private IEnumerator AnimateSpark(RectTransform rect, Image image, Vector2 direction)
    {
        float duration = 0.7f;
        float elapsed = 0f;
        float distance = 160f;

        Color startColor = image.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float eased = NeonTheme.EaseOutCubic(t);

            rect.anchoredPosition = direction * distance * eased;
            rect.localScale = Vector3.one * (1f - t * 0.6f);

            Color c = startColor;
            c.a = startColor.a * (1f - t);
            image.color = c;

            yield return null;
        }

        Destroy(rect.gameObject);
    }

    /// <summary>
    /// Called when the player selects a betting amount.
    /// </summary>
    private void BettingBtnPressed(int gold)
    {
        if (SlotGameManager.Instance == null)
        {
            return;
        }

        // Try to change the current bet. SetBet() now starts the spin
        // itself once the bet is validated, so the reels can never be
        // left un-spun with gold already deducted.
        bool betChanged = SlotGameManager.Instance.SetBet(gold);

        if (betChanged)
        {
            UpdateUI();

            ButtonState();
        }

    }

    /// <summary>
    /// Updates all UI information.
    /// </summary>
    public void UpdateUI()
    {
        if (SlotGameManager.Instance == null)
        {
            return;
        }

        totalGoldText.text = SlotGameManager.Instance.TotalGold.ToString();

        betText.text = SlotGameManager.Instance.CurrentBet.ToString();
    }

    /// <summary>
    /// Called when the Exit button is pressed.
    /// </summary>
    private void ExitBtnPressed()
    {
        Debug.Log("Exit button pressed.");

        Application.Quit();
    }

    public void ButtonState(bool State)
    {
        tenGoldBetBtn.gameObject.SetActive(State);
        fiftyGoldBetBtn.gameObject.SetActive(State);
        hundredGoldBetBtn.gameObject.SetActive(State);
    }

    private void OnDestroy()
    {
        // Remove button listeners
        if (tenGoldBetBtn != null)
            tenGoldBetBtn.onClick.RemoveAllListeners();

        if (fiftyGoldBetBtn != null)
            fiftyGoldBetBtn.onClick.RemoveAllListeners();

        if (hundredGoldBetBtn != null)
            hundredGoldBetBtn.onClick.RemoveAllListeners();

        if (exitBtn != null)
            exitBtn.onClick.RemoveAllListeners();
    }

    public void ButtonState()
    {
        if(SlotGameManager.Instance.TotalGold >= 100)
        {
            hundredGoldBetBtn.interactable = true;
        }
        else
        {
            hundredGoldBetBtn.interactable = false;
        }

        if(SlotGameManager.Instance.TotalGold >= 50)
        {
            fiftyGoldBetBtn.interactable = true;
        }
        else
        {
            fiftyGoldBetBtn.interactable = false;
        }

        if(SlotGameManager.Instance.TotalGold >= 10)
        {
            tenGoldBetBtn.interactable = true;
        }
        else
        {
            tenGoldBetBtn.interactable = false;
        }
    }
}