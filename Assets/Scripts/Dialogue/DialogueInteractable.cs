using UnityEngine;
using UnityEngine.InputSystem;

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

    private PlayerController currentPlayer;
    private bool hasPlayed;

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
            runner.StartDialogue(lines, currentPlayer);
            hasPlayed = true;
        }
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

    private void OnValidate()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }
}
