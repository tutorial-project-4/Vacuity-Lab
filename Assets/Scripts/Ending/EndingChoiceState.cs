using UnityEngine;

public enum EndingChoice
{
    None,
    AcceptedInjection,
    RejectedInjection
}

[DisallowMultipleComponent]
public sealed class EndingChoiceState : MonoBehaviour
{
    private static EndingChoiceState instance;
    private static EndingChoice currentChoice = EndingChoice.None;

    [SerializeField] private bool resetOnAwake = true;

    public static EndingChoiceState Instance => instance;
    public static EndingChoice CurrentChoice => currentChoice;
    public static bool AcceptedInjection => currentChoice == EndingChoice.AcceptedInjection;
    public static bool RejectedInjection => currentChoice == EndingChoice.RejectedInjection;

    public EndingChoice SceneChoice => currentChoice;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[EndingChoiceState] Multiple ending choice states exist. Using the newest scene instance.", this);
        }

        instance = this;

        if (resetOnAwake)
        {
            currentChoice = EndingChoice.None;
        }
    }

    public void SetChoice(EndingChoice choice)
    {
        currentChoice = choice;
    }

    public static void SetCurrentChoice(EndingChoice choice)
    {
        currentChoice = choice;
    }
}
