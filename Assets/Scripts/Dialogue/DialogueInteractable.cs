using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class DialogueInteractable : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] lines =
    {
        new DialogueLine
        {
            speaker = "나레이션",
            text = "주사기다. 무언가를 주입당한 모양이다. 아무것도 기억나지 않는다."
        }
    };

    [Header("Interaction")]
    [SerializeField] private string promptLabel = "F";
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private DialogueInteractionAction[] actionsOnInteract;

    private PlayerController currentPlayer;
    private bool hasPlayed;
    private Coroutine interactionRoutine;

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
        hasPlayed = true;
        runner.StartDialogue(lines, player);

        yield return null;
        while (runner != null && runner.IsRunning)
        {
            yield return null;
        }

        RunInteractionActions();
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

    private void RunInteractionActions()
    {
        if (actionsOnInteract == null)
        {
            return;
        }

        for (int i = 0; i < actionsOnInteract.Length; i++)
        {
            if (actionsOnInteract[i] != null)
            {
                actionsOnInteract[i].Run();
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
