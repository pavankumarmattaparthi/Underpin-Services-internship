using System.Collections;
using UnityEngine;

public class SlotReel : MonoBehaviour
{
    private bool isSpinning;
    private float currentSpinSpeed;

    private string currentSymbolName;

    public string CurrentSymbolName => currentSymbolName;
    public bool IsSpinning => isSpinning;

    public SlotGameManager.SlorRell slorRell;


    [Header("Spin Duration")]
    [SerializeField] private float additionalSpinDuration = 0f;


    // =========================================================
    // START SPIN
    // =========================================================

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


    // =========================================================
    // MOVE CHILDREN DOWN
    // =========================================================

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


    // =========================================================
    // RECYCLE LOWEST CHILD
    // =========================================================

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


    // =========================================================
    // ALIGN REEL
    // =========================================================

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