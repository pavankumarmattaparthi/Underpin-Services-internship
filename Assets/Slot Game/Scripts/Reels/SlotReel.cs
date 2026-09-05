using System.Collections;
using UnityEngine;

public class SlotReel : MonoBehaviour
{
    private bool isSpinning;
    private float currentSpinSpeed;

    private string currentSymbolName;

    // The outcome, decided by SlotGameManager's weighted RNG before the
    // spin animation starts. The reel's job is purely visual: land on
    // whichever belt symbol matches this value.
    private SlotGameManager.SlorRell targetSymbol;

    public string CurrentSymbolName => currentSymbolName;
    public bool IsSpinning => isSpinning;

    public SlotGameManager.SlorRell slorRell;


    [Header("Spin Duration")]
    [SerializeField] private float additionalSpinDuration = 0f;


    // =========================================================
    // START SPIN
    // =========================================================

    public void StartSpin(SlotGameManager.SlorRell resultSymbol)
    {
        if (isSpinning)
            return;

        targetSymbol = resultSymbol;

        currentSpinSpeed = Random.Range(
            SlotGameManager.Instance.SpinReelMinimumSpeed,
            SlotGameManager.Instance.SpinReelMaximumSpeed
        );

        StartCoroutine(SpinReel());
    }


    // =========================================================
    // SPIN REEL
    // =========================================================

    private IEnumerator SpinReel()
    {
        isSpinning = true;

        float timer = 0f;

        // -----------------------------------------------------
        // NORMAL SPIN
        // -----------------------------------------------------

        float totalSpinDuration =
            SlotGameManager.Instance.spinDuration +
            additionalSpinDuration;

        while (timer < totalSpinDuration)
        {
            float deltaTime = Time.deltaTime;

            MoveChildren(currentSpinSpeed, deltaTime);

            timer += deltaTime;

            yield return null;
        }


        // -----------------------------------------------------
        // STOP IMMEDIATELY
        // -----------------------------------------------------

        isSpinning = false;

        AlignReel();
    }


    // =========================================================
    // MOVE CHILDREN DOWN
    // =========================================================

    private void MoveChildren(float speed, float deltaTime)
    {
        int childCount = transform.childCount;

        Vector3 delta = Vector3.down * speed * deltaTime;

        Transform lowestChild = null;
        Transform highestChild = null;

        float lowestY = Mathf.Infinity;
        float highestY = Mathf.NegativeInfinity;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);

            child.localPosition += delta;

            // Track the physically lowest and highest children
            // in the same pass instead of a separate loop.
            float y = child.localPosition.y;

            if (y < lowestY)
            {
                lowestY = y;
                lowestChild = child;
            }

            if (y > highestY)
            {
                highestY = y;
                highestChild = child;
            }
        }

        RecycleLowestChild(childCount, lowestChild, highestChild);
    }


    // =========================================================
    // RECYCLE LOWEST CHILD
    // =========================================================

    private void RecycleLowestChild(int childCount, Transform lowestChild, Transform highestChild)
    {
        if (childCount <= 1 || lowestChild == null || highestChild == null)
            return;

        // Move the lowest child above the highest child.
        if (lowestChild.localPosition.y <=
            SlotGameManager.Instance.bottomLimit)
        {
            float newY =
                highestChild.localPosition.y +
                SlotGameManager.Instance.childSpacing;

            Vector3 position =
                lowestChild.localPosition;

            position.y = newY;

            lowestChild.localPosition = position;
        }
    }


    // =========================================================
    // ALIGN REEL
    // =========================================================

    private void AlignReel()
    {
        int childCount = transform.childCount;

        if (childCount == 0)
            return;

        // Cache singleton reads once instead of re-fetching them
        // on every loop iteration below.
        float targetY = SlotGameManager.Instance.targetY;
        float childSpacing = SlotGameManager.Instance.childSpacing;


        // -----------------------------------------------------
        // Find the belt symbol matching the RNG-decided result,
        // breaking ties by whichever copy is closest to the
        // payline. The outcome was already decided before the
        // spin started - this just picks where to land on it.
        // -----------------------------------------------------

        string targetName = targetSymbol.ToString();

        Transform selectedChild = null;

        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (!string.Equals(child.name, targetName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            float distance = Mathf.Abs(child.localPosition.y - targetY);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                selectedChild = child;
            }
        }

        // Defensive fallback: if the belt has no symbol matching the
        // RNG result (a misconfigured belt), land on whichever symbol
        // is nearest the payline instead of leaving the reel stuck.
        if (selectedChild == null)
        {
            Debug.LogWarning(
                $"{name}: no belt symbol matches RNG result '{targetName}'. Falling back to nearest symbol."
            );

            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);

                float distance = Mathf.Abs(child.localPosition.y - targetY);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    selectedChild = child;
                }
            }
        }

        if (selectedChild == null)
            return;


        // -----------------------------------------------------
        // Store selected symbol.
        // -----------------------------------------------------

        currentSymbolName = selectedChild.name;

        int selectedIndex =
            selectedChild.GetSiblingIndex();


        // -----------------------------------------------------
        // Convert child name to enum.
        // -----------------------------------------------------

        if (TryGetSymbolFromChild(
                selectedChild,
                out SlotGameManager.SlorRell symbol))
        {
            slorRell = symbol;

            SlotGameManager.Instance.SpinComplet();
        }
        else
        {
            Debug.LogWarning($"{name}: selected child '{selectedChild.name}' has no matching SlorRell enum value.");
        }


        // -----------------------------------------------------
        // CASE 1:
        // Selected child is the LAST child.
        // -----------------------------------------------------

        if (selectedIndex == childCount - 1)
        {
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);

                int difference =
                    i - selectedIndex;

                float newY =
                    targetY -
                    (
                        difference *
                        childSpacing
                    );

                child.localPosition =
                    new Vector3(
                        child.localPosition.x,
                        newY,
                        child.localPosition.z
                    );
            }


            // First child goes below selected child.
            Transform firstChild =
                transform.GetChild(0);

            firstChild.localPosition =
                new Vector3(
                    firstChild.localPosition.x,
                    targetY - childSpacing,
                    firstChild.localPosition.z
                );
        }


        // -----------------------------------------------------
        // CASE 2:
        // Selected child is the FIRST child.
        // -----------------------------------------------------

        else if (selectedIndex == 0)
        {
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);

                int difference =
                    i - selectedIndex;

                float newY =
                    targetY -
                    (
                        difference *
                        childSpacing
                    );

                child.localPosition =
                    new Vector3(
                        child.localPosition.x,
                        newY,
                        child.localPosition.z
                    );
            }


            // Last child goes above selected child.
            Transform lastChild =
                transform.GetChild(childCount - 1);

            lastChild.localPosition =
                new Vector3(
                    lastChild.localPosition.x,
                    targetY + childSpacing,
                    lastChild.localPosition.z
                );
        }


        // -----------------------------------------------------
        // CASE 3:
        // Selected child is in the middle.
        // -----------------------------------------------------

        else
        {
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);

                int difference =
                    i - selectedIndex;

                float newY =
                    targetY -
                    (
                        difference *
                        childSpacing
                    );

                child.localPosition =
                    new Vector3(
                        child.localPosition.x,
                        newY,
                        child.localPosition.z
                    );
            }
        }


        // -----------------------------------------------------
        // Make selected child exactly target Y.
        // -----------------------------------------------------

        Vector3 selectedPosition =
            selectedChild.localPosition;

        selectedPosition.y = targetY;

        selectedChild.localPosition =
            selectedPosition;

    }


    // =========================================================
    // GET SYMBOL ENUM
    // =========================================================

    private bool TryGetSymbolFromChild(
        Transform child,
        out SlotGameManager.SlorRell symbol)
    {
        return System.Enum.TryParse(
            child.name,
            true,
            out symbol
        );
    }


    // =========================================================
    // GET RESULT
    // =========================================================

    public string GetResult()
    {
        return currentSymbolName;
    }
}