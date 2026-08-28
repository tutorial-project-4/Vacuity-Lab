using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class DialogueInteractable : MonoBehaviour
{
    private enum DialogueCondition
    {
        Always,
        Boss1NotCleared,
        Boss1Cleared
    }

    [System.Serializable]
    private sealed class DialogueSegment
    {
        public string segmentName;
        public DialogueCondition condition = DialogueCondition.Always;
        public int startIndex;
        public int endIndexInclusive;
        public int requiredPlayCount = -1;
        public int maxPlayCount = -1;
        public DialogueInteractionAction[] actionsBeforeDialogue;
        public float delayBeforeDialogue;
        public DialogueInteractionAction[] actionsOnComplete;
    }

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] lines =
    {
        new DialogueLine
        {
            speaker = "나레이션",
            text = "주사기다. 무언가를 주입당한 모양이다. 아무것도 기억나지 않는다."
        }
    };
    [SerializeField] private BossProgressState progressState;
    [SerializeField] private DialogueSegment[] segments;

    [Header("Interaction")]
    [SerializeField] private string promptLabel = "F";
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private DialogueInteractionAction[] actionsOnInteract;

    private PlayerController currentPlayer;
    private bool hasPlayed;
    private Coroutine interactionRoutine;
    private int[] segmentPlayCounts;

    private void Update()
    {
        DialogueRunner runner = DialogueRunner.Instance;
        if (runner == null || currentPlayer == null || (hasPlayed && triggerOnce) || runner.IsRunning)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            interactionRoutine = StartCoroutine(InteractionRoutine(runner, currentPlayer));
        }
    }

    private IEnumerator InteractionRoutine(DialogueRunner runner, PlayerController player)
    {
        DialogueSegment segment = SelectSegment();
        int segmentIndex = GetSegmentIndex(segment);
        RunInteractionActions(segment?.actionsBeforeDialogue);
        if (segment != null && segment.delayBeforeDialogue > 0f)
        {
            yield return new WaitForSeconds(segment.delayBeforeDialogue);
        }

        DialogueLine[] selectedLines = BuildLinesForSegment(segment);
        if (!runner.StartDialogue(selectedLines, player))
        {
            interactionRoutine = null;
            yield break;
        }

        yield return null;
        while (runner != null && runner.IsRunning)
        {
            yield return null;
        }

        if (runner != null && runner.LastDialogueCompleted)
        {
            hasPlayed = true;
            IncrementSegmentPlayCount(segmentIndex);
            RunInteractionActions(segment != null ? segment.actionsOnComplete : actionsOnInteract);
        }

        interactionRoutine = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        currentPlayer = player;
        ShowPrompt(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player != currentPlayer)
        {
            return;
        }

        currentPlayer = null;
        ShowPrompt(false);
    }

    private void ShowPrompt(bool visible)
    {
        DialogueRunner runner = DialogueRunner.Instance;
        if (runner == null)
        {
            return;
        }

        if (hasPlayed && triggerOnce)
        {
            visible = false;
        }

        runner.ShowPrompt(visible, promptLabel);
    }

    private DialogueSegment SelectSegment()
    {
        if (segments == null || segments.Length == 0)
        {
            return null;
        }

        BossProgressState state = GetProgressState();
        for (int i = 0; i < segments.Length; i++)
        {
            DialogueSegment segment = segments[i];
            if (segment != null && IsConditionMet(segment.condition, state) && IsPlayCountMet(segment, i))
            {
                return segment;
            }
        }

        return null;
    }

    private bool IsPlayCountMet(DialogueSegment segment, int index)
    {
        int playCount = GetSegmentPlayCount(index);
        if (segment.requiredPlayCount >= 0 && playCount != segment.requiredPlayCount)
        {
            return false;
        }

        return segment.maxPlayCount < 0 || playCount < segment.maxPlayCount;
    }

    private bool IsConditionMet(DialogueCondition condition, BossProgressState state)
    {
        return condition switch
        {
            DialogueCondition.Boss1Cleared => state != null && state.IsBoss1Cleared,
            DialogueCondition.Boss1NotCleared => state == null || !state.IsBoss1Cleared,
            _ => true
        };
    }

    private DialogueLine[] BuildLinesForSegment(DialogueSegment segment)
    {
        if (segment == null)
        {
            return lines;
        }

        if (lines == null || lines.Length == 0)
        {
            return lines;
        }

        int start = Mathf.Clamp(segment.startIndex, 0, lines.Length - 1);
        int end = Mathf.Clamp(segment.endIndexInclusive, start, lines.Length - 1);
        int count = end - start + 1;
        DialogueLine[] selectedLines = new DialogueLine[count];
        System.Array.Copy(lines, start, selectedLines, 0, count);
        return selectedLines;
    }

    private BossProgressState GetProgressState()
    {
        if (progressState != null)
        {
            return progressState;
        }

#if UNITY_2023_1_OR_NEWER
        progressState = FindFirstObjectByType<BossProgressState>();
#else
        progressState = FindObjectOfType<BossProgressState>();
#endif
        return progressState;
    }

    private int GetSegmentIndex(DialogueSegment segment)
    {
        if (segment == null || segments == null)
        {
            return -1;
        }

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == segment)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetSegmentPlayCount(int index)
    {
        EnsureSegmentPlayCounts();
        if (index < 0 || index >= segmentPlayCounts.Length)
        {
            return 0;
        }

        return segmentPlayCounts[index];
    }

    private void IncrementSegmentPlayCount(int index)
    {
        EnsureSegmentPlayCounts();
        if (index >= 0 && index < segmentPlayCounts.Length)
        {
            segmentPlayCounts[index]++;
        }
    }

    private void EnsureSegmentPlayCounts()
    {
        int length = segments != null ? segments.Length : 0;
        if (segmentPlayCounts != null && segmentPlayCounts.Length == length)
        {
            return;
        }

        segmentPlayCounts = new int[length];
    }

    private void RunInteractionActions(DialogueInteractionAction[] actions)
    {
        if (actions == null)
        {
            return;
        }

        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i] != null)
            {
                actions[i].Run();
            }
        }
    }

    private void OnValidate()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    private void OnDisable()
    {
        if (interactionRoutine != null)
        {
            StopCoroutine(interactionRoutine);
            interactionRoutine = null;
        }
    }
}
