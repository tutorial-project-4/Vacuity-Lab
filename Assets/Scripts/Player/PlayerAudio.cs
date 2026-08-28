using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class PlayerAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerWallPhaseDash wallPhaseDash;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private AudioSource sfxSource;

    [Header("Movement")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip doubleJumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip dashClip;
    [SerializeField, Range(0f, 1f)] private float movementVolume = 1f;
    [SerializeField] private float minLandSpeed = 1f;

    [Header("Health")]
    [SerializeField] private AudioClip damageClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip healClip;
    [SerializeField, Range(0f, 1f)] private float healthVolume = 1f;

    [Header("Ability")]
    [SerializeField] private AudioClip skillActivateClip;
    [SerializeField] private AudioClip abilityAcquireClip;
    [SerializeField, Range(0f, 1f)] private float abilityVolume = 1f;

    void Awake()
    {
        CacheComponents();
        ConfigureSources();
    }

    void OnEnable()
    {
        CacheComponents();
        Subscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    public void PlaySkillActivate()
    {
        PlayOneShot(skillActivateClip, abilityVolume);
    }

    public void PlayAbilityAcquire()
    {
        PlayOneShot(abilityAcquireClip, abilityVolume);
    }

    public void PlayFootstep(AudioClip clip, float volume = 1f)
    {
        PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void Subscribe()
    {
        if (movement != null)
        {
            movement.Jumped += HandleJumped;
            movement.Landed += HandleLanded;
            movement.DashStarted += HandleDashStarted;
        }

        if (wallPhaseDash != null)
        {
            wallPhaseDash.DashStarted += HandleWallPhaseDashStarted;
        }

        if (health != null)
        {
            health.Damaged += HandleDamaged;
            health.Healed += HandleHealed;
            health.Died += HandleDied;
        }
    }

    private void Unsubscribe()
    {
        if (movement != null)
        {
            movement.Jumped -= HandleJumped;
            movement.Landed -= HandleLanded;
            movement.DashStarted -= HandleDashStarted;
        }

        if (wallPhaseDash != null)
        {
            wallPhaseDash.DashStarted -= HandleWallPhaseDashStarted;
        }

        if (health != null)
        {
            health.Damaged -= HandleDamaged;
            health.Healed -= HandleHealed;
            health.Died -= HandleDied;
        }
    }

    private void HandleJumped(PlayerJumpType jumpType)
    {
        PlayOneShot(jumpType == PlayerJumpType.Air ? doubleJumpClip : jumpClip, movementVolume);
    }

    private void HandleLanded(float fallSpeed)
    {
        if (fallSpeed >= minLandSpeed)
        {
            PlayOneShot(landClip, movementVolume);
        }
    }

    private void HandleDashStarted()
    {
        PlayOneShot(dashClip, movementVolume);
    }

    private void HandleWallPhaseDashStarted()
    {
        PlaySkillActivate();
    }

    private void HandleDamaged(int damage, Vector2 sourcePosition)
    {
        PlayOneShot(damageClip, healthVolume);
    }

    private void HandleHealed(int hearts)
    {
        PlayOneShot(healClip, healthVolume);
    }

    private void HandleDied()
    {
        PlayOneShot(deathClip, healthVolume);
    }

    private void PlayOneShot(AudioClip clip, float volume)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }

    private void CacheComponents()
    {
        if (movement == null)
        {
            movement = GetComponent<PlayerMovement>();
        }

        if (health == null)
        {
            health = GetComponent<PlayerHealth>();
        }

        if (wallPhaseDash == null)
        {
            wallPhaseDash = GetComponent<PlayerWallPhaseDash>();
        }

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
    }

    private void ConfigureSources()
    {
        ConfigureSource(sfxSource, false);
    }

    private static void ConfigureSource(AudioSource source, bool loop)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
    }

    void OnValidate()
    {
        minLandSpeed = Mathf.Max(0f, minLandSpeed);
        movementVolume = Mathf.Clamp01(movementVolume);
        healthVolume = Mathf.Clamp01(healthVolume);
        abilityVolume = Mathf.Clamp01(abilityVolume);
        ConfigureSources();
    }
}
