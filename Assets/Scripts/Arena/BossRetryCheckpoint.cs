using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BossRetryCheckpoint : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform respawnPoint;

    [Header("Camera")]
    [SerializeField] private CameraFocusTrigger retryCameraFocus;

    [Header("Boss")]
    [SerializeField] private MonoBehaviour bossRetryTarget;
    [SerializeField] private bool beginBattleAfterRetry;

    [Header("Arena State")]
    [SerializeField] private Transform[] raisedStateTargets;
    [SerializeField] private float raisedTargetLocalY = 0f;

    [Header("Respawn Safety")]
    [SerializeField] private float respawnInvincibleDuration = 0.25f;

    private PlayerHealth respawnInvincibleHealth;
    private Coroutine respawnInvincibleRoutine;

    public void ApplyRetryState(PlayerHealth playerHealth)
    {
        ResetBoss();
        ApplyArenaState();
        BeginBattleIfNeeded();
        RespawnPlayer(playerHealth);
        StartRespawnInvincibility(playerHealth);
        ApplyCamera(playerHealth);
    }

    private void OnDisable()
    {
        ClearRespawnInvincibility();
    }

    private void ApplyArenaState()
    {
        if (raisedStateTargets == null)
        {
            return;
        }

        for (int i = 0; i < raisedStateTargets.Length; i++)
        {
            Transform target = raisedStateTargets[i];
            if (target == null)
            {
                continue;
            }

            Vector3 localPosition = target.localPosition;
            localPosition.y = raisedTargetLocalY;
            target.localPosition = localPosition;
        }
    }

    private void RespawnPlayer(PlayerHealth playerHealth)
    {
        if (playerHealth != null && respawnPoint != null)
        {
            playerHealth.Respawn(respawnPoint.position);
            return;
        }

        Debug.LogWarning("[BossRetryCheckpoint] PlayerHealth 또는 Respawn Point가 연결되지 않았습니다.", this);
    }

    private void ResetBoss()
    {
        if (bossRetryTarget is IBossEncounter encounter)
        {
            encounter.ResetForRetry();
            return;
        }

        Debug.LogWarning("[BossRetryCheckpoint] IBossEncounter 재시작 대상이 연결되지 않았습니다.", this);
    }

    private void BeginBattleIfNeeded()
    {
        if (!beginBattleAfterRetry)
        {
            return;
        }

        if (bossRetryTarget is IBossEncounter encounter)
        {
            encounter.BeginBattle();
        }
    }

    private void ApplyCamera(PlayerHealth playerHealth)
    {
        if (retryCameraFocus != null)
        {
            retryCameraFocus.Focus(playerHealth != null ? playerHealth.transform : null);
        }
    }

    private void StartRespawnInvincibility(PlayerHealth playerHealth)
    {
        ClearRespawnInvincibility();

        if (playerHealth == null || respawnInvincibleDuration <= 0f)
        {
            return;
        }

        respawnInvincibleHealth = playerHealth;
        respawnInvincibleHealth.AddInvincibleOverride(this);
        respawnInvincibleRoutine = StartCoroutine(RespawnInvincibleRoutine());
    }

    private IEnumerator RespawnInvincibleRoutine()
    {
        yield return new WaitForSeconds(respawnInvincibleDuration);
        respawnInvincibleRoutine = null;
        ClearRespawnInvincibility();
    }

    private void ClearRespawnInvincibility()
    {
        if (respawnInvincibleRoutine != null)
        {
            StopCoroutine(respawnInvincibleRoutine);
            respawnInvincibleRoutine = null;
        }

        if (respawnInvincibleHealth != null)
        {
            respawnInvincibleHealth.RemoveInvincibleOverride(this);
            respawnInvincibleHealth = null;
        }
    }
}
