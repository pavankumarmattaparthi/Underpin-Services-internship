using System.Collections;
using UnityEngine;

/// <summary>
/// Controls a single slot reel: scrolls its child symbol objects downward
/// while spinning, recycles symbols that scroll off the bottom back to the
/// top, and aligns the reel to a landed symbol when the spin ends.
/// Symbol child GameObjects must be named to match a
/// <see cref="SlotGameManager.SlorRell"/> enum value, since the landed
/// symbol is resolved by parsing the child's name.
/// </summary>
public class SlotReel : MonoBehaviour
{
    // =========================================================
    // STATE
    // =========================================================

    #region State

    private bool isSpinning;
    private float currentSpinSpeed;

    private string currentSymbolName;

    public string CurrentSymbolName => currentSymbolName;
    public bool IsSpinning => isSpinning;

    /// <summary>
    /// The symbol this reel last landed on. Read/written by
    /// SlotGameManager to check win conditions - do not rename.
    /// </summary>
    public SlotGameManager.SlorRell slorRell;

    #endregion


    // =========================================================
    // INSPECTOR SETTINGS
    // =========================================================

    #region Inspector Settings

    [Header("Spin Duration")]
    [SerializeField] private float additionalSpinDuration = 0f;

    #endregion


    // =========================================================
    // START SPIN
    // =========================================================

    #region Start Spin

    /// <summary>
    /// Begins the spin coroutine with a randomized speed.
    /// Does nothing if the reel is already spinning.
    /// </summary>
    public void StartSpin()
    {
        if (isSpinning)
            return;

        currentSpinSpeed = Random.Range(
            SlotGameManager.Instance.SpinReelMinimumSpeed,
            SlotGameManager.Instance.SpinReelMaximumSpeed
        );

        StartCoroutine(SpinReel());
    }

    #endregion


    // =========================================================
    // SPIN REEL
    // =========================================================

    #region Spin Reel

    /// <summary>
    /// Scrolls the reel for its full duration, then stops and aligns
    /// to the nearest symbol.
    /// </summary>
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
            MoveChildren(currentSpinSpeed);

            timer += Time.deltaTime;

            yield return null;
        }


        // -----------------------------------------------------
        // STOP IMMEDIATELY
        // -----------------------------------------------------

        isSpinning = false;

        AlignReel();
    }

    #endregion


    // =========================================================
    // MOVE CHILDREN DOWN
    // =========================================================

    #region Move Children Down

    /// <summary>
    /// Moves every symbol child downward at the given speed and
    /// recycles any child that scrolls past the bottom limit.
    /// </summary>
    private void MoveChildren(float speed)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            child.localPosition +=
                Vector3.down * speed * Time.deltaTime;
        }

        RecycleLowestChild();
    }

    #endregion


    // =========================================================
    // RECYCLE LOWEST CHILD
    // =========================================================

    #region Recycle Lowest Child

    /// <summary>
    /// If the lowest symbol has scrolled past the bottom limit, moves it
    /// above the highest symbol so the reel appears to loop endlessly.
    /// </summary>
    private void RecycleLowestChild()
    {
        if (transform.childCount <= 1)
            return;

        Transform lowestChild = null;
        Transform highestChild = null;

        float lowestY = Mathf.Infinity;
        float highestY = Mathf.NegativeInfinity;

        // Find the physically lowest and highest children.
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

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

        if (lowestChild == null || highestChild == null)
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

    #endregion


    // =========================================================
    // ALIGN REEL
    // =========================================================

    #region Align Reel

    /// <summary>
    /// Snaps the child symbol closest to the target Y position into place,
    /// evenly spaces the remaining symbols around it, resolves the landed
    /// symbol from its GameObject name, and notifies the game manager
    /// that this reel has finished spinning.
    /// </summary>
    private void AlignReel()
    {
        int childCount = transform.childCount;

        if (childCount == 0)
            return;


        // -----------------------------------------------------
        // Find the child closest to target Y.
        // -----------------------------------------------------

        Transform selectedChild = null;

        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);

            float distance =
                Mathf.Abs(
                    child.localPosition.y -
                    SlotGameManager.Instance.targetY
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                selectedChild = child;
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


        // -----------------------------------------------------
        // Get all children.
        // -----------------------------------------------------

        Transform[] children =
            new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            children[i] = transform.GetChild(i);
        }


        // -----------------------------------------------------
        // CASE 1:
        // Selected child is the LAST child.
        // -----------------------------------------------------

        if (selectedIndex == childCount - 1)
        {
            for (int i = 0; i < childCount; i++)
            {
                Transform child = children[i];

                int difference =
                    i - selectedIndex;

                float newY =
                    SlotGameManager.Instance.targetY -
                    (
                        difference *
                        SlotGameManager.Instance.childSpacing
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
                children[0];

            firstChild.localPosition =
                new Vector3(
                    firstChild.localPosition.x,
                    SlotGameManager.Instance.targetY -
                    SlotGameManager.Instance.childSpacing,
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
                Transform child = children[i];

                int difference =
                    i - selectedIndex;

                float newY =
                    SlotGameManager.Instance.targetY -
                    (
                        difference *
                        SlotGameManager.Instance.childSpacing
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
                children[childCount - 1];

            lastChild.localPosition =
                new Vector3(
                    lastChild.localPosition.x,
                    SlotGameManager.Instance.targetY +
                    SlotGameManager.Instance.childSpacing,
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
                Transform child = children[i];

                int difference =
                    i - selectedIndex;

                float newY =
                    SlotGameManager.Instance.targetY -
                    (
                        difference *
                        SlotGameManager.Instance.childSpacing
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

        selectedPosition.y =
            SlotGameManager.Instance.targetY;

        selectedChild.localPosition =
            selectedPosition;

    }

    #endregion


    // =========================================================
    // GET SYMBOL ENUM
    // =========================================================

    #region Get Symbol Enum

    /// <summary>
    /// Attempts to parse a child's GameObject name into a
    /// <see cref="SlotGameManager.SlorRell"/> value.
    /// </summary>
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

    #endregion


    // =========================================================
    // GET RESULT
    // =========================================================

    #region Get Result

    /// <summary>
    /// Returns the name of the symbol this reel last landed on.
    /// </summary>
    public string GetResult()
    {
        return currentSymbolName;
    }

    #endregion
}
