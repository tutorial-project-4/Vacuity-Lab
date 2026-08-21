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

    [Header("Arena State")]
    [SerializeField] private Transform[] raisedStateTargets;
    [SerializeField] private float raisedTargetLocalY = 0f;

    [Header("Respawn Safety")]
    [SerializeField] private float respawnInvincibleDuration = 0.25f;

    private PlayerHealth respawnInvincibleHealth;
    private Coroutine respawnInvincibleRoutine;

    public void ApplyRetryState(PlayerHealth playerHealth)
    {
        ApplyArenaState();
        ResetBoss();
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
        if (bossRetryTarget != null)
        {
            bossRetryTarget.SendMessage("ResetForRetry", SendMessageOptions.DontRequireReceiver);
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
