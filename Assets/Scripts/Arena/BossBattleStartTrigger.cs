using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class BossBattleStartTrigger : MonoBehaviour
{
    [Tooltip("IBossEncounter를 구현한 보스 컴포넌트입니다.")]
    [SerializeField] MonoBehaviour boss;
    [SerializeField] float dropHeight = 20f;
    [SerializeField] float entranceDuration = 1f;
    Vector3 bossEndPosition;
    Collider2D[] bossColliders;
    bool[] bossColliderStates;
    Rigidbody2D bossBody;
    bool bossBodySimulated;
    bool triggered;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Awake()
    {
        if (boss == null) return;
        bossEndPosition = boss.transform.position;
        bossColliders = boss.GetComponentsInChildren<Collider2D>(true);
        bossColliderStates = System.Array.ConvertAll(bossColliders, item => item.enabled);
        bossBody = boss.GetComponent<Rigidbody2D>();
        bossBodySimulated = bossBody == null || bossBody.simulated;
        foreach (Collider2D item in bossColliders) item.enabled = false;
        if (bossBody != null) bossBody.simulated = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || other.GetComponentInParent<PlayerController>() == null) return;
        if (boss is not IBossEncounter encounter)
        {
            Debug.LogWarning("[BossBattleStartTrigger] IBossEncounter 보스 참조가 없습니다", this);
            return;
        }

        triggered = true;
        StartCoroutine(PlayEntrance(encounter));
    }

    IEnumerator PlayEntrance(IBossEncounter encounter)
    {
        Transform bossTransform = boss.transform;
        Vector3 start = bossEndPosition + Vector3.up * dropHeight;
        bossTransform.position = start;

        float elapsed = 0f;
        while (elapsed < entranceDuration)
        {
            elapsed += Time.deltaTime;
            bossTransform.position = Vector3.Lerp(start, bossEndPosition, Mathf.Clamp01(elapsed / entranceDuration));
            yield return null;
        }

        bossTransform.position = bossEndPosition;
        if (bossBody != null)
        {
            bossBody.position = bossEndPosition;
            bossBody.linearVelocity = Vector2.zero;
            bossBody.angularVelocity = 0f;
        }
        Physics2D.SyncTransforms();
        if (bossBody != null) bossBody.simulated = bossBodySimulated;
        for (int i = 0; i < bossColliders.Length; i++)
            if (bossColliders[i] != null) bossColliders[i].enabled = bossColliderStates[i];
        encounter.BeginBattle();
    }
}
