using UnityEngine;

[DisallowMultipleComponent]
public sealed class SceneBgmPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private bool playOnStart = true;

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        if (AudioManager.Instance != null && bgmClip != null)
        {
            AudioManager.Instance.PlayBgm(bgmClip, fadeInDuration);
        }
    }

    private void OnValidate()
    {
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
    }
}
