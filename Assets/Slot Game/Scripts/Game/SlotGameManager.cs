using System.Collections;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class SlotGameManager : MonoBehaviour
{
    public static SlotGameManager Instance { get; private set; }

    [Header("SlotReel Script")]
    public SlotReel slotReel1;
    public SlotReel slotReel2;
    public SlotReel slotReel3;

    [Header("SlotUI")]
    [SerializeField] private SlotUI slotUI;

    [Header("Gold and Bet Values")]
    [SerializeField] private int totalGold = 1000;
    [SerializeField] private int currentBet = 10;

    [Header("Reel Settings")]
    public float childSpacing = 1.5f;

    [Header("Position Settings")]
    public float targetY = 1.5f;
    public float bottomLimit = -4.5f;

    [Header("Spin Settings")]
    public float spinDuration = 3f;
    public float slowDownDuration = 1.5f;

    [Header("Spin Speed")]
    public float SpinReelMinimumSpeed = 4;
    public float SpinReelMaximumSpeed = 8;

    [Header("GameObject")]
    [SerializeField] GameObject UppullingLever;
    [SerializeField] GameObject DownpullingLever;

    public enum SlorRell
    {
        Nun,
        Seven,
        Chery,
        Bell,
        Bar
    }

   
    public int TotalGold => totalGold;
    public int CurrentBet => currentBet;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

    }

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

    private IEnumerator LeverAnimation()
    {
        LeverAnimation(true);

        yield return new WaitForSeconds(0.3f);

        LeverAnimation(false);
    }

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

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        totalGold += amount;
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || amount > totalGold)
            return false;

        totalGold -= amount;
        return true;
    }

    public void SpinallReels()
    {
        slotReel1.StartSpin();
        slotReel2.StartSpin();
        slotReel3.StartSpin();
    }

    private void NunAllScrolReel()
    {
        slotReel1.slorRell = SlorRell.Nun;
        slotReel2.slorRell = SlorRell.Nun;
        slotReel3.slorRell = SlorRell.Nun;
    }

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
}