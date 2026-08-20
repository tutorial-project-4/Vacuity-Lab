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
        CachePlayerHealth();
        HideGameOver();
    }

    private void OnEnable()
    {
        CachePlayerHealth();

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
        CachePlayerHealth();

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

    private void CachePlayerHealth()
    {
        if (playerHealth != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        playerHealth = FindFirstObjectByType<PlayerHealth>();
#else
        playerHealth = FindObjectOfType<PlayerHealth>();
#endif
    }
}
