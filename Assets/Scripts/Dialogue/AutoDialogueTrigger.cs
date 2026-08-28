using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class AutoDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private DialogueLine[] lines =
    {
        new DialogueLine
        {
            speaker = "나레이션",
            text = "강한 전류가 흐르고 있다. 가까이 가면 위험할 것 같다."
        }
    };

    [Header("Trigger")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        DialogueRunner runner = GetDialogueRunner();
        if (runner == null || runner.IsRunning)
        {
            return;
        }

        if (runner.StartDialogue(lines, player))
        {
            hasTriggered = true;
        }
    }

    private DialogueRunner GetDialogueRunner()
    {
        if (dialogueRunner != null)
        {
            return dialogueRunner;
        }

        dialogueRunner = DialogueRunner.Instance;
        return dialogueRunner;
    }

    private void Reset()
    {
        ConfigureCollider();
    }

    private void Awake()
    {
        ConfigureCollider();
    }

    private void OnValidate()
    {
        ConfigureCollider();
    }

    private void ConfigureCollider()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }
}
