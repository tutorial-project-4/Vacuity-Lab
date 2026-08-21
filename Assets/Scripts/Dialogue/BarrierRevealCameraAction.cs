using System.Collections;
using UnityEngine;

public class BarrierRevealCameraAction : DialogueInteractionAction
{
    [SerializeField] private CameraFocusTrigger revealCameraFocus;
    [SerializeField] private DialogueInteractionAction[] revealActions;
    [SerializeField] private CameraFocusTrigger returnCameraFocus;
    [SerializeField] private float delayBeforeReveal = 0.35f;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private bool runOnce = true;

    private bool hasRun;
    private Coroutine revealRoutine;

    public override void Run()
    {
        if (runOnce && hasRun)
        {
            return;
        }

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
        }

        revealRoutine = StartCoroutine(RevealRoutine());
        hasRun = true;
    }

    private IEnumerator RevealRoutine()
    {
        Transform playerTransform = FindPlayerTransform();

        if (revealCameraFocus != null)
        {
            revealCameraFocus.Focus(playerTransform);
        }
        else
        {
            Debug.LogWarning("[BarrierRevealCameraAction] Reveal Camera Focus is not assigned.", this);
        }

        yield return new WaitForSeconds(delayBeforeReveal);
        RunRevealActions();

        yield return new WaitForSeconds(holdDuration);

        if (returnCameraFocus != null)
        {
            returnCameraFocus.Focus(playerTransform);
        }

        revealRoutine = null;
    }

    private void RunRevealActions()
    {
        if (revealActions == null)
        {
            return;
        }

        for (int i = 0; i < revealActions.Length; i++)
        {
            if (revealActions[i] != null)
            {
                revealActions[i].Run();
            }
        }
    }

    private Transform FindPlayerTransform()
    {
#if UNITY_2023_1_OR_NEWER
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
#else
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
#endif
        return playerMovement != null ? playerMovement.transform : null;
    }

    private void OnValidate()
    {
        delayBeforeReveal = Mathf.Max(0f, delayBeforeReveal);
        holdDuration = Mathf.Max(0f, holdDuration);

        if (revealCameraFocus == null)
        {
            Debug.LogWarning("[BarrierRevealCameraAction] Reveal Camera Focus is not assigned.", this);
        }
    }
}
