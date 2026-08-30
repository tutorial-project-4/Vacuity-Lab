using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BossArenaRespawnController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Animator playerAnimator;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverRoot;
    [SerializeField] private CanvasGroup gameOverGroup;
    [SerializeField] private AudioClip gameOverClip;

    [Header("Death Animation")]
    [SerializeField] private string deathStateName = "Death";
    [SerializeField] private float deathAnimationFallbackDelay = 1f;

    private BossRetryCheckpoint activeCheckpoint;
    private Coroutine showGameOverRoutine;

    private void Awake()
    {
        ResolvePlayerHealth();
        HideGameOver();
    }

    private void OnEnable()
    {
        ResolvePlayerHealth();

        if (playerHealth != null)
        {
            playerHealth.Died += HandlePlayerDied;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= HandlePlayerDied;
        }

        if (showGameOverRoutine != null)
        {
            StopCoroutine(showGameOverRoutine);
            showGameOverRoutine = null;
        }
    }

    public void ActivateCheckpoint(BossRetryCheckpoint checkpoint)
    {
        if (checkpoint == null)
        {
            Debug.LogWarning("[BossArenaRespawnController] 활성화할 보스전 체크포인트가 연결되지 않았습니다.", this);
            return;
        }

        activeCheckpoint = checkpoint;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        HideGameOver();
        ResolvePlayerHealth();

        if (activeCheckpoint == null)
        {
            Debug.LogWarning("[BossArenaRespawnController] 활성화된 보스전 체크포인트가 없어 재도전할 수 없습니다.", this);
            return;
        }

        activeCheckpoint.ApplyRetryState(playerHealth);
    }

    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    private void HandlePlayerDied()
    {
        if (showGameOverRoutine != null)
        {
            StopCoroutine(showGameOverRoutine);
        }

        showGameOverRoutine = StartCoroutine(ShowGameOverAfterDeathAnimation());
    }

    private IEnumerator ShowGameOverAfterDeathAnimation()
    {
        ResolvePlayerAnimator();

        yield return null;
        yield return null;

        float waitTime = GetDeathAnimationWaitTime();
        if (waitTime > 0f)
        {
            yield return new WaitForSecondsRealtime(waitTime);
        }

        showGameOverRoutine = null;
        ShowGameOver();
    }

    private void ShowGameOver()
    {
        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(true);
        }

        if (gameOverClip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUi(gameOverClip);
        }

        if (gameOverGroup != null)
        {
            gameOverGroup.alpha = 1f;
            gameOverGroup.interactable = true;
            gameOverGroup.blocksRaycasts = true;
        }
    }

    private float GetDeathAnimationWaitTime()
    {
        float waitTime = deathAnimationFallbackDelay;
        if (playerAnimator == null)
        {
            return waitTime;
        }

        AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clipInfos = playerAnimator.GetCurrentAnimatorClipInfo(0);
        if (stateInfo.IsName(deathStateName) && clipInfos.Length > 0 && clipInfos[0].clip != null)
        {
            float speed = Mathf.Abs(stateInfo.speed * playerAnimator.speed);
            if (speed > 0f)
            {
                waitTime = Mathf.Max(waitTime, clipInfos[0].clip.length / speed);
            }
        }

        return waitTime;
    }

    private void HideGameOver()
    {
        if (gameOverGroup != null)
        {
            gameOverGroup.alpha = 0f;
            gameOverGroup.interactable = false;
            gameOverGroup.blocksRaycasts = false;
        }

        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(false);
        }
    }

    private void ResolvePlayerHealth()
    {
        if (playerHealth != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        PlayerHealth[] candidates = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        PlayerHealth[] candidates = Resources.FindObjectsOfTypeAll<PlayerHealth>();
#endif
        if (candidates == null || candidates.Length == 0)
        {
            Debug.LogError("[BossArenaRespawnController] PlayerHealth reference is missing and no PlayerHealth was found in the scene.", this);
            return;
        }

        if (candidates.Length > 1)
        {
            Debug.LogError("[BossArenaRespawnController] PlayerHealth reference is missing and multiple PlayerHealth components were found. Assign the intended PlayerHealth in the Inspector.", this);
            return;
        }

        playerHealth = candidates[0];
    }

    private void ResolvePlayerAnimator()
    {
        if (playerAnimator != null)
        {
            return;
        }

        ResolvePlayerHealth();
        if (playerHealth == null)
        {
            return;
        }

        playerAnimator = PlayerVisualResolver.FindAnimator(playerHealth, null);
    }

    private void OnValidate()
    {
        deathAnimationFallbackDelay = Mathf.Max(0f, deathAnimationFallbackDelay);

        if (gameOverRoot == null)
        {
            Debug.LogWarning("[BossArenaRespawnController] Game Over Root is not assigned.", this);
        }

        if (gameOverGroup == null)
        {
            Debug.LogWarning("[BossArenaRespawnController] Game Over Group is not assigned.", this);
        }

        if (playerHealth == null)
        {
            Debug.LogWarning("[BossArenaRespawnController] PlayerHealth is not assigned. Assign it in the Inspector when possible.", this);
        }
    }
}
