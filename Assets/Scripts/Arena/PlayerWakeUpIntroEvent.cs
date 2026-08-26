using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerWakeUpIntroEvent : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float startDelay = 0.15f;

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAnimationController playerAnimationController;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private DialogueRunner dialogueRunner;

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] introLines =
    {
        new DialogueLine { speaker = "???", text = "여기가 어디지?" },
        new DialogueLine { speaker = "???", text = "윽... 머리가 깨질 것처럼 아프다." },
    };

    [Header("Animation States")]
    [SerializeField] private string lyingStateName = "Intro_Lie";
    [SerializeField] private string wakeUpStateName = "Intro_WakeUp";
    [SerializeField] private string idleStateName = "idle";
    [SerializeField] private float wakeUpAnimationDuration = 1f;
    [SerializeField] private bool disablePlayerAnimationControllerDuringIntro = true;
    [SerializeField] private bool warnMissingAnimationStates = false;

    private Coroutine routine;
    private bool restoredControl;
    private bool animationControllerWasEnabled;

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        if (routine != null)
        {
            return;
        }

        routine = StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        CacheReferences();
        LockPlayer();

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        PlayAnimatorState(lyingStateName);
        yield return RunDialogue();

        PlayAnimatorState(wakeUpStateName);
        if (wakeUpAnimationDuration > 0f)
        {
            yield return new WaitForSeconds(wakeUpAnimationDuration);
        }

        PlayAnimatorState(idleStateName);
        RestoreControl();
        routine = null;
    }

    private IEnumerator RunDialogue()
    {
        if (dialogueRunner == null || introLines == null || introLines.Length == 0)
        {
            yield break;
        }

        if (!dialogueRunner.StartDialogue(introLines, null))
        {
            yield break;
        }

        while (dialogueRunner != null && dialogueRunner.IsRunning)
        {
            yield return null;
        }
    }

    private void LockPlayer()
    {
        restoredControl = false;

        if (playerController != null)
        {
            playerController.SetCutsceneLock(true);
        }
        else if (playerMovement != null)
        {
            playerMovement.SetControlLocked(true);
        }

        if (playerAnimationController != null && disablePlayerAnimationControllerDuringIntro)
        {
            animationControllerWasEnabled = playerAnimationController.enabled;
            playerAnimationController.enabled = false;
        }
    }

    private void RestoreControl()
    {
        if (restoredControl)
        {
            return;
        }

        restoredControl = true;

        if (playerAnimationController != null && disablePlayerAnimationControllerDuringIntro)
        {
            playerAnimationController.enabled = animationControllerWasEnabled;
        }

        if (playerController != null)
        {
            playerController.SetCutsceneLock(false);
        }
        else if (playerMovement != null)
        {
            playerMovement.SetControlLocked(false);
        }
    }

    private void PlayAnimatorState(string stateName)
    {
        if (playerAnimator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (!playerAnimator.HasState(0, stateHash))
        {
            if (warnMissingAnimationStates)
            {
                Debug.LogWarning($"[PlayerWakeUpIntroEvent] Animator state '{stateName}' is missing. Add the clip later or update the state name.", this);
            }

            return;
        }

        playerAnimator.Play(stateHash, 0, 0f);
        playerAnimator.Update(0f);
    }

    private void CacheReferences()
    {
        if (playerController == null)
        {
#if UNITY_2023_1_OR_NEWER
            playerController = FindFirstObjectByType<PlayerController>();
#else
            playerController = FindObjectOfType<PlayerController>();
#endif
        }

        if (playerMovement == null && playerController != null)
        {
            playerMovement = playerController.GetComponent<PlayerMovement>();
        }

        if (playerAnimationController == null && playerController != null)
        {
            playerAnimationController = playerController.GetComponent<PlayerAnimationController>();
        }

        if (playerAnimator == null && playerController != null)
        {
            playerAnimator = PlayerVisualResolver.FindAnimator(playerController, null);
        }

        if (dialogueRunner == null)
        {
            dialogueRunner = DialogueRunner.Instance;
        }
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        RestoreControl();
    }

    private void OnValidate()
    {
        startDelay = Mathf.Max(0f, startDelay);
        wakeUpAnimationDuration = Mathf.Max(0f, wakeUpAnimationDuration);
    }
}
