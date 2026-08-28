using System;
using UnityEngine;

public enum PlayerAbility
{
    Dash,
    DoubleJump,
    WallPhaseDash
}

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    private static GameManager instance;

    [Header("Scene Lifetime")]
    [SerializeField] private bool persistAcrossScenes;

    [Header("Initial Player Abilities")]
    [SerializeField] private bool dashUnlocked = true;
    [SerializeField] private bool doubleJumpUnlocked;
    [SerializeField] private bool wallPhaseDashUnlocked;

    public static GameManager Instance => instance;

    public bool IsDashUnlocked => dashUnlocked;
    public bool IsDoubleJumpUnlocked => doubleJumpUnlocked;
    public bool IsWallPhaseDashUnlocked => wallPhaseDashUnlocked;

    public event Action<PlayerAbility, bool> AbilityChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[GameManager] Multiple GameManagers exist. Destroying the newer instance.", this);
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public bool IsAbilityUnlocked(PlayerAbility ability)
    {
        return ability switch
        {
            PlayerAbility.Dash => dashUnlocked,
            PlayerAbility.DoubleJump => doubleJumpUnlocked,
            PlayerAbility.WallPhaseDash => wallPhaseDashUnlocked,
            _ => false
        };
    }

    public void UnlockAbility(PlayerAbility ability)
    {
        SetAbilityUnlocked(ability, true);
    }

    public void LockAbility(PlayerAbility ability)
    {
        SetAbilityUnlocked(ability, false);
    }

    public void SetAbilityUnlocked(PlayerAbility ability, bool unlocked)
    {
        bool changed = false;
        switch (ability)
        {
            case PlayerAbility.Dash:
                changed = SetIfChanged(ref dashUnlocked, unlocked);
                break;
            case PlayerAbility.DoubleJump:
                changed = SetIfChanged(ref doubleJumpUnlocked, unlocked);
                break;
            case PlayerAbility.WallPhaseDash:
                changed = SetIfChanged(ref wallPhaseDashUnlocked, unlocked);
                break;
        }

        if (changed)
        {
            AbilityChanged?.Invoke(ability, unlocked);
            Debug.Log($"[GameManager] {ability} {(unlocked ? "unlocked" : "locked")}.", this);
        }
    }

    public void UnlockAbilities(params PlayerAbility[] abilities)
    {
        if (abilities == null)
        {
            return;
        }

        for (int i = 0; i < abilities.Length; i++)
        {
            UnlockAbility(abilities[i]);
        }
    }

    public void ResetAbilities(bool dash, bool doubleJump, bool wallPhaseDash)
    {
        SetAbilityUnlocked(PlayerAbility.Dash, dash);
        SetAbilityUnlocked(PlayerAbility.DoubleJump, doubleJump);
        SetAbilityUnlocked(PlayerAbility.WallPhaseDash, wallPhaseDash);
    }

    private static bool SetIfChanged(ref bool field, bool value)
    {
        if (field == value)
        {
            return false;
        }

        field = value;
        return true;
    }
}
