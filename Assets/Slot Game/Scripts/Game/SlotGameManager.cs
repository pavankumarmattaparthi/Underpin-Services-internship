using System.Collections;
using UnityEngine;

/// <summary>
/// Central controller for the slot game.
/// Owns the player's gold/bet state, drives the reels, plays the lever
/// animation, and calculates prizes once all reels have stopped.
/// Implemented as a simple singleton so other scripts (SlotReel, SlotUI)
/// can access it via <see cref="Instance"/>.
/// </summary>
public class SlotGameManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    #region Singleton

    public static SlotGameManager Instance { get; private set; }

    #endregion


    // =========================================================
    // INSPECTOR FIELDS
    // (Names below are referenced by the Scene / Inspector - do not rename)
    // =========================================================

    #region References

    [Header("SlotReel Script")]
    public SlotReel slotReel1;
    public SlotReel slotReel2;
    public SlotReel slotReel3;

    [Header("SlotUI")]
    [SerializeField] private SlotUI slotUI;

    #endregion

    #region Economy Settings

    [Header("Gold and Bet Values")]
    [SerializeField] private int totalGold = 1000;
    [SerializeField] private int currentBet = 10;

    #endregion

    #region Reel Layout Settings

    [Header("Reel Settings")]
    public float childSpacing = 1.5f;

    [Header("Position Settings")]
    public float targetY = 1.5f;
    public float bottomLimit = -4.5f;

    #endregion

    #region Spin Settings

    [Header("Spin Settings")]
    public float spinDuration = 3f;
    public float slowDownDuration = 1.5f;

    [Header("Spin Speed")]
    public float SpinReelMinimumSpeed = 4;
    public float SpinReelMaximumSpeed = 8;

    #endregion

    #region Lever GameObjects

    [Header("GameObject")]
    [SerializeField] GameObject UppullingLever;
    [SerializeField] GameObject DownpullingLever;

    #endregion


    // =========================================================
    // REEL SYMBOLS
    // Enum member names match the reel child GameObject names in the
    // Scene (matched via Enum.TryParse in SlotReel) - do not rename.
    // =========================================================

    #region Symbol Enum

    public enum SlorRell
    {
        Nun,
        Seven,
        Chery,
        Bell,
        Bar
    }

    #endregion


    // =========================================================
    // PUBLIC PROPERTIES
    // =========================================================

    #region Public Properties

    public int TotalGold => totalGold;
    public int CurrentBet => currentBet;

    #endregion


    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    #endregion


    // =========================================================
    // BETTING
    // =========================================================

    #region Betting

    /// <summary>
    /// Attempts to place a bet, deduct the gold, reset the reels and
    /// kick off the spin/lever sequence. Returns false if the bet is
    /// invalid or the player doesn't have enough gold.
    /// </summary>
    public bool SetBet(int betAmount)
    {
        if (betAmount <= 0)
        {
            Debug.LogWarning("Bet must be greater than 0.");
            return false;
        }

        if (betAmount > totalGold)
        {
            Debug.LogWarning("Not enough gold!");
            return false;
        }

        NunAllScrolReel();

        totalGold -= betAmount;

        currentBet = betAmount;

        slotUI.ButtonState(false);

        StartCoroutine(LeverAnimation());

        return true;
    }

    #endregion


    // =========================================================
    // LEVER ANIMATION
    // =========================================================

    #region Lever Animation

    /// <summary>
    /// Plays the "pulled" lever pose briefly before returning to idle.
    /// </summary>
    private IEnumerator LeverAnimation()
    {
        LeverAnimation(true);

        yield return new WaitForSeconds(0.3f);

        LeverAnimation(false);
    }

    /// <summary>
    /// Swaps the lever GameObjects to show either the pulled-down or
    /// resting-up pose.
    /// </summary>
    private void LeverAnimation(bool State)
    {
        if (State)
        {
            UppullingLever.SetActive(false);
            DownpullingLever.SetActive(true);
        }
        else
        {
            UppullingLever.SetActive(true);
            DownpullingLever.SetActive(false);
        }
    }

    #endregion


    // =========================================================
    // GOLD ECONOMY
    // =========================================================

    #region Gold Economy

    /// <summary>
    /// Adds gold to the player's total (e.g. a prize payout).
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        totalGold += amount;
    }

    /// <summary>
    /// Attempts to deduct gold from the player's total.
    /// Returns false if the amount is invalid or exceeds the current total.
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0 || amount > totalGold)
            return false;

        totalGold -= amount;
        return true;
    }

    #endregion


    // =========================================================
    // REEL CONTROL
    // =========================================================

    #region Reel Control

    /// <summary>
    /// Starts all three reels spinning.
    /// </summary>
    public void SpinallReels()
    {
        slotReel1.StartSpin();
        slotReel2.StartSpin();
        slotReel3.StartSpin();
    }

    /// <summary>
    /// Resets all reels' current symbol to "Nun" (none) before a new spin.
    /// </summary>
    private void NunAllScrolReel()
    {
        slotReel1.slorRell = SlorRell.Nun;
        slotReel2.slorRell = SlorRell.Nun;
        slotReel3.slorRell = SlorRell.Nun;
    }

    /// <summary>
    /// Called by a SlotReel once it lands on a symbol. Once all three
    /// reels have stopped, calculates and pays out any prize, then
    /// re-enables the betting UI.
    /// </summary>
    public void SpinComplet()
    {
        if (slotReel1.slorRell != SlorRell.Nun &&
            slotReel2.slorRell != SlorRell.Nun &&
            slotReel3.slorRell != SlorRell.Nun)
        {
            int prize = CalculatePrize();

            if (prize > 0)
            {
                AddGold(prize);
                Debug.Log("WIN! Prize: " + prize);
            }


            slotUI.ButtonState();

            slotUI.ButtonState(true);

            slotUI.UpdateUI();
        }
    }

    #endregion


    // =========================================================
    // PRIZE CALCULATION
    // =========================================================

    #region Prize Calculation

    /// <summary>
    /// Works out the prize for the current reel symbols:
    /// all three matching pays the full multiplier, any two matching
    /// pays the reduced "two match" multiplier, otherwise no prize.
    /// </summary>
    private int CalculatePrize()
    {
        // No prize if any reel has not stopped yet
        if (slotReel1.slorRell == SlorRell.Nun ||
            slotReel2.slorRell == SlorRell.Nun ||
            slotReel3.slorRell == SlorRell.Nun)
        {
            return 0;
        }

        // All 3 symbols are the same
        if (slotReel1.slorRell == slotReel2.slorRell &&
            slotReel2.slorRell == slotReel3.slorRell)
        {
            int multiplier = GetMultiplier(slotReel1.slorRell);
            return currentBet * multiplier;
        }

        // Two matching symbols
        if (slotReel1.slorRell == slotReel2.slorRell)
        {
            return currentBet * GetTwoMatchMultiplier(slotReel1.slorRell);
        }

        if (slotReel2.slorRell == slotReel3.slorRell)
        {
            return currentBet * GetTwoMatchMultiplier(slotReel2.slorRell);
        }

        if (slotReel1.slorRell == slotReel3.slorRell)
        {
            return currentBet * GetTwoMatchMultiplier(slotReel1.slorRell);
        }

        return 0;
    }

    /// <summary>
    /// Payout multiplier when all three reels show the same symbol.
    /// </summary>
    private int GetMultiplier(SlorRell symbol)
    {
        switch (symbol)
        {
            case SlorRell.Seven:
                return 20;

            case SlorRell.Bar:
                return 10;

            case SlorRell.Bell:
                return 5;

            case SlorRell.Chery:
                return 3;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Payout multiplier when only two of the three reels match.
    /// </summary>
    private int GetTwoMatchMultiplier(SlorRell symbol)
    {
        switch (symbol)
        {
            case SlorRell.Seven:
                return 5;

            case SlorRell.Bar:
                return 3;

            case SlorRell.Bell:
                return 2;

            case SlorRell.Chery:
                return 1;

            default:
                return 0;
        }
    }

    #endregion
}
