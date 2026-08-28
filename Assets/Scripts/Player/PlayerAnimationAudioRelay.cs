using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAnimationAudioRelay : MonoBehaviour
{
    [SerializeField] private PlayerAudio playerAudio;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.75f;

    void Awake()
    {
        CacheComponents();
    }

    public void PlayFootstep()
    {
        CacheComponents();
        if (playerAudio == null || footstepClips == null || footstepClips.Length == 0)
        {
            return;
        }

        playerAudio.PlayFootstep(footstepClips[Random.Range(0, footstepClips.Length)], footstepVolume);
    }

    private void CacheComponents()
    {
        if (playerAudio == null)
        {
            playerAudio = GetComponentInParent<PlayerAudio>();
        }
    }

    void OnValidate()
    {
        footstepVolume = Mathf.Clamp01(footstepVolume);
    }
}
