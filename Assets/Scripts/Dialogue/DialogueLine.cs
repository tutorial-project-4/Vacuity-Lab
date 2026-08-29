using System;
using UnityEngine;

[Serializable]
public struct DialogueLine
{
    public string speaker;
    [TextArea]
    public string text;
    public Sprite image;
    public bool hideDialogueWhileImage;
    public DialogueChoice[] choices;
}

[Serializable]
public struct DialogueChoice
{
    public string text;
    public DialogueLine[] nextLines;
    public DialogueInteractionAction[] actionsOnChoose;
    public DialogueInteractionAction[] actionsOnComplete;
}
