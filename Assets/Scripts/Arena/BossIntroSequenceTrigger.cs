using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class BossIntroSequenceTrigger : MonoBehaviour
{
    [Header("Required Dialogue")]
    [SerializeField] private BossIntroDialogueSequenceTrigger requiredDialogue;

    [Header("Camera")]
    [SerializeField] private CameraFocusTrigger[] cameraFocuses;

    [Header("Scene Actions")]
    [SerializeField] private TriggeredLocalYMove[] localYMoves;
    [SerializeField] private DialogueInteractionAction[] actions;

    [Header("Boss Battle")]
    [Tooltip("IBossEncounter를 구현한 보스 컴포넌트입니다.")]
    [SerializeField] private MonoBehaviour boss;
    [SerializeField] private float bossStartDelay = 1f;
    [SerializeField] private BossArenaRespawnController respawnController;
    [SerializeField] private BossRetryCheckpoint checkpoint;
    [SerializeField] private float checkpointActivationDelay = 1f;

    [Header("Trigger")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;
    private Coroutine introRoutine;

    private void Reset()
    {
        ConfigureTriggerCollider();
    }

    private void Awake()
    {
        ConfigureTriggerCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        if (requiredDialogue != null && !requiredDialogue.HasCompleted)
        {
            return;
        }

        PlayIntro(player.transform);
    }

    public void PlayIntro(Transform playerTransform)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
        }

        introRoutine = StartCoroutine(IntroRoutine(playerTransform));
        hasTriggered = true;
    }

    private IEnumerator IntroRoutine(Transform playerTransform)
    {
        RunCameraFocuses(playerTransform);
        RunLocalYMoves();
        RunActions();

        if (bossStartDelay > 0f)
        {
            yield return new WaitForSeconds(bossStartDelay);
        }

        StartBossBattle();

        if (checkpointActivationDelay > 0f)
        {
            yield return new WaitForSeconds(checkpointActivationDelay);
        }

        ActivateCheckpoint();
        introRoutine = null;
    }

    private void RunCameraFocuses(Transform playerTransform)
    {
        if (cameraFocuses == null)
        {
            return;
        }

        for (int i = 0; i < cameraFocuses.Length; i++)
        {
            if (cameraFocuses[i] != null)
            {
                cameraFocuses[i].Focus(playerTransform);
            }
        }
    }

    private void RunLocalYMoves()
    {
        if (localYMoves == null)
        {
            return;
        }

        for (int i = 0; i < localYMoves.Length; i++)
        {
            if (localYMoves[i] != null)
            {
                localYMoves[i].TriggerMove();
            }
        }
    }

    private void RunActions()
    {
        if (actions == null)
        {
            return;
        }

        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i] != null)
            {
                actions[i].Run();
            }
        }
    }

    private void StartBossBattle()
    {
        if (boss == null)
        {
            return;
        }

        if (boss is IBossEncounter encounter)
        {
            encounter.BeginBattle();
        }
        else
        {
            Debug.LogWarning("[BossIntroSequenceTrigger] IBossEncounter 보스 참조가 없습니다.", this);
        }
    }

    private void ActivateCheckpoint()
    {
        if (respawnController != null)
        {
            respawnController.ActivateCheckpoint(checkpoint);
        }
    }

    private void OnValidate()
    {
        bossStartDelay = Mathf.Max(0f, bossStartDelay);
        checkpointActivationDelay = Mathf.Max(0f, checkpointActivationDelay);
        ConfigureTriggerCollider();
    }

    private void ConfigureTriggerCollider()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnDisable()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }
    }
}
