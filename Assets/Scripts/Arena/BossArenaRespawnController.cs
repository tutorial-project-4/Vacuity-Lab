using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BossArenaRespawnController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverRoot;
    [SerializeField] private CanvasGroup gameOverGroup;

    private BossRetryCheckpoint activeCheckpoint;

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
            playerHealth.Died += ShowGameOver;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= ShowGameOver;
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

    private void ShowGameOver()
    {
        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(true);
        }

        if (gameOverGroup != null)
        {
            gameOverGroup.alpha = 1f;
            gameOverGroup.interactable = true;
            gameOverGroup.blocksRaycasts = true;
        }
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

    private void OnValidate()
    {
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
