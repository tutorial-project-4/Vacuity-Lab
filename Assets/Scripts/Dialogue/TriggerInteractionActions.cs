using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TriggerInteractionActions : MonoBehaviour
{
    [SerializeField] private DialogueInteractionAction[] actionsOnTrigger;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    private void Reset()
    {
        ConfigureTriggerCollider();
    }

    private void Awake()
    {
        ConfigureTriggerCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerMovement>() == null)
        {
            return;
        }

        RunActions();
        hasTriggered = true;
    }

    private void RunActions()
    {
        if (actionsOnTrigger == null)
        {
            return;
        }

        for (int i = 0; i < actionsOnTrigger.Length; i++)
        {
            if (actionsOnTrigger[i] != null)
            {
                actionsOnTrigger[i].Run();
            }
        }
    }

    private void ConfigureTriggerCollider()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnValidate()
    {
        ConfigureTriggerCollider();
    }
}
