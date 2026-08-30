using UnityEngine;

public enum DialogueAudioChannel
{
    Sfx,
    Ui,
    Story
}

public sealed class PlayDialogueAudioAction : DialogueInteractionAction
{
    [SerializeField] private AudioClip clip;
    [SerializeField] private DialogueAudioChannel channel = DialogueAudioChannel.Story;

    public override void Run()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null || clip == null)
        {
            return;
        }

        switch (channel)
        {
            case DialogueAudioChannel.Ui:
                audioManager.PlayUi(clip);
                break;
            case DialogueAudioChannel.Story:
                audioManager.PlayStory(clip);
                break;
            default:
                audioManager.PlaySfx(clip);
                break;
        }
    }
}
