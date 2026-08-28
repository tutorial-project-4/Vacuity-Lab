using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PlayerAttack))]
public sealed class PlayerAttackAudio : MonoBehaviour
{
    [SerializeField] private PlayerAttack attack;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] swingClips;
    [SerializeField] private AudioClip hitClip;
    [SerializeField, Range(0f, 1f)] private float swingVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float hitVolume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.96f, 1.04f);

    void Awake()
    {
        CacheComponents();
        ConfigureSource();
    }

    void OnEnable()
    {
        CacheComponents();
        if (attack != null)
        {
            attack.AttackStarted += HandleAttackStarted;
            attack.AttackConnected += HandleAttackConnected;
        }
    }

    void OnDisable()
    {
        if (attack != null)
        {
            attack.AttackStarted -= HandleAttackStarted;
            attack.AttackConnected -= HandleAttackConnected;
        }
    }

    private void HandleAttackStarted()
    {
        PlayOneShot(GetRandomClip(swingClips), swingVolume, true);
    }

    private void HandleAttackConnected()
    {
        PlayOneShot(hitClip, hitVolume, false);
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        return clips[Random.Range(0, clips.Length)];
    }

    private void PlayOneShot(AudioClip clip, float volume, bool randomizePitch)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.pitch = randomizePitch ? Random.Range(pitchRange.x, pitchRange.y) : 1f;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void CacheComponents()
    {
        if (attack == null)
        {
            attack = GetComponent<PlayerAttack>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void ConfigureSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    void OnValidate()
    {
        swingVolume = Mathf.Clamp01(swingVolume);
        hitVolume = Mathf.Clamp01(hitVolume);
        if (pitchRange.x > pitchRange.y)
        {
            (pitchRange.x, pitchRange.y) = (pitchRange.y, pitchRange.x);
        }

        ConfigureSource();
    }
}
