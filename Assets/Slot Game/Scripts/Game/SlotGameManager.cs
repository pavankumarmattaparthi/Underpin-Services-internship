using System.Collections;
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

    [System.Serializable]
    public struct SymbolWeight
    {
        public SlorRell symbol;
        [Min(0f)] public float weight;
    }

    // Virtual reel strips: real slot machines decide outcomes from a
    // weighted probability table (a "virtual reel") that is independent
    // of how many physical symbols are on the visible belt. This lets
    // rare/high-paying symbols (Seven) show up far less often than the
    // belt's physical 1-in-4 layout would otherwise suggest, exactly
    // like real cabinets have used since the 1980s "virtual reel
    // mapping" technique.
    [Header("Reel 1 - Virtual Reel Weights")]
    [SerializeField]
    private SymbolWeight[] reel1Weights = DefaultWeights();

    [Header("Reel 2 - Virtual Reel Weights")]
    [SerializeField]
    private SymbolWeight[] reel2Weights = DefaultWeights();

    [Header("Reel 3 - Virtual Reel Weights")]
    [SerializeField]
    private SymbolWeight[] reel3Weights = DefaultWeights();

    // System.Random rather than UnityEngine.Random so odds can be unit
    // tested / reseeded outside of PlayMode. A real cash-money cabinet
    // would use a certified hardware/PRNG source instead of either.
    private readonly System.Random rng = new System.Random();

    private static SymbolWeight[] DefaultWeights()
    {
        return new SymbolWeight[]
        {
            new SymbolWeight { symbol = SlorRell.Chery, weight = 40f },
            new SymbolWeight { symbol = SlorRell.Bell,  weight = 30f },
            new SymbolWeight { symbol = SlorRell.Bar,   weight = 20f },
            new SymbolWeight { symbol = SlorRell.Seven, weight = 10f },
        };
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

        SpinallReels();

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
        // Decide every reel's outcome up front from the RNG before any
        // animation plays. The spin itself is purely visual - the
        // result is never read back off physical reel positions like
        // it used to be.
        SlorRell result1 = RollWeightedSymbol(reel1Weights);
        SlorRell result2 = RollWeightedSymbol(reel2Weights);
        SlorRell result3 = RollWeightedSymbol(reel3Weights);

        slotReel1.StartSpin(result1);
        slotReel2.StartSpin(result2);
        slotReel3.StartSpin(result3);
    }

    private SlorRell RollWeightedSymbol(SymbolWeight[] weights)
    {
        float totalWeight = 0f;

        for (int i = 0; i < weights.Length; i++)
            totalWeight += weights[i].weight;

        double roll = rng.NextDouble() * totalWeight;

        float cumulative = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i].weight;

            if (roll < cumulative)
                return weights[i].symbol;
        }

        // Floating point rounding safety net - land on the last entry.
        return weights[weights.Length - 1].symbol;
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

        // Real single-payline slot machines only pay when every symbol
        // on the payline matches exactly - a 2-of-3 "near miss" is not
        // a win. Removing the old 2-match payout also keeps the RTP
        // (return to player) calculable and realistic; see
        // LogTheoreticalRTP().
        if (slotReel1.slorRell == slotReel2.slorRell &&
            slotReel2.slorRell == slotReel3.slorRell)
        {
            int multiplier = GetMultiplier(slotReel1.slorRell);
            return currentBet * multiplier;
        }

        return 0;
    }

    private int GetMultiplier(SlorRell symbol)
    {
        switch (symbol)
        {
            case SlorRell.Seven:
                return 100;

            case SlorRell.Bar:
                return 25;

            case SlorRell.Bell:
                return 10;

            case SlorRell.Chery:
                return 5;

            default:
                return 0;
        }
    }

    // Verifies the game's Return To Player against the configured
    // weights/paytable, the way a real studio would validate a slot's
    // math model before shipping. With the default weights this
    // reports ~89% RTP.
    [ContextMenu("Log Theoretical RTP")]
    private void LogTheoreticalRTP()
    {
        double rtp = 0.0;

        foreach (SlorRell symbol in System.Enum.GetValues(typeof(SlorRell)))
        {
            if (symbol == SlorRell.Nun)
                continue;

            double p1 = GetWeightFraction(reel1Weights, symbol);
            double p2 = GetWeightFraction(reel2Weights, symbol);
            double p3 = GetWeightFraction(reel3Weights, symbol);

            rtp += p1 * p2 * p3 * GetMultiplier(symbol);
        }

        Debug.Log($"Theoretical RTP: {rtp:P2}");
    }

    private static float GetWeightFraction(SymbolWeight[] weights, SlorRell symbol)
    {
        float total = 0f;
        float match = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            total += weights[i].weight;

            if (weights[i].symbol == symbol)
                match += weights[i].weight;
        }

        return total > 0f ? match / total : 0f;
    }
}
