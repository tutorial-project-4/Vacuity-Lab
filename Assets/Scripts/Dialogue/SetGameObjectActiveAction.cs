using UnityEngine;

public class SetGameObjectActiveAction : DialogueInteractionAction
{
    [SerializeField] private GameObject target;
    [SerializeField] private bool active;
    [SerializeField] private bool runOnce = true;

    private bool hasRun;

    public override void Run()
    {
        if (runOnce && hasRun)
        {
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("[SetGameObjectActiveAction] Target is not assigned.", this);
            return;
        }

        target.SetActive(active);
        hasRun = true;
    }

    private void OnValidate()
    {
        if (target == null)
        {
            Debug.LogWarning("[SetGameObjectActiveAction] Target is not assigned.", this);
        }
    }
}
