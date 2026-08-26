using UnityEngine;

public class BarrierActivateAction : DialogueInteractionAction
{
    [SerializeField] private Collider2D targetCollider;
    [SerializeField] private TerrainDescriptor targetTerrainDescriptor;
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private string animatorTriggerName = "Active";
    [SerializeField] private string resetAnimatorTriggerName = "Deactive";
    [SerializeField] private string animationStateName = "barrier_activate";
    [SerializeField] private bool runOnce = true;

    private bool hasRun;

    public override void Run()
    {
        if (runOnce && hasRun)
        {
            return;
        }

        bool changedState = false;

        if (targetCollider != null)
        {
            targetCollider.enabled = true;
            changedState = true;
        }

        if (targetTerrainDescriptor != null)
        {
            targetTerrainDescriptor.enabled = true;
            changedState = true;
        }

        if (targetAnimator != null)
        {
            if (!string.IsNullOrWhiteSpace(resetAnimatorTriggerName))
            {
                targetAnimator.ResetTrigger(resetAnimatorTriggerName);
            }

            if (!string.IsNullOrWhiteSpace(animatorTriggerName))
            {
                targetAnimator.SetTrigger(animatorTriggerName);
            }

            if (!string.IsNullOrWhiteSpace(animationStateName))
            {
                targetAnimator.Play(animationStateName, 0, 0f);
            }

            changedState = true;
        }

        if (changedState)
        {
            hasRun = true;
        }
    }

    private void OnValidate()
    {
        if (targetCollider == null)
        {
            Debug.LogWarning("[BarrierActivateAction] Target Collider is not assigned.", this);
        }

        if (targetAnimator == null)
        {
            Debug.LogWarning("[BarrierActivateAction] Target Animator is not assigned.", this);
        }
    }
}
