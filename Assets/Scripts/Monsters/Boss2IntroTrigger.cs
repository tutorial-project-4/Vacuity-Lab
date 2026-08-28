using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class Boss2IntroTrigger : MonoBehaviour
{
    [SerializeField] GameObject boss2;
    [SerializeField] string platformName = "platform-boss2";
    [SerializeField] float platformTargetY = 26f;
    [SerializeField] float platformMoveDuration = 1f;
    [SerializeField] Transform retryPoint;
    Transform platform;
    Vector3 platformStartPosition;
    bool triggered;

    void Awake()
    {
        platform = GameObject.Find(platformName)?.transform;
        if (platform != null) platformStartPosition = platform.position;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || boss2 == null || other.GetComponentInParent<PlayerMovement>() == null) return;
        IBossEncounter encounter = boss2.GetComponent<IBossEncounter>();
        if (encounter == null)
        {
            Debug.LogWarning("[Boss2IntroTrigger] IBossEncounter 보스 참조가 없습니다", this);
            return;
        }
        triggered = true;
        FindAnyObjectByType<GameplayUI>()?.SetRetryCheckpoint(encounter, retryPoint);
        StartCoroutine(RaisePlatform(encounter));
    }

    IEnumerator RaisePlatform(IBossEncounter encounter)
    {
        if (platform == null)
        {
            Debug.LogWarning($"[Boss2IntroTrigger] '{platformName}' 오브젝트가 없습니다", this);
            encounter.BeginBattle();
            yield break;
        }

        Vector3 start = platform.position;
        float elapsed = 0f;
        while (elapsed < platformMoveDuration)
        {
            elapsed += Time.deltaTime;
            Vector3 position = start;
            position.y = Mathf.Lerp(start.y, platformTargetY, Mathf.Clamp01(elapsed / platformMoveDuration));
            platform.position = position;
            yield return null;
        }

        Vector3 target = start;
        target.y = platformTargetY;
        platform.position = target;
        encounter.BeginBattle();
    }

    public void ResetForRetry()
    {
        StopAllCoroutines();
        triggered = false;
        if (platform != null) platform.position = platformStartPosition;
    }
}
