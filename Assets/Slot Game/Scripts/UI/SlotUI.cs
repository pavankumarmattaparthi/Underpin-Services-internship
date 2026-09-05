using TMPro;
using UnityEngine;
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

        // Update UI when the game starts
        UpdateUI();
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