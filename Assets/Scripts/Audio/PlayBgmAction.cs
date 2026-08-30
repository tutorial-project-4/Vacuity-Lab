using UnityEngine;

public sealed class PlayBgmAction : DialogueInteractionAction
{
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private float fadeDuration = 0.6f;

    public override void Run()
    {
        if (bgmClip == null || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlayBgm(bgmClip, fadeDuration);
    }

    private void OnValidate()
    {
        fadeDuration = Mathf.Max(0f, fadeDuration);
    }
}
