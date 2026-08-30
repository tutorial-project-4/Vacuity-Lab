using System.Collections;
using UnityEngine;

public class DannyReleaseSequenceAction : DialogueInteractionAction
{
    [Header("Dialogue")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private PlayerController player;
    [SerializeField] private DialogueLine[] postReleaseLines;

    [Header("Fade")]
    [SerializeField] private ScreenFadeController fadeController;
    [SerializeField] private float fadeOutDuration = 0.8f;
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float postFadeDialogueDelay;

    [Header("Scene Objects")]
    [SerializeField] private SpriteRenderer targetTankRenderer;
    [SerializeField] private Sprite releasedTankSprite;
    [SerializeField] private GameObject dannyObject;

    [Header("Options")]
    [SerializeField] private bool lockPlayerDuringFade = true;
    [SerializeField] private bool runOnce = true;

    private bool hasRun;
    private Coroutine routine;

    public override void Run()
    {
        if ((runOnce && hasRun) || routine != null)
        {
            return;
        }

        routine = StartCoroutine(ReleaseRoutine());
    }

    private IEnumerator ReleaseRoutine()
    {
        PlayerController targetPlayer = GetPlayer();
        ScreenFadeController fade = GetFadeController();

        if (targetPlayer != null && lockPlayerDuringFade)
        {
            targetPlayer.SetCutsceneLock(true);
        }

        if (fade != null)
        {
            fade.gameObject.SetActive(true);
            yield return fade.FadeOut(fadeOutDuration);
        }

        if (targetTankRenderer != null && releasedTankSprite != null)
        {
            targetTankRenderer.sprite = releasedTankSprite;
        }

        if (dannyObject != null)
        {
            dannyObject.SetActive(true);
        }

        if (fade != null)
        {
            yield return fade.FadeIn(fadeInDuration);
        }

        if (targetPlayer != null && lockPlayerDuringFade)
        {
            targetPlayer.SetCutsceneLock(false);
        }

        if (postFadeDialogueDelay > 0f)
        {
            yield return new WaitForSeconds(postFadeDialogueDelay);
        }

        DialogueRunner runner = GetDialogueRunner();
        if (runner != null && postReleaseLines != null && postReleaseLines.Length > 0)
        {
            runner.StartDialogue(postReleaseLines, targetPlayer);
        }

        hasRun = true;
        routine = null;
    }

    private DialogueRunner GetDialogueRunner()
    {
        if (dialogueRunner != null)
        {
            return dialogueRunner;
        }

        dialogueRunner = DialogueRunner.Instance;
        return dialogueRunner;
    }

    private PlayerController GetPlayer()
    {
        if (player != null)
        {
            return player;
        }

#if UNITY_2023_1_OR_NEWER
        player = FindFirstObjectByType<PlayerController>();
#else
        player = FindObjectOfType<PlayerController>();
#endif
        return player;
    }

    private ScreenFadeController GetFadeController()
    {
        if (fadeController != null)
        {
            return fadeController;
        }

#if UNITY_2023_1_OR_NEWER
        fadeController = FindFirstObjectByType<ScreenFadeController>(FindObjectsInactive.Include);
#else
        fadeController = FindObjectOfType<ScreenFadeController>(true);
#endif
        return fadeController;
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (player != null && lockPlayerDuringFade)
        {
            player.SetCutsceneLock(false);
        }
    }
}
