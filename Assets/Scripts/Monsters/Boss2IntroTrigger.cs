using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Boss2IntroTrigger : MonoBehaviour
{
    const string CheckpointKey = "game.checkpoint";
    [SerializeField] GameObject boss2;
    [SerializeField] string platformName = "platform-boss2";
    [SerializeField] float platformTargetY = 26f;
    [SerializeField] float platformMoveDuration = 1f;
    [SerializeField] float bossRiseDistance = 20f;
    [SerializeField] Transform retryPoint;
    [SerializeField] bool triggerOnPlayerEnter = true;
    Transform platform;
    Transform bossTransform;
    Vector3 platformStartPosition;
    Vector3 bossStartPosition;
    Collider2D[] bossColliders;
    bool[] bossColliderStates;
    Rigidbody2D bossBody;
    bool bossBodySimulated;
    bool triggered;

    public Vector2 RetryPosition => retryPoint != null ? retryPoint.position : transform.position;

    void Awake()
    {
        platform = GameObject.Find(platformName)?.transform;
        if (platform != null) platformStartPosition = platform.position;
        bossTransform = boss2 != null ? boss2.transform : null;
        if (bossTransform != null) bossStartPosition = bossTransform.position;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnPlayerEnter || other.GetComponentInParent<PlayerMovement>() == null) return;

        PlayIntro();
    }

    public bool PlayIntro()
    {
        if (triggered || boss2 == null) return false;
        IBossEncounter encounter = boss2.GetComponent<IBossEncounter>();
        if (encounter == null)
        {
            Debug.LogWarning("[Boss2IntroTrigger] IBossEncounter 보스 참조가 없습니다", this);
            return false;
        }
        triggered = true;
        PlayerPrefs.SetInt(CheckpointKey, 2);
        PlayerPrefs.Save();
        FindAnyObjectByType<GameplayUI>()?.SetRetryCheckpoint(encounter, retryPoint);
        StartCoroutine(RaisePlatform(encounter));
        return true;
    }

    IEnumerator RaisePlatform(IBossEncounter encounter)
    {
        if (platform == null)
            Debug.LogWarning($"[Boss2IntroTrigger] '{platformName}' 오브젝트가 없습니다", this);

        Vector3 platformStart = platform != null ? platform.position : default;
        Vector3 bossEntranceStart = bossStartPosition - Vector3.up * bossRiseDistance;
        BeginBossEntrance(bossEntranceStart);
        float elapsed = 0f;
        while (elapsed < platformMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / platformMoveDuration);
            if (platform != null)
            {
                Vector3 position = platformStart;
                position.y = Mathf.Lerp(platformStart.y, platformTargetY, t);
                platform.position = position;
            }
            if (bossTransform != null) bossTransform.position = Vector3.Lerp(bossEntranceStart, bossStartPosition, t);
            yield return null;
        }

        if (platform != null)
        {
            Vector3 target = platformStart;
            target.y = platformTargetY;
            platform.position = target;
        }
        RestoreBossEntrance();
        encounter.BeginBattle();
    }

    void BeginBossEntrance(Vector3 start)
    {
        if (bossTransform == null) return;
        bossColliders = boss2.GetComponentsInChildren<Collider2D>(true);
        bossColliderStates = System.Array.ConvertAll(bossColliders, item => item.enabled);
        bossBody = boss2.GetComponent<Rigidbody2D>();
        bossBodySimulated = bossBody == null || bossBody.simulated;
        foreach (Collider2D item in bossColliders) item.enabled = false;
        if (bossBody != null) bossBody.simulated = false;
        bossTransform.position = start;
    }

    void RestoreBossEntrance()
    {
        if (bossTransform != null) bossTransform.position = bossStartPosition;
        if (bossColliders != null)
            for (int i = 0; i < bossColliders.Length; i++)
                if (bossColliders[i] != null) bossColliders[i].enabled = bossColliderStates[i];
        if (bossBody != null) bossBody.simulated = bossBodySimulated;
        bossColliders = null;
        bossColliderStates = null;
        bossBody = null;
    }

    public void ResetForRetry()
    {
        StopAllCoroutines();
        triggered = false;
        if (platform != null) platform.position = platformStartPosition;
        RestoreBossEntrance();
    }

    public bool PrepareQuickStart(GameplayUI ui)
    {
        IBossEncounter encounter = boss2 != null ? boss2.GetComponent<IBossEncounter>() : null;
        if (encounter == null || ui == null) return false;
        ui.SetRetryCheckpoint(encounter, retryPoint);
        return true;
    }
}
